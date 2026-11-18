// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// The behaviour, as the RE_Kenshi plugin's: Shift+L (or the window) switches slow motion on at the chosen speed and back off to the speed it came
// from; Shift+L held with + / - steps the speed; the game's own speed buttons while it is on hand control back to the game (logged). Runs on the
// host thread: the input resolve, then the control tick (WorldSession.Tick, also while paused). The window's buttons queue actions for the tick.
// 0.2.0: the window, its drag, the tiny button and its wheel are PZA's Meitou UI's (SlowWindow.cs); this file keeps the keys and the speed.
// 0.3.0: holds, the door for other PZA mods (SlowMoApi, contract version 1): time runs at the slowest hold
// while one is held, under these rules (paused stays paused, the player wins, the last release puts the speeds back).
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

    // holds (0.3.0): holder -> speed in hundredths; holders whose hold changes nothing until they release (started while paused, or
    // handed back to the player's speed buttons); whether a hold runs the clock; the clock's speeds from before it (slow motion off)
    static readonly object s_holdLock = new();
    static readonly Dictionary<string, int> s_holds = new();
    static readonly HashSet<string> s_ignored = new();
    static bool s_holdOn;
    static SimulationClock.SpeedState? s_saved;

    /// <summary>The control tick runs (Start succeeded); holds are refused before. Public for the offline check.</summary>
    public static bool Live;

    /// <summary>SlowMoApi.Hold: recorded here, taken up at the next control tick.</summary>
    public static bool Hold(string holder, double speed)
    {
        if (!Live || string.IsNullOrWhiteSpace(holder) || !double.IsFinite(speed)) return false;
        int h = (int)Math.Round(Math.Clamp(speed, 0.05, 1) * 100);
        lock (s_holdLock) s_holds[holder] = h;
        return true;
    }

    /// <summary>SlowMoApi.Release: ends that hold at the next control tick; an unknown holder changes nothing.</summary>
    public static void Release(string holder)
    {
        if (holder is null) return;
        lock (s_holdLock) { s_holds.Remove(holder); s_ignored.Remove(holder); }
    }

    /// <summary>Any hold is recorded (SlowMoApi.Held).</summary>
    public static bool Held { get { lock (s_holdLock) return s_holds.Count > 0; } }

    /// <summary>A hold runs the clock now (for the offline check).</summary>
    public static bool HoldRuns => s_holdOn;

    /// <summary>The slowest hold that counts, in hundredths, or -1.</summary>
    static int HoldH()
    {
        lock (s_holdLock)
        {
            int min = -1;
            foreach (var (holder, h) in s_holds)
                if (!s_ignored.Contains(holder) && (min < 0 || h < min)) min = h;
            return min;
        }
    }

    /// <summary>Every recorded hold stops counting until its holder releases it; returns those holders.</summary>
    static List<string> IgnoreAll()
    {
        lock (s_holdLock)
        {
            var list = s_holds.Keys.Where(k => !s_ignored.Contains(k)).ToList();
            foreach (var k in list) s_ignored.Add(k);
            return list;
        }
    }

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

    /// <summary>Meitou's build mode is on. The offline check swaps it for a stub (it has no PlayerInterface).</summary>
    public static Func<bool> IsBuilding = () => Player is not null && Access.Building(Player);

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
        int step = SlowCore.ChordStep(ref s_chord, started, held, plus, minus);
        if ((step == SlowCore.Faster || step == SlowCore.Slower) && IsBuilding())
        {
            // 0.3.0 (Meitou 0.22): while a building is placed, + / - move and tilt its ghost; we do not also change the speed. The tap still
            // counts as used (no toggle when Shift+L is let go).
            Counters.Add("skip.build");
            step = SlowCore.None;
        }
        switch (step)
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
            int dropped;
            lock (s_holdLock) { dropped = s_holds.Count; s_holds.Clear(); s_ignored.Clear(); }
            if (s_session is not null && (dropped > 0 || s_holdOn))
            {   // rule 7: a new world drops every hold, silently (no pop-up)
                Counters.Add("hold.new_session");
                Log.Line($"holds dropped: a new world was loaded ({dropped} holder(s); its own speed kept)");
            }
            s_holdOn = false;
            s_saved = null;
            s_session = world;
        }

        if (SlowCore.ChangedOutside(On || s_holdOn, s_setH, clock.Speed, clock.IsPaused))
        {
            if (s_holdOn)
            {   // rule 4: the player's speed button wins over a hold; its later release changes nothing
                var who = IgnoreAll();
                s_holdOn = false;
                s_saved = null;
                Counters.Add("hold.handed_back");
                Log.Line($"hold dropped ({string.Join(", ", who)}): the game's speed was set to {clock.Speed:0.##} (its speed buttons / F2-F4)");
            }
            if (On)
            {
                On = false;
                Counters.Add("off.game_speed_button");
                Log.Line($"slow motion OFF: the game's speed was set to {clock.Speed:0.##} (its speed buttons / F2-F4): control handed back");
            }
            s_setH = -1;   // nothing of ours runs the clock now
        }

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
                    }
                    break;
            }
        }

        StepHolds(clock);

        // the speed setting or a hold moved (settings page, window, keys, another mod): follow it; a player's pause stays a pause
        if (On || s_holdOn)
        {
            int target = Target();
            if (target != s_setH) Set(clock, target, keepPause: true);
        }
    }

    /// <summary>What the clock runs at while we run it: slow motion's speed (or 1), and the slowest hold below that (rules 3, 6).</summary>
    static int Target()
    {
        int h = On ? SpeedH : 100;
        int hold = s_holdOn ? HoldH() : -1;
        return hold > 0 && hold < h ? hold : h;
    }

    /// <summary>Takes up a new hold and ends the last one (rules 1, 2, 3, 6).</summary>
    static void StepHolds(SimulationClock clock)
    {
        int hold = HoldH();
        if (!s_holdOn && hold > 0)
        {
            if (clock.IsPaused)
            {   // rule 1: paused stays paused; these holders change nothing until they release
                var who = IgnoreAll();
                Counters.Add("hold.while_paused");
                Log.Line($"hold ({string.Join(", ", who)}) while the game is paused: nothing changes");
                return;
            }
            s_holdOn = true;
            s_saved = On ? null : clock.SaveSpeeds();   // rule 2 (slow motion off) / rule 3 (on: its own return speed stands)
            if (!On) s_setH = -1;   // the target step below sets the hold's speed even when it equals a speed we set long ago
            Counters.Add("hold.on");
            Log.Line($"hold ON at {Strings.Speed(hold)} (was {clock.Speed:0.##}{(On ? ", slow motion on at " + Strings.Speed(SpeedH) : "")})");
        }
        else if (s_holdOn && hold < 0)
        {
            s_holdOn = false;
            if (On)
                Set(clock, SpeedH, keepPause: true);   // rule 3: back to the slow speed
            else if (s_saved is { } sv)
            {
                var now = clock.SaveSpeeds();
                if (now.Speed == 0)
                {   // rule 4: the player paused during the hold: stays paused, and the pause key resumes where the game was
                    bool keyPaused = Math.Abs(now.Remembered - s_setH / 100.0) < 0.004;
                    clock.RestoreSpeeds(new(0, sv.Last, keyPaused ? sv.Speed : sv.Remembered));
                }
                else
                    clock.RestoreSpeeds(sv);   // rule 2
            }
            s_saved = null;
            if (!On) s_setH = -1;
            Counters.Add("hold.off");
            Log.Line($"hold OFF: speed {clock.Speed:0.##}{(clock.Speed == 0 ? " (paused)" : "")}");
        }
    }

    static void OnAt(SimulationClock clock, string from)
    {
        // rule 5: during a hold, slow motion returns to the speed from before the hold
        double now = s_holdOn && s_saved is { } sv ? sv.Speed : clock.Speed > 0 ? clock.Speed : clock.LastNonZeroSpeed;
        s_return = SlowCore.ReturnSpeed(now);
        s_saved = null;
        On = true;
        Set(clock, Target());
        Counters.Add("on");
        ShowNotice(Strings.T("meitou.msg.on"));
        Log.Line($"slow motion ON at {Strings.Speed(SpeedH)} ({from}; was {now:0.##}, returns to {s_return:0.##}); the game reports {clock.Speed:0.###}");
    }

    static void Off(SimulationClock clock, string from)
    {
        On = false;
        if (s_holdOn)
        {   // rule 5: the hold goes on; its release returns to the speed slow motion came from
            s_saved = new SimulationClock.SpeedState(s_return, s_return, s_return);
            Set(clock, Target());
        }
        else
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

    /// <summary>Runs the clock at <paramref name="h"/> hundredths. <paramref name="keepPause"/>: a player's pause (speed 0) stays, and the pause
    /// key / the pause button resume at the new speed instead.</summary>
    static void Set(SimulationClock clock, int h, bool keepPause = false)
    {
        if (keepPause && clock.Speed == 0)
        {
            var now = clock.SaveSpeeds();
            bool ours = Math.Abs(now.Remembered - s_setH / 100.0) < 0.004;
            clock.RestoreSpeeds(new(0, h / 100.0, ours ? h / 100.0 : now.Remembered));
            s_setH = h;
            return;
        }
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
