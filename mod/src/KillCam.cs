// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.4.0 kill cam (setting, off by default): when a SELECTED character kills, knocks out or severs a limb, time drops to the kill-cam speed for
// a moment of real time, through the same hold door other PZA mods use (SlowMoApi; holder "pza.slowmotion.killcam"), so every hold rule
// stands: a pause stays a pause, the player's speed buttons win, slow motion already slower than the kill cam is left alone.
//   HitResolved   Meitou's public ICombatHitObserver (tick thread, serial, read only): judges the hit (KillCamCore.Judge) when its attacker
//                 is selected and takes the hold. Every hit also notes its victim's severed limbs (the "before" of the next hit on it).
//   HostTick      the control tick (host thread): copies the selection for the tick thread, and releases the hold when the moment is over.
using System.Diagnostics;
using Meitou.Simulation;
using Meitou.Simulation.Bodies;
using Meitou.Simulation.Combat;

namespace PZA.SlowMotion.Meitou;

public sealed class KillCam : ICombatHitObserver
{
    public const string Holder = "pza.slowmotion.killcam";

    /// <summary>The selection, copied on the host thread each control tick (empty while the kill cam is off).</summary>
    static volatile CharacterId[] s_selected = [];
    /// <summary>When the moment ends (Stopwatch ticks); 0 = no moment.</summary>
    static long s_until;
    static bool s_holding;
    /// <summary>Victim key -> severed arms / legs after the last hit on it (tick thread only). Capped: cleared when it grows past the cap.</summary>
    static readonly Dictionary<long, int> s_severed = new();
    const int SeveredCap = 4096;
    static World? s_world;
    /// <summary>0.4.2: each selected character's dodge count at the last look (tick thread only), and the world's combat system.</summary>
    static readonly Dictionary<long, int> s_dodges = new();
    static World? s_combatWorld;
    static CombatSystem? s_combat;
    static bool s_noCombatLogged;

    public void HitResolved(World world, in CombatHit hit)
    {
        if (!ReferenceEquals(world, s_world)) { s_world = world; s_severed.Clear(); }
        var victim = Cold(world, hit.Victim);
        if (victim?.Medical is not { } m || m.Parts.Count <= 0) return;
        int n = Math.Min(m.Parts.Count, 16), sevNow = 0;
        for (int i = 0; i < n; i++)
            if (m.Parts[i].Limb is LimbState.Stump or LimbState.Crushed) sevNow++;
        long key = Key(hit.Victim);
        int sevBefore = s_severed.TryGetValue(key, out int b) ? b : -1;
        if (s_severed.Count >= SeveredCap && sevBefore < 0) { s_severed.Clear(); Counters.Add("killcam.severed_cap"); }
        s_severed[key] = sevNow;

        var st = Driver.Settings;
        if (!st.KillCam) return;
        bool byUs = IsSelected(hit.Attacker), onUs = IsSelected(hit.Victim);
        if (!KillCamCore.Counts(byUs, onUs, hit.Attacker == hit.Victim, st.KillCamOwn)) return;
        bool partSevered = hit.Part >= 0 && hit.Part < n && m.Parts[hit.Part].Limb is LimbState.Stump or LimbState.Crushed;
        var s = Driver.Settings;
        var kind = KillCamCore.Judge(hit.VictimWasAlive, hit.VictimWasIncapacitated, m.Dead, m.Unconscious || m.Dead, sevBefore, sevNow, partSevered,
            s.KillCamKills, s.KillCamKnockouts, s.KillCamLimbs);
        if (kind == KillCamCore.Kind.None) return;
        Trigger(kind, Cold(world, hit.Attacker)?.Name ?? "?", victim.Name ?? "?", sevBefore < 0, own: !byUs);
    }

    /// <summary>0.4.2 dodges: once per tick, after combat's own slow world (anchor End), each selected character's running count of dodges
    /// made (CombatSystem.StateOf, public); a rise is a dodge. Read only.</summary>
    public void SlowWorld(World world)
    {
        if (!Driver.Settings.KillCam || !Driver.Settings.KillCamDodges) { if (s_dodges.Count > 0) s_dodges.Clear(); return; }
        if (!ReferenceEquals(world, s_combatWorld)) { s_combatWorld = world; s_combat = world.Systems.OfType<CombatSystem>().FirstOrDefault(); s_dodges.Clear(); }
        if (s_combat is null) { if (!s_noCombatLogged) { s_noCombatLogged = true; Log.Line("kill cam dodges: this world has no combat system"); } return; }
        var sel = s_selected;
        foreach (var id in sel)
        {
            int now = s_combat.StateOf(id).Dodges;
            long key = Key(id);
            int before = s_dodges.TryGetValue(key, out int b) ? b : -1;
            s_dodges[key] = now;
            if (KillCamCore.Dodged(before, now))
                Trigger(KillCamCore.Kind.Dodge, Cold(world, id)?.Name ?? "?", "-");
        }
        if (s_dodges.Count > 64) s_dodges.Clear();   // the selection changed a lot: start again (a first look makes no moment)
    }

    /// <summary>Takes (or extends) the moment. Public for the offline check.</summary>
    public static void Trigger(KillCamCore.Kind kind, string attacker, string victim, bool noBaseline = false, bool own = false)
    {
        long now = Stopwatch.GetTimestamp();
        long until = KillCamCore.Extend(Interlocked.Read(ref s_until), now, KillCamCore.Length(Driver.Settings.KillCamSeconds, Stopwatch.Frequency));
        Interlocked.Exchange(ref s_until, until);
        double speed = KillCamCore.SpeedH(Driver.Settings.KillCamSpeed) / 100.0;
        bool took = Driver.Hold(Holder, speed);
        string what = kind switch { KillCamCore.Kind.Kill => "kill", KillCamCore.Kind.Knockout => "knockout", KillCamCore.Kind.Dodge => "dodge", _ => "limb" };
        Counters.Add("killcam." + what + (own ? ".own" : "") + (noBaseline && kind == KillCamCore.Kind.Limb ? ".no_baseline" : ""));
        Log.Line($"kill cam: {(kind == KillCamCore.Kind.Dodge ? attacker + " dodged" : attacker + " -> " + victim + ": " + what + (own ? " (our own character)" : ""))}; {speed:0.00}x for {Math.Clamp(Driver.Settings.KillCamSeconds, KillCamCore.MinSeconds, KillCamCore.MaxSeconds):0.##} s{(took ? "" : " (hold refused)")}");
    }

    /// <summary>The control tick (host thread): the selection for the tick thread, and the end of the moment.</summary>
    public static void HostTick(object? player)
    {
        if (!Driver.Settings.KillCam)
        {
            if (s_selected.Length > 0) s_selected = [];
        }
        else if (player is not null && Access.HasSelection)
        {
            try { s_selected = [.. Access.SelectedIds(player)]; }
            catch (Exception e) { Access.DropSelection(); Log.Exception("kill cam selection (kill cam stays off)", e); s_selected = []; }
        }
        long until = Interlocked.Read(ref s_until);
        bool held = until != 0;
        if (held && Stopwatch.GetTimestamp() >= until)
        {
            Interlocked.CompareExchange(ref s_until, 0, until);
            Driver.Release(Holder);
            held = false;
        }
        if (held != s_holding) { s_holding = held; if (!held) Counters.Add("killcam.end"); }
    }

    /// <summary>The moment is running (for the offline check).</summary>
    public static bool Running => Interlocked.Read(ref s_until) != 0;

    static bool IsSelected(CharacterId id)
    {
        if (id.IsNone) return false;
        foreach (var s in s_selected) if (s == id) return true;
        return false;
    }

    /// <summary>Sets the selection directly (the offline check has no player interface).</summary>
    public static void SetSelectionForCheck(params CharacterId[] ids) => s_selected = ids;

    static long Key(CharacterId id) => ((long)id.Slot << 32) | (uint)id.Generation;

    static CharacterCold? Cold(World world, CharacterId id) =>
        !id.IsNone && world.Characters.TryResolveNext(id, out int slot) ? world.Characters.Cold(slot) : null;
}
