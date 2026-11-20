// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.4.0 kill cam: the pure rules (no game types), shared by the tick system (KillCam.cs) and the fuzz harness.
//   Judge   what one landed hit did to its victim, from Meitou's pre-hit flags (CombatHit.VictimWasAlive / VictimWasIncapacitated) and the
//           victim's state read right after: a kill (alive -> dead, a downed victim finished off included), a knockout (standing -> down,
//           not dead), or a limb (more arms / legs severed than before this hit). Judged on the blow itself: telling a kill from a KO
//           for good needs a settle window (the dead flag can land a little after the hit), but slow motion only needs "went down on
//           this hit", which is known at once.
//   Window  how long the slow moment lasts in real time: a new trigger extends it, never shortens it.
namespace PZA.SlowMotion.Meitou;

public static class KillCamCore
{
    public enum Kind { None, Kill, Knockout, Limb, Dodge }

    /// <summary>Slowest and fastest kill-cam speed, longest moment (seconds of real time).</summary>
    public const double MinSpeed = 0.10, MaxSpeed = 0.60, MinSeconds = 0.25, MaxSeconds = 3.0;

    /// <summary>What the hit did. <paramref name="severedBefore"/> = -1: this victim's limbs were never read before this hit (then a limb
    /// counts only when the part this hit landed on is the one now severed: <paramref name="hitPartSevered"/>).
    /// 0.4.1 (the user, 2026-11-19): each kind can be switched off (<paramref name="kills"/>, <paramref name="knockouts"/>,
    /// <paramref name="limbs"/>); a hit that did two things (a killing blow that also took an arm) still counts for the kind that is on.</summary>
    public static Kind Judge(bool wasAlive, bool wasIncapacitated, bool deadNow, bool downNow, int severedBefore, int severedNow, bool hitPartSevered,
        bool kills = true, bool knockouts = true, bool limbs = true)
    {
        if (!wasAlive) return Kind.None;   // hits on a corpse: never a moment
        if (kills && deadNow) return Kind.Kill;
        if (knockouts && !wasIncapacitated && downNow && !deadNow) return Kind.Knockout;
        if (limbs && (severedBefore >= 0 ? severedNow > severedBefore : hitPartSevered && severedNow > 0)) return Kind.Limb;
        return Kind.None;
    }

    /// <summary>0.4.2 (the user, 2026-11-19): whose hit makes a moment. A selected attacker always counts; a selected VICTIM (your own
    /// character killed, knocked out or losing a limb) only with <paramref name="ownOn"/>. Hitting yourself never counts.</summary>
    public static bool Counts(bool attackerSelected, bool victimSelected, bool sameCharacter, bool ownOn) =>
        !sameCharacter && (attackerSelected || (ownOn && victimSelected));

    /// <summary>0.4.2: a selected character dodged since the last look: Meitou's own running count of dodges made
    /// (CombatSystem.StateOf(id).Dodges) went up. <paramref name="before"/> = -1: first look (no moment); a count that went down (a load, a
    /// new character in the slot) is only a new start.</summary>
    public static bool Dodged(int before, int now) => before >= 0 && now > before;

    /// <summary>The kill-cam speed in hundredths from the setting (clamped; NaN -> the default 0.25).</summary>
    public static int SpeedH(double speed) =>
        (int)Math.Round(Math.Clamp(double.IsFinite(speed) ? speed : 0.25, MinSpeed, MaxSpeed) * 100);

    /// <summary>The moment's length in stopwatch ticks from the setting (clamped; NaN -> 1 s).</summary>
    public static long Length(double seconds, long frequency) =>
        (long)(Math.Clamp(double.IsFinite(seconds) ? seconds : 1.0, MinSeconds, MaxSeconds) * frequency);

    /// <summary>The end of the moment after a trigger at <paramref name="now"/>: extended, never shortened.</summary>
    public static long Extend(long until, long now, long length) => Math.Max(until, now + length);
}
