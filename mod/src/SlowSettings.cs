// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// The Settings > Mods group (MeitouClient docs/modding.md "Settings"). The page sets these on the interface thread, outside the tick; the window's
// buttons, drags and the mouse wheel change Speed, Tiny, the positions and the opacity through the same group (Driver.SetSetting), so the page shows them.
using Meitou.Modding;

namespace PZA.SlowMotion.Meitou;

public sealed class SlowSettings
{
    /// <summary>The slow speed (0.10 - 0.60, snapped to 0.05 when used).</summary>
    [Setting(Strings.En.SpeedLabel, Hint = Strings.En.SpeedHint, Min = 0.10, Max = 0.60, Step = 0.05)]
    public double Speed { get; set; } = 0.50;

    /// <summary>The window is shown (off: the hotkey only).</summary>
    [Setting(Strings.En.WindowLabel, Hint = Strings.En.WindowHint)]
    public bool ShowWindow { get; set; } = true;

    /// <summary>Only the tiny button.</summary>
    [Setting(Strings.En.TinyLabel, Hint = Strings.En.TinyHint, VisibleWhen = nameof(ShowWindow))]
    public bool Tiny { get; set; }

    /// <summary>Where the window was left: its offset from its first place, as a fraction of the screen (PZA's Meitou UI saves it after a drag,
    /// so it survives a resolution change). 0.2.0 names: the 0.1.x percentages are not read again.</summary>
    [Setting(Strings.En.XLabel, Hint = Strings.En.XHint, Min = -1, Max = 1, Step = 0.001, VisibleWhen = nameof(ShowWindow))]
    public double WindowPlaceX { get; set; }

    [Setting(Strings.En.YLabel, Hint = Strings.En.YHint, Min = -1, Max = 1, Step = 0.001, VisibleWhen = nameof(ShowWindow))]
    public double WindowPlaceY { get; set; }

    /// <summary>The tiny button's top-left as a fraction of the screen (default the top-right corner, out of the HUD's way). Drag it to move it.</summary>
    [Setting(Strings.En.TinyXLabel, Hint = Strings.En.TinyXHint, Min = 0, Max = 1, Step = 0.001, VisibleWhen = nameof(ShowWindow))]
    public double TinyPlaceX { get; set; } = 1;

    [Setting(Strings.En.TinyYLabel, Hint = Strings.En.TinyYHint, Min = 0, Max = 1, Step = 0.001, VisibleWhen = nameof(ShowWindow))]
    public double TinyPlaceY { get; set; } = 0.06;

    /// <summary>The tiny button's opacity, 0.35 (faint, never invisible) to 1 (UI rule 6). Mouse wheel over the tiny button changes it.</summary>
    [Setting(Strings.En.OpacityLabel, Hint = Strings.En.OpacityHint, Min = 0.35, Max = 1, Step = 0.05, VisibleWhen = nameof(ShowWindow))]
    public double TinyOpacity { get; set; } = 1;

    /// <summary>A message on Meitou's message line when slow motion turns on or off (the plugin's Popups=1).</summary>
    [Setting(Strings.En.PopupsLabel, Hint = Strings.En.PopupsHint)]
    public bool Popups { get; set; } = true;

    /// <summary>auto = the game's language.</summary>
    [Setting(Strings.En.LangLabel, Hint = Strings.En.LangHint, Options = ["auto", "en", "ru", "zh", "ja"])]
    public string Language { get; set; } = "auto";
}
