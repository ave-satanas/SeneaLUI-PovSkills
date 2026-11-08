# SeneaLUI PoV 6.0 Bridge

SeneaL UI 1.2.8 predates Path of Valheiman 6.0. SeneaL hosts PoV's equipment window and replaces the crafting panel and build HUD, so most of 6.0's new UI is hidden or unreachable with it. This plugin connects them:

- **Skills (gem board):** a button beside SeneaL's Appearance button opens PoV's own gem board next to the bag. Drag a gem from the bag onto a cell, right-click a placed gem to lift it, and left-click an aura or effect to toggle or cycle it. Right-clicking an Awakening Chisel opens the board too. Closing the inventory closes the board, and PoV returns a lifted gem to the bag.
- **Quivers:** the PoV doll slot accepts PoV quivers and, when installed, BowsBeforeHoes quivers. BowsBeforeHoes still handles its own equip callbacks and ammo inventory. Equipping one kind takes off the other. BowsBeforeHoes quivers expose up to three PoV arrow cells as well as their own ammo grid.
- **Worn PoV quivers:** use the BackShield_attach joint for position and a character-relative tilt. Fine-tune Body Position and Body Rotation under Quiver in the bridge config. The 2.4.0 joint-local settings are superseded.
- **Halls pins:** preserves the PoV artwork and state overlays in the SeneaL map.
- **Belt lantern cell:** once the craft tree's Belt Lantern node is taken, the cell appears among SeneaL's extra equipment cells. Drop a Dvergr lantern in and click to take it out, as with SeneaL's other PoV cells.
- **Runic Anvil, Book of the Halls and the Halls shop:** these PoV windows draw inside the game's crafting panel, which SeneaL keeps invisible. While one is open, SeneaL's craft panel steps aside and the game's panel shows in SeneaL's craft column. At the anvil, SeneaL's chest panel is hidden too, because PoV shows the anvil stock in its own window. PoV's buttons in these windows (and in its Enchant tab) are tinted gold when usable and grey when not, since PoV's own buttons look the same either way.
- **Ballista count:** SeneaL's build panel shows PoV's «Ballistas n/m» line for the ballista.

Not bridged: PoV 6.0's siege crossbow and bolt magazine cells. PoV retires them in 6.0 — nothing uses them, they accept no items, and PoV empties them into the bag once per session.

It doesn't change SeneaL UI's or Path of Valheiman's files. Each part checks the SeneaL and PoV members it uses at startup. If one has changed in an update, only that part turns off, and the log says which.

Test command (a cheat, after `devcommands`), since PoV's release build strips its own `pt_` test commands: `pov_halls_gate` places a Halls of Oaths gate in front of you, themed for the biome, for testing the Book of the Halls. `spawn PoV_HallsGate` gives a gate without the theme and site PoV sets up, so it doesn't work. To find other prefab names, use Server Devcommands' `search_id <text>`.

## Disclaimer

This mod was written with help from an AI coding assistant (Claude, by Anthropic). Its code came from studying decompiled builds of SeneaL UI and Path of Valheiman, and the original release was tested by hand in one modded single-player setup. The 2.4.1 defaults use the placement checked in PoVPlus. Existing configuration values take precedence.

This mod is provided "as is", without warranty of any kind, express or implied. That includes any warranty of merchantability, fitness for a particular purpose, or compatibility with other mods or with future versions of SeneaL UI, Path of Valheiman or Valheim. You use it entirely at your own risk. The author accepts no liability for any loss or damage arising from its use. That includes lost or corrupted characters, worlds, items or save data, crashes, or problems with other mods.

Back up your characters and worlds before installing. This is an unofficial, fan-made mod, not affiliated with or endorsed by the authors of SeneaL UI or Path of Valheiman, or by Iron Gate. Please don't report problems with it to them.
