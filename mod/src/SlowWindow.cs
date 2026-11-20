// PZA's SlowMo for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.2.0: the window on PZA's Meitou UI (the shared window library, compiled in): a real Kenshi window (Kenshi_WindowCXM: caption, close and
// minimize) with two tabs:
//   SLOW MOTION   the on / off button, the speed line, and - [slider] + on SlowMo's scale (0.10x - 0.60x in 0.05 steps: 11 positions)
//   HELP          the help text, a scrolling list wrapped in the real font (PZA's Meitou UI 0.3.0: it no longer spills in ru / ja)
// and the tiny button (shown while the window is closed: click = slow motion on / off, + = the window, drag it, mouse wheel = opacity). The "-"
// or a double click on the title minimizes the window to it. The window's and the tiny button's places and the opacity are saved in the
// settings. The user, 2026-11-16: "it also lacks a slider, it only has the +/- buttons".
using System.Numerics;
using Meitou.Data.Ui;
using Meitou.Game.Ui;
using PZA.MeitouUi;
using L = PZA.MeitouUi.PzaLayout;

namespace PZA.SlowMotion.Meitou;

public static class SlowWindow
{
    public const string Id = "pza.slowmo";
    public static PzaWindow Window = null!;
    public static PzaTinyButton Tiny = null!;

    static int s_shownH = -1;
    static bool s_started;
    static int s_scrollLogs;

    /// <summary>Slider position (0..10) for a speed in hundredths, and back.</summary>
    public static int Pos(int h) => (SlowCore.Snap(h) - SlowCore.MinH) / SlowCore.StepH;
    public static int FromPos(float pos) => SlowCore.Snap(SlowCore.MinH + SlowCore.StepH * (int)Math.Round(pos));
    public static int Positions => (SlowCore.MaxH - SlowCore.MinH) / SlowCore.StepH + 1;

    // 0.3.1: 0.19 x 0.21 of the screen (was 0.22 x 0.29; the user 2026-11-19: "obnoxiously large compared to our RE_Kenshi version", whose
    // window is about 366 x 164 px at 1080p = 0.19 x 0.15; ours keeps the extra height for its title bar and tab row). 0.2.4: 0.29 (was 0.36).
    // 0.3.2: 0.19 x 0.25 with the rows at their old pixel heights (toggle ~43, speed line ~28, slider row ~49 px at 1080p) and a clear gap
    // under the tab strip: the even shrink to 0.21 squeezed the slider skin and the - / + buttons and put the toggle against the tabs (the user,
    // 2026-11-19: "the menu clearly looks fucked"). Only empty space is cut.
    public static string LayoutXml() => L.Doc(L.WindowMin("Root", "PZA'S SLOWMO", 0.62, 0.12, 0.19, 0.25,
        L.Tabs("Tabs", 0.03, 0.02, 0.94, 0.95,
            ("TabMain", [
                L.Button("Toggle", 0.04, 0.08, 0.92, 0.23),
                L.Text("lbSpeed", 0.05, 0.37, 0.90, 0.15),
                L.Button("Slower", 0.04, 0.59, 0.13, 0.26),
                L.Slider("Slider", 0.19, 0.59, 0.62, 0.26, range: Positions, pos: Pos(Driver.SpeedH)),
                L.Button("Faster", 0.83, 0.59, 0.13, 0.26)]),
            L.HelpList("TabHelp", "lstHelp"))));

    public static void Setup()
    {
        Window = new PzaWindow(Id, LayoutXml)
        {
            Minimizable = true,
            Bind = Bind,
            Event = OnEvent,
            LoadPlace = () => new Vector2((float)Driver.Settings.WindowPlaceX, (float)Driver.Settings.WindowPlaceY),
            SavePlace = p =>
            {
                Driver.SetSetting(nameof(SlowSettings.WindowPlaceX), Math.Round((double)p.X, 3));
                Driver.SetSetting(nameof(SlowSettings.WindowPlaceY), Math.Round((double)p.Y, 3));
                Counters.Add("window.place_saved");
            },
            OpenChanged = open =>
            {
                if (!Driver.Settings.ShowWindow) return;   // closed because the window was switched off: keep the remembered form
                Driver.SetSetting(nameof(SlowSettings.Tiny), !open);
                Counters.Add(open ? "window.full" : "window.tiny");
                if (open) s_shownH = -1;   // the slider follows the speed again
            },
        };
        PzaUi.Add(Window);

        Tiny = new PzaTinyButton("slowmo")
        {
            Caption = () => Strings.TF(Driver.On ? "meitou.mini.on" : "meitou.mini.off", Strings.Speed(Driver.SpeedH)),
            Width = 150,   // "SlowMo OFF 0.30x" at 1080p
            Visible = () => Driver.Settings.ShowWindow && !Window.IsOpen,
            Click = () => { Driver.Queue(Driver.Act.Toggle, "tiny button"); Counters.Add("tiny.click"); },
            // the user, 2026-11-17: a double click opens the window, as the RE_Kenshi plugin; its first click's toggle is undone
            DoubleClick = () => { Driver.UndoToggle("tiny button double click"); Window.Open(); Counters.Add("tiny.double"); },
            OpenFull = () => { Window.Open(); Counters.Add("tiny.plus"); },
            LoadPlace = () => new Vector2((float)Driver.Settings.TinyPlaceX, (float)Driver.Settings.TinyPlaceY),
            SavePlace = p =>
            {
                Driver.SetSetting(nameof(SlowSettings.TinyPlaceX), Math.Round((double)p.X, 3));
                Driver.SetSetting(nameof(SlowSettings.TinyPlaceY), Math.Round((double)p.Y, 3));
                Counters.Add("tiny.moved");
            },
            LoadOpacity = () => Driver.Settings.TinyOpacity,
            SaveOpacity = a => { Driver.SetSetting(nameof(SlowSettings.TinyOpacity), a); Counters.Add("tiny.opacity"); },
        };
        PzaUi.Add(Tiny);
    }

    static void Bind(UiBindings b)
    {
        string sp = Strings.Speed(Driver.SpeedH);
        PzaWindow.Enable(b, "Toggle", "Slower", "Faster");
        b.Text["Root"] = Strings.T("win.title");
        PzaWindow.TabText(b, "Tabs", 0, Strings.T("meitou.tab.main"));
        PzaWindow.TabText(b, "Tabs", 1, Strings.T("meitou.tab.help"));
        b.Text["Toggle"] = Strings.TF(Driver.On ? "toggle.on" : "toggle.off", sp);
        b.Text["lbSpeed"] = Strings.TF("speed.row", sp, Strings.Speed(SlowCore.MinH), Strings.Speed(SlowCore.MaxH));
        b.Text["Slower"] = "-";
        b.Text["Faster"] = "+";
        PzaWindow.Tooltip(b, "Toggle", Strings.T("tip.toggle"));
        PzaWindow.Tooltip(b, "Slower", Strings.T("tip.minus"));
        PzaWindow.Tooltip(b, "Faster", Strings.T("tip.plus"));
        Window.Paragraphs(b, "lstHelp", string.Join("\n\n", Strings.T("help.1"), Strings.T("help.2"), Strings.T("meitou.help.3"), Strings.T("meitou.help.4"), Strings.T("meitou.help.5"), Strings.T("meitou.help.6"), Strings.T("meitou.help.7")));
    }

    static void OnEvent(MyGuiEvent e)
    {
        Counters.Add("event." + e.Action);
        switch (e.Action)
        {
            case "Toggle": Driver.Queue(Driver.Act.Toggle, "window"); break;
            case "Slower": Driver.Queue(Driver.Act.Slower, "window -"); break;
            case "Faster": Driver.Queue(Driver.Act.Faster, "window +"); break;
            case "scroll" when e.Widget == "Slider":
                int h = FromPos(e.Value);
                if (h != Driver.SpeedH)
                {
                    Driver.SetSetting(nameof(SlowSettings.Speed), h / 100.0);   // the control tick follows the setting while slow motion is on
                    s_shownH = h;
                    if (++s_scrollLogs <= 20) Log.Line($"slow speed {Strings.Speed(h)} (the slider)");
                }
                break;
        }
    }

    /// <summary>Once a frame (PzaUi.EachFrame): the window's first opening, the window switched off, the slider following the speed when it
    /// changed elsewhere (Shift+L with + / -, the - / + buttons, the settings page).</summary>
    public static void EachFrame()
    {
        Log.Tick();
        if (!s_started)
        {
            s_started = true;
            if (Driver.Settings.ShowWindow && !Driver.Settings.Tiny) Window.Open();
        }
        if (!Driver.Settings.ShowWindow && Window.IsOpen) Window.Close();
        if (Driver.SpeedH != s_shownH && Window.IsOpen)
        {
            s_shownH = Driver.SpeedH;
            Window.SetSlider("Slider", Pos(Driver.SpeedH));
        }
    }
}
