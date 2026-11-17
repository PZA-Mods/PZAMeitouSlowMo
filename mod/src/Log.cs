// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// The mod's log (ModContext.Log) and rule counters: every rule counts when it fires, and a line every 5 s lists the counts that changed
// (adversarial method: a rule that never logs is a failed test). Same shape as PZA Direct Control for Meitou's.
using System.Diagnostics;
using System.Text;

namespace PZA.SlowMotion.Meitou;

public static class Counters
{
    static readonly Dictionary<string, long> s_counts = new(StringComparer.Ordinal);
    static readonly Dictionary<string, long> s_reported = new(StringComparer.Ordinal);

    public static void Add(string rule) => s_counts[rule] = s_counts.GetValueOrDefault(rule) + 1;

    public static long Get(string rule) => s_counts.GetValueOrDefault(rule);

    /// <summary>"rule=total(+new)" for each rule that fired since the last call; null when none did.</summary>
    public static string? Changes()
    {
        var sb = new StringBuilder();
        foreach (var (rule, n) in s_counts.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            long before = s_reported.GetValueOrDefault(rule);
            if (n == before) continue;
            sb.Append(sb.Length == 0 ? "" : " ").Append(rule).Append('=').Append(n).Append("(+").Append(n - before).Append(')');
            s_reported[rule] = n;
        }
        return sb.Length == 0 ? null : sb.ToString();
    }
}

public static class Log
{
    public static Action<string> Sink = _ => { };
    static long s_lastReport = Stopwatch.GetTimestamp();
    static long s_lastException;

    public static void Line(string text) => Sink("PZA's SlowMo: " + text);

    /// <summary>Once a control tick: the 5 s counter line, when anything fired.</summary>
    public static void Tick()
    {
        long now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(s_lastReport, now).TotalSeconds < 5) return;
        s_lastReport = now;
        if (Counters.Changes() is { } changes) Line("[" + (Driver.On ? "on" : "off") + "] " + changes);
    }

    /// <summary>A throw from a patch or the window: counted always, logged at most once a second.</summary>
    public static void Exception(string where, Exception e)
    {
        Counters.Add("error." + where.Replace(' ', '_'));
        long now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(s_lastException, now).TotalSeconds < 1) return;
        s_lastException = now;
        Line("error in " + where + ": " + e.GetType().Name + ": " + e.Message);
    }
}
