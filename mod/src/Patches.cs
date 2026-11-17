// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// The Harmony patches. Each body is wrapped: a throw is logged (at most once a second) and never reaches the game. All four run on the host
// thread (frame, input resolve, control tick, key events), one after another, so the Driver's plain statics are safe.
using HarmonyLib;
using Meitou.Game.Host;
using Meitou.Game.Host.Input;

namespace PZA.SlowMotion.Meitou;

public static class Patches
{
    /// <summary>Applies all of them or none; returns one line per patch.</summary>
    public static List<string> Apply(Harmony h)
    {
        var lines = new List<string>();
        try
        {
            h.Patch(Access.Steer, prefix: Hook(nameof(SteerPrefix)));
            lines.Add("patched PlayerInterface.Steer prefix");
            h.Patch(Access.Resolve, postfix: Hook(nameof(ResolvePostfix)));
            lines.Add("patched InputBindings.Resolve postfix");
            h.Patch(Access.SessionTick, postfix: Hook(nameof(TickPostfix)));
            lines.Add("patched WorldSession.Tick postfix");
            h.Patch(Access.HotkeyKey, prefix: Hook(nameof(HotkeyPrefix)));
            lines.Add("patched GameHotkeys.Key prefix (Shift+L no longer also rests the selection)");
        }
        catch
        {
            h.UnpatchAll(h.Id);
            throw;
        }
        return lines;
    }

    static HarmonyMethod Hook(string name) => new(AccessTools.Method(typeof(Patches), name));

    static void SteerPrefix(object __instance)
    {
        try { Driver.Player = __instance; }
        catch (Exception e) { Log.Exception("Steer prefix", e); }
    }

    static void ResolvePostfix(TickInput input)
    {
        try { Driver.OnResolve(input); }
        catch (Exception e) { Log.Exception("Resolve postfix", e); }
    }

    static void TickPostfix(WorldSession __instance)
    {
        try { Driver.OnTick(__instance); }
        catch (Exception e) { Log.Exception("Tick postfix", e); }
    }

    /// <summary>false = skip the game's handler: only for our hotkey's letter with Shift held (Meitou's Rest is plain L).</summary>
    static bool HotkeyPrefix(Key key, bool shift)
    {
        try
        {
            if (!Driver.OwnsKey(key, shift)) return true;
            Counters.Add("rest.blocked_shift_l");
            return false;
        }
        catch (Exception e)
        {
            Log.Exception("GameHotkeys prefix", e);
            return true;
        }
    }
}
