// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// The behaviour, as the RE_Kenshi plugin's: Shift+L (or the window) switches slow motion on at the chosen speed and back off to the speed it came
// from; Shift+L held with + / - steps the speed; the game's own speed buttons while it is on hand control back to the game (logged). Runs on the
// host thread: the input resolve, then the control tick (WorldSession.Tick, also while paused). The window's buttons queue actions for the tick.
// 0.2.0: the window, its drag, the tiny button and its wheel are PZA's Meitou UI's (SlowWindow.cs); this file keeps the keys and the speed.
using System.Collections.Concurrent;
using Meitou.Game.Host;
using Meitou.Game.Host.Input;
using Meitou.Modding;
using Meitou.Simulation.Session;

namespace PZA.SlowMotion.Meitou;

public static class Driver
{
    public enum Act { Toggle, Faster, Slower }

    /// <summary>Slow motion is on.</summary>
    public static bool On;
    /// <summary>The settings object (Settings > Mods) and its group (to change a setting so the page shows it).</summary>
    public static SlowSettings Settings = new();
    public static ModSettingsGroup? Group;
    /// <summary>The player interface seen last (PlayerInterface.Steer prefix).</summary>
    public static object? Player;

    /// <summary>The slow speed in hundredths (from the setting, snapped).</summary>
    public static int SpeedH => SlowCore.FromSpeed(Settings.Speed);

    /// <summary>Our hotkey's letter. Shift + L, as the plugin; plain L stays Meitou's Rest (GameHotkeys prefix), Ctrl / Alt never ours.</summary>
    public const Key Letter = Key.L;

    static object? s_session;
    static double s_return = 1;
    static int s_setH = -1;
    static bool s_readbackChecked;
    static SlowCore.Chord s_chord;
    static readonly ConcurrentQueue<(Act Act, string From)> s_pending = new();

    /// <summary>The window's buttons and the debug row queue here; the control tick runs them.</summary>
    public static void Queue(Act act, string from) => s_pending.Enqueue((act, from));

    /// <summary>Undoes the last toggle: drops it while it is still queued, or queues a second one (the speed ends where it started).</summary>
    public static void UndoToggle(string from)
    {
        var list = TakePending();
        int last = list.FindLastIndex(x => x.Act == Act.Toggle);
        if (last >= 0) list.RemoveAt(last);
        foreach (var p in list) s_pending.Enqueue(p);
        if (last < 0) Queue(Act.Toggle, from);
    }

    /// <summary>The queued actions, taken out (for the offline check, which has no world to run them).</summary>
    public static List<(Act Act, string From)> TakePending()
    {
        var list = new List<(Act, string)>();
        while (s_pending.TryDequeue(out var p)) list.Add(p);
        return list;
    }

    /// <summary>Shift + our letter: the game's hotkey handler must not also run Rest.</summary>
    public static bool OwnsKey(Key key, bool shift) => shift && key == Letter;

    #region Input (InputBindings.Resolve postfix)

    public static void OnResolve(TickInput input)
    {
        if (Player is not null && Access.UiEditing(Player))
        {
            if (s_chord.Held) Counters.Add("skip.typing");
            s_chord = default;   // typing ends a chord without a toggle
            return;
        }
        bool shift = input.IsDown(Key.LeftShift) || input.IsDown(Key.RightShift);
        bool ctrlAlt = input.IsDown(Key.LeftControl) || input.IsDown(Key.RightControl) || input.IsDown(Key.LeftAlt) || input.IsDown(Key.RightAlt);
        bool started = shift && !ctrlAlt && input.WasPressed(Letter);
        bool held = shift && input.IsDown(Letter);
        bool plus = input.WasPressed(Key.Equal) || input.WasPressed(Key.KeypadAdd);
        bool minus = input.WasPressed(Key.Minus) || input.WasPressed(Key.KeypadSubtract);
        switch (SlowCore.ChordStep(ref s_chord, started, held, plus, minus))
        {
            case SlowCore.Toggle: Queue(Act.Toggle, "Shift+L"); break;
            case SlowCore.Faster: Queue(Act.Faster, "Shift+L and +"); break;
            case SlowCore.Slower: Queue(Act.Slower, "Shift+L and -"); break;
        }
    }

    #endregion

    #region Control tick (WorldSession.Tick postfix)

    public static void OnTick(WorldSession session)
    {
        Log.Tick();
        Step(session, session.Simulation);
    }

    /// <summary>One control tick against a world (<paramref name="world"/>: its identity, to see a load) and its clock. Public for the offline
    /// check, which drives Meitou's real SimulationClock without a world.</summary>
    public static void Step(object world, SimulationClock clock)
    {
        if (!ReferenceEquals(world, s_session))
        {
            if (s_session is not null && On)
            {   // a load or a new game: the world's own speed stands (the plugin does the same after a load)
                On = false;
                Counters.Add("off.new_session");
                Log.Line("slow motion OFF: a new world was loaded (its own speed kept)");
            }
            s_session = world;
        }

        if (SlowCore.ChangedOutside(On, s_setH, clock.Speed, clock.IsPaused))
        {
            On = false;
            Counters.Add("off.game_speed_button");
            Log.Line($"slow motion OFF: the game's speed was set to {clock.Speed:0.##} (its speed buttons / F2-F4): control handed back");
        }

        // the speed setting moved (settings page, window, keys): follow it while on
        if (On && SpeedH != s_setH) Set(clock, SpeedH);

        while (s_pending.TryDequeue(out var p))
        {
            switch (p.Act)
            {
                case Act.Toggle:
                    if (On) Off(clock, p.From); else OnAt(clock, p.From);
                    break;
                case Act.Faster:
                case Act.Slower:
                    int h = SlowCore.Step(SpeedH, p.Act == Act.Faster ? 1 : -1);
                    if (h != SpeedH)
                    {
                        SetSetting(nameof(SlowSettings.Speed), h / 100.0);
                        Counters.Add(p.Act == Act.Faster ? "speed.faster" : "speed.slower");
                        Log.Line($"slow speed {Strings.Speed(h)} ({p.From})");
                        if (On) Set(clock, h);
                    }
                    break;
            }
        }
    }

    static void OnAt(SimulationClock clock, string from)
    {
        double now = clock.Speed > 0 ? clock.Speed : clock.LastNonZeroSpeed;
        s_return = SlowCore.ReturnSpeed(now);
        On = true;
        Set(clock, SpeedH);
        Counters.Add("on");
        ShowNotice(Strings.T("meitou.msg.on"));
        Log.Line($"slow motion ON at {Strings.Speed(SpeedH)} ({from}; was {now:0.##}, returns to {s_return:0.##}); the game reports {clock.Speed:0.###}");
    }

    static void Off(SimulationClock clock, string from)
    {
        On = false;
        clock.SetSpeed(s_return);
        Counters.Add("off");
        ShowNotice(Strings.T("meitou.msg.off"));
        Log.Line($"slow motion OFF ({from}): speed {clock.Speed:0.##}");
    }

    /// <summary>The on / off message on Meitou's message line (as PZA's WASD Control's), when the setting is on and the game has the call.</summary>
    static void ShowNotice(string text)
    {
        if (!Settings.Popups) return;
        if (Player is null || !Access.HasNotice) { Counters.Add("notice.unavailable"); return; }
        try { Access.Notice(Player, text); Counters.Add("notice.shown"); }
        catch (Exception e) { Log.Exception("notice", e); }
    }

    static void Set(SimulationClock clock, int h)
    {
        s_setH = h;
        clock.SetSpeed(h / 100.0);
        if (s_readbackChecked) return;
        s_readbackChecked = true;   // falsifier: does the clock keep the speed we set?
        bool ok = Math.Abs(clock.Speed - h / 100.0) < 1e-9;
        Log.Line($"read-back check: set {Strings.Speed(h)}, the clock reports {clock.Speed:0.###} -> {(ok ? "OK" : "DIFFERS")}");
    }

    /// <summary>Changes a setting through its group, so the settings page shows it (and keeps it, as when the player sets it there).</summary>
    public static void SetSetting(string key, object value) => PZA.MeitouUi.PzaSettings.Set(Group, Settings, key, value);

    #endregion

    /// <summary>The F10 debug row.</summary>
    public static string DebugToggle()
    {
        Queue(Act.Toggle, "F10");
        return On ? "Slow motion: turning off" : "Slow motion: turning on";
    }
}
