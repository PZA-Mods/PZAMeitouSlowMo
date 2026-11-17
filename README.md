# PZA's SlowMo for Meitou (beta 0.2.6)

One key, **Shift+L**, puts Meitou into slow motion, from 0.10x to 0.60x. Press it again to go back to the speed you were playing at. Made
for dramatic fights, screenshots and machinima. A port of PZA's SlowMo for RE_Kenshi.

**Download:** the mod zip and its source zip are on the [Releases page](https://github.com/PZA-Mods/PZAMeitouSlowMo/releases). Building from source: [BUILD.md](BUILD.md).

## Hotkeys

| Key | What it does |
|---|---|
| **Shift+L** | Slow motion on / off |
| **Hold Shift+L, tap + or -** | One 0.05 step faster / slower (main keys or numpad) |
| Meitou's speed buttons, F2 - F4 | While slow motion is on, they hand control back to the game |
| **F7** | Hides Meitou's interface and the SlowMo window with it |

Plain **L** stays Meitou's Rest. Shift+L does nothing while you type in a text box.

## The window
A small window in Kenshi's own style: an on / off button, a speed slider (0.10x - 0.60x in 0.05 steps) and a Help tab. The **-** button
(or a double click on the title) shrinks it to a tiny "SlowMo ON 0.30x" button: click it to switch, double-click it to open the window,
drag it anywhere, mouse wheel over it for its opacity. Speed, places and opacity are kept after a restart. More in
**Settings > Mods > PZASlowMo**.

## Requires
**Meitou 0.20.1 up to 0.21.x.** It refuses to load on other Meitou versions.

## Install
- Unpack `PZASlowMo.Meitou-0.2.6-beta.zip` into Kenshi's folder, so you get `Kenshi\mods\PZASlowMo\`, then tick
  **PZASlowMo** in Meitou's mod list. If you also use PZA's SlowMo for RE_Kenshi, it is the same folder on purpose: each game loads only its
  own file.
- It adds no items or world data, so its place in the load order doesn't matter. Safe to remove at any time.

## This is a beta
The first builds were played in Meitou and worked. **This build (the slider window and the kept settings) has passed its offline checks
on Meitou 0.20.1 and 0.21.0 but has not been played in game yet.** Expect rough edges.

Known limits:
- The Settings page labels are English only; the window, help and messages are in English, Russian, Chinese and Japanese (machine
  translations, not yet checked by native speakers).

## Reporting a bug
Tell us what happened, how to make it happen again, your Meitou version and your mod list, and attach Meitou's `log.txt` (in your Meitou
folder) copied right after that session. Every line of ours in it starts with `pza.slowmotion`. Post it in its thread on the Meitou Discord or in the Workshop page's Bug reports discussion.

## Licence and source
Free software under the GNU GPL version 3 or later. The source is attached to the release (`-source.zip`). Made by **PZA**. Unofficial
fan-made mod; not affiliated with Lo-Fi Games.
