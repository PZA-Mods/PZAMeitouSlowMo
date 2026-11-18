// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// Every user-visible string, English compiled in, other languages from UTF-8 files in the mod folder (localization plan, text rules 1-6):
//   lang\<code>.txt          the RE_Kenshi plugin's own files (same folder when both builds share one mod folder): the keys both builds use
//   lang\meitou.<code>.txt   the keys only this build has
// Format and rules as the plugin's StringTable.h: "key = text", '#' / ';' comments, escapes \n \\ \= \#; a line whose {1}..{9} set differs from the
// English line is refused (English shown); unknown keys are skipped (the plugin's file holds keys this build does not use). A translation is
// never a format string: {n} is replaced here. Log lines stay English (they are for us). Language: Meitou's own (Loc.Language.Code, "ru_RU" ...),
// or the Language setting.
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PZA.SlowMotion.Meitou;

public static class Strings
{
    /// <summary>English strings that must be constants (setting attributes). The settings page shows these in English for now.</summary>
    public static class En
    {
        public const string SpeedLabel = "Slow speed";
        public const string SpeedHint = "How slow slow motion runs: 0.10x to 0.60x in 0.05 steps. The window's - and + and Shift+L with + / - change it too.";
        public const string WindowLabel = "Show the window";
        public const string WindowHint = "The small SlowMo window. Off: Shift+L only.";
        public const string TinyLabel = "Tiny window";
        public const string TinyHint = "The window is minimized to its tiny button (the window's - , a double click on its title and the tiny button's + switch this too).";
        public const string XLabel = "Window place across";
        public const string XHint = "Where you left the window (a fraction of the screen from its first place). Drag the window by its title to move it.";
        public const string YLabel = "Window place down";
        public const string YHint = "As above, down.";
        public const string TinyXLabel = "Tiny button across";
        public const string TinyXHint = "Where the tiny button sits, from the left edge (1 = right edge). Drag the tiny button to move it.";
        public const string TinyYLabel = "Tiny button down";
        public const string TinyYHint = "Where the tiny button sits, from the top (0 = top).";
        public const string OpacityLabel = "Tiny button opacity";
        public const string OpacityHint = "0.35 (faint) to 1 (solid). The mouse wheel over the tiny button changes it.";
        public const string PopupsLabel = "On-screen messages";
        public const string PopupsHint = "A message when slow motion turns on or off (\"Slow motion 0.30x\").";
        public const string LangLabel = "Language";
        public const string LangHint = "auto = the game's language. Needs a restart of the game.";
    }

    /// <summary>The English table: (key, text). Keys shared with the plugin keep the plugin's names.</summary>
    public static readonly (string Key, string En)[] Table =
    [
        ("win.title", "PZA's SlowMo"),
        ("toggle.on", "Slow motion: ON ({1}x)"),
        ("toggle.off", "Slow motion: off ({1}x)"),
        ("speed.row", "Speed {1}x  ({2} - {3})"),
        ("meitou.mini.on", "SlowMo ON {1}x"),    // the user, 2026-11-17: SLOW / slow (capitals = on) was not clear; keep the speed
        ("meitou.mini.off", "SlowMo OFF {1}x"),
        ("meitou.msg.on", "SlowMo ON"),     // the user, 2026-11-17: just ON / OFF, no speed
        ("meitou.msg.off", "SlowMo OFF"),
        ("help.1", "PZA's SlowMo slows the whole game down for dramatic moments, then puts it back."),
        ("help.2", "The top button switches slow motion on and off (also Shift+L). The slider, - and + pick how slow. Hold Shift+L and tap + or - to change the speed from the keyboard."),
        ("meitou.help.3", "Meitou's own speed buttons (or F2 to F4) while it is on hand control back to the game. Plain L still makes the selection rest: only Shift+L is ours."),
        ("meitou.help.4", "The - at the top right, or a double click on the title, shrinks the window to a tiny button: click it to switch slow motion, double-click it or press its + to open the window again, drag it to move it, mouse wheel for opacity. Everything is saved."),
        ("meitou.help.5", "Other PZA mods may slow time briefly, e.g. WASD's dials: a pause stays a pause, and your own speed buttons always win."),
        ("meitou.tab.main", "SLOW MOTION"),
        ("meitou.tab.help", "HELP"),
        ("tip.toggle", "Slow motion on/off (also Shift+L)."),
        ("tip.minus", "Slower (one step)."),
        ("tip.plus", "Faster (one step)."),
    ];

    static readonly Dictionary<string, string> s_en = Table.ToDictionary(t => t.Key, t => t.En, StringComparer.Ordinal);
    static readonly Dictionary<string, string> s_text = new(StringComparer.Ordinal);

    /// <summary>The language in use: "en", "ru", "zh" or "ja".</summary>
    public static string Lang { get; private set; } = "en";

    /// <summary>"ru_RU" / "ru" / "RU" -> "ru"; anything else (de, fr, ...) -> null.</summary>
    public static string? CodeFor(string? v)
    {
        if (string.IsNullOrEmpty(v) || v.Length < 2) return null;
        string c = v[..2].ToLowerInvariant();
        return c is "en" or "ru" or "zh" or "ja" ? c : null;
    }

    /// <summary>Picks the language and loads its files; returns the log lines (English).</summary>
    public static List<string> Init(string modFolder, string gameLanguage, string setting)
    {
        var log = new List<string>();
        s_text.Clear();
        string code = CodeFor(gameLanguage) ?? "en";
        if (!string.Equals(setting, "auto", StringComparison.OrdinalIgnoreCase))
            code = CodeFor(setting) ?? code;
        Lang = code;
        log.Add($"language: Meitou runs in '{gameLanguage}', setting '{setting}' -> {code}");
        if (code == "en") return log;

        foreach (string file in new[] { Path.Combine(modFolder, "lang", code + ".txt"), Path.Combine(modFolder, "lang", "meitou." + code + ".txt") })
        {
            if (!File.Exists(file)) { log.Add($"language: {Path.GetFileName(file)} not found - English for its keys"); continue; }
            var (ok, refused) = Load(File.ReadAllBytes(file), s_text);
            log.Add($"language: {Path.GetFileName(file)}: {ok} texts used, {refused.Count} line(s) refused" + (refused.Count > 0 ? ": " + string.Join(", ", refused.Take(10)) : ""));
        }
        var missing = Table.Where(t => !s_text.ContainsKey(t.Key)).Select(t => t.Key).ToList();
        if (missing.Count > 0) log.Add("language: shown in English: " + string.Join(", ", missing));
        return log;
    }

    static readonly Regex Placeholder = new(@"\{([1-9])\}", RegexOptions.CultureInvariant);
    static string PlaceholderSet(string s) => string.Concat(Placeholder.Matches(s).Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x));

    /// <summary>Parses one language file into <paramref name="into"/> (only keys this build knows). Returns the count used and the refused
    /// lines ("line N reason"). Public for the offline check.</summary>
    public static (int Used, List<string> Refused) Load(byte[] data, Dictionary<string, string> into)
    {
        var refused = new List<string>();
        int used = 0;
        string text;
        try { text = new UTF8Encoding(false, true).GetString(data); }
        catch (DecoderFallbackException) { refused.Add("file: not valid UTF-8"); return (0, refused); }
        if (text.Length > 0 && text[0] == '﻿') text = text[1..];
        string[] lines = text.Split('\n');
        for (int n = 0; n < lines.Length; n++)
        {
            string line = lines[n].TrimEnd('\r').Trim(' ', '\t');
            if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;
            int eq = line.IndexOf('=');
            if (eq <= 0) { refused.Add($"line {n + 1} no key"); continue; }
            string key = line[..eq].TrimEnd(' ', '\t');
            if (!s_en.TryGetValue(key, out var english)) continue;   // the plugin's keys this build doesn't use
            string raw = line[(eq + 1)..].TrimStart(' ', '\t');
            var sb = new StringBuilder();
            bool bad = raw.Length == 0;
            for (int i = 0; i < raw.Length && !bad; i++)
            {
                char c = raw[i];
                if (c == '\t' || (c < 0x20)) { bad = true; break; }
                if (c != '\\') { sb.Append(c); continue; }
                if (i + 1 >= raw.Length) { bad = true; break; }
                char d = raw[++i];
                if (d == 'n') sb.Append('\n'); else if (d is '\\' or '=' or '#') sb.Append(d); else bad = true;
            }
            if (bad) { refused.Add($"line {n + 1} {key} (bad text or escape)"); continue; }
            string val = sb.ToString();
            if (PlaceholderSet(val) != PlaceholderSet(english)) { refused.Add($"line {n + 1} {key} (placeholders differ from English)"); continue; }
            if (into.ContainsKey(key)) { refused.Add($"line {n + 1} {key} (duplicate, first kept)"); continue; }
            into[key] = val; used++;
        }
        return (used, refused);
    }

    /// <summary>The text for a key in the language in use (English when missing; the key itself when unknown, so a gap shows).</summary>
    public static string T(string key) => s_text.TryGetValue(key, out var s) ? s : s_en.TryGetValue(key, out var e) ? e : key;

    /// <summary>T(key) with {1}..{3} replaced (never a format string).</summary>
    public static string TF(string key, params string[] args)
    {
        string s = T(key);
        for (int i = 0; i < args.Length && i < 9; i++) s = s.Replace("{" + (i + 1).ToString(CultureInfo.InvariantCulture) + "}", args[i]);
        return s;
    }

    /// <summary>A speed in hundredths as the window shows it: "0.30".</summary>
    public static string Speed(int h) => (h / 100.0).ToString("0.00", CultureInfo.InvariantCulture);
}
