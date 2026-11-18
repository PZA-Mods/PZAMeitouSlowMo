// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.3.0: the hold door for other mods (contract version 1, rules in Driver.cs). Neither mod references the other; a caller finds
// this type by name and calls it through reflection:
//   var api = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PZA.SlowMotion.Meitou.SlowMoApi")).FirstOrDefault(t => t is not null);
// Call on the host thread (an input resolve or a control tick); a hold takes effect at the next control tick.
namespace PZA.SlowMotion.Meitou;

public static class SlowMoApi
{
    /// <summary>Bumped when a member changes meaning; a caller checks it is at least the version it was written for.</summary>
    public const int Version = 1;

    /// <summary>Asks for time to run at <paramref name="speed"/> (clamped 0.05 - 1) while <paramref name="holder"/> holds. True = accepted (from
    /// the next control tick). Again for the same holder: only its speed changes. False while the mod is off (the game lacks what it needs).</summary>
    public static bool Hold(string holder, double speed) => Driver.Hold(holder, speed);

    /// <summary>Ends that hold; an unknown holder changes nothing.</summary>
    public static void Release(string holder) => Driver.Release(holder);

    /// <summary>Any hold is active.</summary>
    public static bool Held => Driver.Held;

    /// <summary>The player's own slow motion is on (Shift+L / the window).</summary>
    public static bool On => Driver.On;

    /// <summary>The player's slow speed (the setting).</summary>
    public static double SlowSpeed => Driver.SpeedH / 100.0;
}
