// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// The rules, with no game types (tools\check tests them directly). Same rules as the RE_Kenshi plugin's src/SlowCore.h: speeds are whole
// hundredths, so 0.10, 0.50 and 0.60 are exact; slow motion never "returns" to a slow speed or to a pause.
namespace PZA.SlowMotion.Meitou;

public static class SlowCore
{
    /// <summary>The slider range and step, in hundredths (0.10 - 0.60 in 0.05 steps).</summary>
    public const int MinH = 10, MaxH = 60, StepH = 5;

    /// <summary>A speed in hundredths, clamped to the range and snapped to the nearest step (ties go up).</summary>
    public static int Snap(int h)
    {
        if (h < MinH) h = MinH;
        if (h > MaxH) h = MaxH;
        int k = (h - MinH + StepH / 2) / StepH;
        int s = MinH + k * StepH;
        return s > MaxH ? MaxH : s;
    }

    /// <summary>A speed setting (e.g. 0.3 from the settings page) in hundredths, snapped; NaN or infinity -> 0.50.</summary>
    public static int FromSpeed(double speed) => double.IsFinite(speed) ? Snap((int)Math.Round(speed * 100)) : 50;

    /// <summary>One step faster (+1) or slower (-1) along the grid, held at the ends.</summary>
    public static int Step(int h, int dir) => Snap(Snap(h) + (dir > 0 ? StepH : dir < 0 ? -StepH : 0));

    /// <summary>The speed to go back to when slow motion goes off: the speed it was switched on from, unless that was itself slow or a pause
    /// (or not a number), then normal speed.</summary>
    public static double ReturnSpeed(double current) => double.IsFinite(current) && current >= 1 ? current : 1;

    /// <summary>True when the clock no longer runs at our speed while slow motion is on: the player picked one of Meitou's speeds (buttons,
    /// F2 - F4, faster / slower). A pause (speed 0, or a requested pause) is not a change: the pause key resumes at our speed.</summary>
    public static bool ChangedOutside(bool on, int ourH, double clockSpeed, bool paused) =>
        on && !paused && clockSpeed > 0 && Math.Abs(clockSpeed - ourH / 100.0) > 0.004;

    /// <summary>The hotkey chord's memory between control ticks.</summary>
    public struct Chord
    {
        public bool Held, Stepped;
    }

    public const int None = 0, Toggle = 1, Faster = 2, Slower = 3;

    /// <summary>
    /// One control tick of the hotkey: <paramref name="started"/> = the letter went down this tick with Shift held (a fresh press, so a key
    /// Windows reports as held never starts it), <paramref name="held"/> = letter and Shift still both down, <paramref name="plus"/> /
    /// <paramref name="minus"/> = + or - went down this tick. Tapping + / - while the chord is held steps the speed; letting go of a chord
    /// with no step toggles slow motion (as the plugin: the toggle fires on release). Both + and - in one tick: + wins.
    /// </summary>
    public static int ChordStep(ref Chord c, bool started, bool held, bool plus, bool minus)
    {
        if (!c.Held)
        {
            if (!started) return None;
            c.Held = true; c.Stepped = false;
        }
        if (held)
        {
            if (plus) { c.Stepped = true; return Faster; }
            if (minus) { c.Stepped = true; return Slower; }
            return None;
        }
        bool stepped = c.Stepped;
        c = default;
        return stepped ? None : Toggle;
    }
}
