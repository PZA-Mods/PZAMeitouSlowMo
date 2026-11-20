# PZA's SlowMo for Meitou (beta 0.4.2)

One key, **Shift+L**, puts Meitou into slow motion, from 0.10x to 0.60x. Press it again to go back to the speed you were playing at. Made
for dramatic fights, screenshots and machinima. A port of PZA's SlowMo for RE_Kenshi.

![PZA's SlowMo in Meitou: a duel and a kill](https://raw.githubusercontent.com/PZA-Mods/PZASlowMo/main/media/slowmo-meitou-duel.gif)

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

## The kill cam (new in 0.4)
An optional, automatic slow-motion moment. When a **selected** character **kills**, **knocks out** or **severs a limb**, time drops to
**0.25x for one second**, then comes back on its own. Another blow during the moment makes it last longer.

![PZA's SlowMo kill cam in Meitou](https://raw.githubusercontent.com/PZA-Mods/PZASlowMo/main/media/slowmo-meitou-killcam.gif)

- **Off by default:** turn it on in **Settings > Mods > PZASlowMo > Kill cam**.
- Speed (0.10x to 0.60x) and length (0.25 to 3 seconds) are adjustable.
- Kills, knockouts and severed limbs switch on and off one by one.
- Two more options, both off by default: a moment when your selected character **dodges** a blow, and when your selected character is the
  one killed, knocked out or losing a limb.
- A pause stays a pause, and the game's own speed buttons always win. If slow motion is already on at a slower speed, the kill cam leaves
  it alone.

## Requires
**Meitou 0.20.1 and newer.** Checked on Meitou 0.20.1, 0.21.0 and 0.22.1; there is no upper version cap any more, so a newer Meitou no longer refuses it.

## Install
- **Workshop:** it comes inside the PZA's SlowMo Workshop item (one item for both games); subscribe, then tick **PZASlowMo** in
  Meitou's mod list.
- **By hand (GitHub):** unpack `PZASlowMo.Meitou-0.4.2-beta.zip` into Kenshi's folder, so you get `Kenshi\mods\PZASlowMo\`, then tick
  **PZASlowMo** in Meitou's mod list. If you also use PZA's SlowMo for RE_Kenshi, it is the same folder on purpose: each game loads only its
  own file.
- It adds no items or world data, so its place in the load order doesn't matter. Safe to remove at any time.

## This is a beta
The first builds were played in Meitou and worked. **This build (the kill cam) has passed its offline checks
on Meitou 0.20.1, 0.21.0 and 0.22.1 but has not been played in game yet.** Expect rough edges.

Known limits:
- The Settings page labels are English only; the window, help and messages are in English, Russian, Chinese and Japanese (machine
  translations, not yet checked by native speakers).

## Planned
Ideas, no dates:
- A **brisk fast-forward** between normal speed and 2x (1.25x / 1.5x), for watching fights and cloth at a quicker pace.

## Reporting a bug
Tell us what happened, how to make it happen again, your Meitou version and your mod list, and attach Meitou's `log.txt` (in your Meitou
folder) copied right after that session. Every line of ours in it starts with `pza.slowmotion`. Post it in its thread on the Meitou Discord or in the Workshop page's Bug reports discussion.

## Licence and source
Free software under the GNU GPL version 3 or later. The source is attached to the release (`-source.zip`). Made by **PZA**. Unofficial
fan-made mod; not affiliated with Lo-Fi Games.
