// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// The code mod's entry (meitou.json "entry"). Patches are applied inside Initialize, before the game's JIT warm-up (MeitouClient docs/modding.md
// "Harmony"). Anything missing -> the mod stays off with a line saying why; the game goes on.
using System.Reflection;
using Meitou.Localisation;
using Meitou.Modding;

namespace PZA.SlowMotion.Meitou;

public sealed class SlowMoMod : IMod
{
    public void Initialize(ModContext context)
    {
        Log.Sink = context.Log;
        Driver.Group = context.AddSettings(Driver.Settings);   // saved values are applied before this returns
        // 0.2.5: Meitou keeps only the Settings page's own changes; the speed, places and opacity the mod changes are kept in its own file
        var kept = PZA.MeitouUi.PzaSettings.Keep(Driver.Group, context.Folder);

        string version = typeof(SlowMoMod).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        int plus = version.IndexOf('+');
        if (plus >= 0) version = version[..plus];
        Log.Line($"{version} for Meitou, by PZA: Shift+L slow motion on / off, hold Shift+L and tap + / - for 0.05 steps; speed {Strings.Speed(Driver.SpeedH)}");

        foreach (var line in Strings.Init(context.Folder, Loc.Language?.Code ?? "en", Driver.Settings.Language)) Log.Line(line);
        foreach (var line in kept) Log.Line(line);

        if (!Start(context.Harmony)) return;
        Driver.Live = true;   // 0.3.0: SlowMoApi holds are taken from here on
        // the window and the tiny button: PZA's Meitou UI (0.2.0)
        PZA.MeitouUi.PzaUi.Log = s => Log.Line(s);
        PZA.MeitouUi.PzaUi.EachFrame = SlowWindow.EachFrame;
        SlowWindow.Setup();
        PZA.MeitouUi.PzaUi.Install(context);
        context.AddDebugCommand("Slow motion: on / off (Shift+L)", _ => Driver.DebugToggle());
    }

    /// <summary>Resolves the game's members and patches; false (with a line) when anything is missing. Public for the offline check.</summary>
    public static bool Start(HarmonyLib.Harmony harmony)
    {
        var missing = Access.ResolveAll();
        if (missing.Count > 0)
        {
            Log.Line("stays OFF: the game lacks " + string.Join("; ", missing) + " (built for Meitou 0.20.1)");
            return false;
        }
        try
        {
            foreach (var line in Patches.Apply(harmony)) Log.Line(line);
            return true;
        }
        catch (Exception e)
        {
            Log.Line("stays OFF: patching failed (" + e.GetType().Name + ": " + e.Message + ")");
            return false;
        }
    }
}
