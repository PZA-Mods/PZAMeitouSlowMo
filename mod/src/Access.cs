// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// The game's non-public types and members this mod reaches, looked up ONCE by name (MeitouClient 0.20.1, clone ae1a402). A missing one turns the
// mod off with a line naming it, never a crash later. The speed itself goes through public API: WorldSession.Simulation (SimulationClock):
// SetSpeed takes any speed of 0 or above (Meitou's photo mode uses that for its own slow motion), Speed, IsPaused.
using System.Reflection;
using HarmonyLib;
using Meitou.Game.Host;
using Meitou.Game.Host.Input;

namespace PZA.SlowMotion.Meitou;

public static class Access
{
    /// <summary>Meitou.Game.PlayerInterface (internal): the player's interface.</summary>
    public static Type PlayerInterface = null!;
    /// <summary>PlayerInterface.Steer(bool): once a frame on the host thread (we only note the interface).</summary>
    public static MethodInfo Steer = null!;
    /// <summary>InputBindings.Resolve(TickInput): the control tick's keys (public).</summary>
    public static MethodInfo Resolve = null!;
    /// <summary>WorldSession.Tick(): one control tick, also while paused (public class, the method may be internal).</summary>
    public static MethodInfo SessionTick = null!;
    /// <summary>Meitou.Game.Ui.GameHotkeys.Key(Key, bool shift) (internal): the game view's hotkeys; L there is Meitou's Rest, with or without
    /// Shift. Our prefix skips it only for Shift+L.</summary>
    public static MethodInfo HotkeyKey = null!;


    /// <summary>PlayerInterface.Session (internal field).</summary>
    public static AccessTools.FieldRef<object, WorldSession> Session = null!;
    /// <summary>PlayerInterface.UiEditing { get; }: a text box has the keyboard.</summary>
    static PropertyInfo s_uiEditing = null!;

    public static List<string> ResolveAll()
    {
        var missing = new List<string>();
        var game = typeof(WorldSession).Assembly;

        PlayerInterface = game.GetType("Meitou.Game.PlayerInterface")!;
        if (PlayerInterface is null) { missing.Add("type Meitou.Game.PlayerInterface"); return missing; }

        Steer = Need(AccessTools.Method(PlayerInterface, "Steer", [typeof(bool)]), "PlayerInterface.Steer(bool)", missing);
        Resolve = Need(AccessTools.Method(typeof(InputBindings), "Resolve", [typeof(TickInput)]), "InputBindings.Resolve(TickInput)", missing);
        SessionTick = Need(AccessTools.Method(typeof(WorldSession), "Tick", Type.EmptyTypes), "WorldSession.Tick()", missing);


        var hotkeys = game.GetType("Meitou.Game.Ui.GameHotkeys");
        if (hotkeys is null) missing.Add("type Meitou.Game.Ui.GameHotkeys");
        else HotkeyKey = Need(AccessTools.Method(hotkeys, "Key", [typeof(Key), typeof(bool)]), "GameHotkeys.Key(Key, bool)", missing);

        var sessionField = AccessTools.Field(PlayerInterface, "Session");
        if (sessionField is null || sessionField.FieldType != typeof(WorldSession)) missing.Add("field PlayerInterface.Session");
        else Session = AccessTools.FieldRefAccess<WorldSession>(PlayerInterface, "Session");

        s_uiEditing = AccessTools.Property(PlayerInterface, "UiEditing")!;
        if (s_uiEditing is null || s_uiEditing.PropertyType != typeof(bool)) missing.Add("property PlayerInterface.UiEditing");

        // the public speed API this mod relies on (a later Meitou could rename it)
        var clock = typeof(global::Meitou.Simulation.Session.SimulationClock);
        foreach (var name in new[] { "SetSpeed", "get_Speed", "get_IsPaused" })
            if (clock.GetMethod(name) is null) missing.Add("method SimulationClock." + name);
        // optional: the message line (as PZA's WASD Control); a miss only turns the on / off message off
        s_notice = AccessTools.Method(PlayerInterface, "Notice", [typeof(string), typeof(bool), typeof(string)]);
        // optional: Meitou's build mode (PlayerInterface.Build => BuildMode, its internal bool Active; 0.20.1 to 0.22.1). Since 0.22.0 its
        // ghost uses = / - and NUM+ / NUM- (BuildMoveUp / Down, BuildTiltIncrease / Decrease), so our Shift+L + / - stands aside while it
        // is on. A miss means "never building": the speed keys then always work, as before.
        s_build = AccessTools.Property(PlayerInterface, "Build");
        s_buildActive = s_build is null ? null : AccessTools.Field(s_build.PropertyType, "Active");
        if (s_buildActive is not null && s_buildActive.FieldType != typeof(bool)) s_buildActive = null;
        s_selectedIds = AccessTools.Method(PlayerInterface, "SelectedIds", Type.EmptyTypes);
        if (s_selectedIds is not null && !typeof(IReadOnlyList<global::Meitou.Simulation.CharacterId>).IsAssignableFrom(s_selectedIds.ReturnType))
            s_selectedIds = null;
        return missing;
    }

    /// <summary>PlayerInterface.Notice(text, error, logText): Meitou's message line (4 s each, in turn) and the message log, as "Saved" is.</summary>
    static MethodInfo? s_notice;
    public static bool HasNotice => s_notice is not null;
    public static void Notice(object player, string text) => s_notice!.Invoke(player, [text, false, null]);

    static MethodInfo Need(MethodInfo? m, string name, List<string> missing)
    {
        if (m is null) missing.Add("method " + name);
        return m!;
    }

    static PropertyInfo? s_build;
    static FieldInfo? s_buildActive;
    /// <summary>Meitou's build-mode lookup resolved (the offline check asserts it on every Meitou it checks).</summary>
    public static bool HasBuild => s_build is not null && s_buildActive is not null;
    /// <summary>Meitou's build mode is on (placing a building): its ghost owns = / - and NUM+ / NUM-.</summary>
    public static bool Building(object player)
    {
        if (!HasBuild) return false;
        var b = s_build!.GetValue(player);
        return b is not null && (bool)s_buildActive!.GetValue(b)!;
    }

    /// <summary>0.4.0 kill cam: PlayerInterface.SelectedIds() (internal; 0.20.1 to 0.22.1). A miss only keeps the kill cam off.</summary>
    static MethodInfo? s_selectedIds;
    public static bool HasSelection => s_selectedIds is not null;
    public static IReadOnlyList<global::Meitou.Simulation.CharacterId> SelectedIds(object player) =>
        (IReadOnlyList<global::Meitou.Simulation.CharacterId>)s_selectedIds!.Invoke(player, null)!;
    /// <summary>The call threw: the kill cam stays off for the session.</summary>
    public static void DropSelection() => s_selectedIds = null;

    /// <summary>A text box has the keyboard.</summary>
    public static bool UiEditing(object player) => (bool)s_uiEditing.GetValue(player)!;
}
