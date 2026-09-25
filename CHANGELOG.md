# Changelog

## 0.10.0
- Added French translation for quick-stack and restock messages (community contribution by [Merkur39](https://github.com/Zellds/SmartCraft-Storage/pull/21))

## 0.9.0
- Fixed a rare multiplayer bug where a station could overwrite a chest's real contents with a stale, out-of-date view of it right when taking ownership, silently losing whatever another player had just stored there (community contribution by [ManuelROAL](https://github.com/Zellds/SmartCraft-Storage/pull/19))
- Stations now prefer a chest that already holds the item they're producing over a closer empty one, so sorted storage stays sorted. New `ChestOutputStrategy` setting to go back to always using the nearest chest with room. Also fixed the fermenter and plant harvest occasionally logging a harmless-but-noisy error when the closest chest was full (community contribution by [uy8Uk4N56G](https://github.com/Zellds/SmartCraft-Storage/pull/17))

## 0.8.0
- Fixed the available-amount text (`10(34)`) overflowing into the next ingredient slot on crowded recipes; it now shrinks to fit instead
- Added `AvailableAmountFormat` to choose how that number is written: `Compact` (abbreviates past a thousand, e.g. `1.1k`), `Exact` (the full number), or `Spaced` (the full number at the original size, like before this update) (community contribution by [uy8Uk4N56G](https://github.com/Zellds/SmartCraft-Storage/pull/15))

## 0.7.0
- `Alt + left-click` locks on chest stacks are now reserved from crafting, building, station fuel/inputs, and Epic Loot's Enchanter, so a locked stack is never spent automatically. Locked equipment, including what you're carrying, is also protected from Epic Loot's Sacrifice: a selection that includes a locked item is canceled instead of consuming it (community contribution by [EremesNG](https://github.com/Zellds/SmartCraft-Storage/pull/14))
- Fixed lock and restock marks getting left behind in empty inventory slots when other UI mods (such as Shield Me Bruh) copy item icons. Marks also now follow items moved into ExtraSlots and keep their size when MyLittleUI scales icons (community contribution by [EremesNG](https://github.com/Zellds/SmartCraft-Storage/pull/13))

## 0.6.0
- If you also have Epic Loot installed, the Enchanter can now use materials from nearby chests too, the same way crafting and building already do. Not required, only kicks in when Epic Loot is present

## 0.5.1
- Fixed an edge case where undoing a partial automatic beehive collection (when a nearby chest couldn't fit everything) could remove honey that was already sitting in the chest instead of just the honey that had been added (community contribution by [ManuelROAL](https://github.com/Zellds/SmartCraft-Storage/pull/9))

## 0.5.0
Community contribution by [uy8Uk4N56G](https://github.com/Zellds/SmartCraft-Storage/pull/6):
- Further reduced stutter/freezing while crafting or building near chests, building on the fix from 0.4.1
- Fixed a rare multiplayer issue where writing to a chest at the same moment another player opened it could cause problems
- Crafting and building now show how much of each material you actually have available (inventory plus nearby chests), not just how much is needed. Toggle this off in the settings if you prefer the old look
- Cleaned up some unnecessary warnings that could show up in the mod's log
- Now requires BepInExPack 5.4.2350 or newer

## 0.4.2
- Fixed a beehive item duplication bug: if the automatic honey collection could only partially fit the harvested honey into nearby chests, the leftover was also duplicated on the ground instead of just the leftover being dropped
- Automatic beehive collection now leaves the honey queued in the hive (instead of dropping any of it) when no nearby chest can fit it all; manually interacting with the hive still drops the leftover on the ground as usual

## 0.4.1
- Fixed a performance issue where crafting/building from nearby chests could re-scan for chests dozens of times per frame while the crafting panel or the build piece list was open, potentially stalling the host long enough to disconnect other players. Nearby-chest results are now cached for 0.1s and refreshed immediately after an actual consumption, instead of re-scanning on every check

## 0.4.0
- Fermenters now auto-pull any mead/potion base from nearby chests and auto-collect the finished product once ready, with their own radius (`FermenterRadius`, max 25m) and an optional duration override (`FermenterDurationOverride`, 0 = keep the vanilla duration)
- New experimental feature, off by default: `PlantAutoHarvest` auto-harvests ripe cultivated crops near the player into the nearest chest (its own radius, `PlantHarvestRadius`, max 25m); marked in test since the game only exposes one flag to tell a farmed crop apart from a wild pickable

## 0.3.0
- Beehives now harvest honey automatically as soon as it's ready and store it in the nearest chest, no need to visit the hive (`BeehiveRadius`/`BeehiveAutoCollect`)
- Charcoal kilns now default to pulling only regular Wood from nearby chests instead of any wood type, since Fine Wood and Core Wood convert to coal at the same rate and burning them was pure waste (configurable via `KilnRegularWoodOnly`)

## 0.2.0
- Nearby chests are now searched nearest-first, so automations that stop at the first usable chest prefer the closest one (community contribution by [BearFlinn](https://github.com/Zellds/SmartCraft-Storage/pull/1))
- Servers running this mod can now set gameplay config (radii, all station toggles, kiln settings, animal feeder) for every connecting client; hotkeys always stay per-player (community contribution by [BearFlinn](https://github.com/Zellds/SmartCraft-Storage/pull/2))

## 0.1.3
- Hotkeys are now fully configurable: `QuickStackShortcut`/`RestockShortcut` let you set any key combo via the Configuration Manager (click and press), no fixed base key
- Lock and restock-mark click combos (`LockClickShortcut`/`RestockMarkClickShortcut`) are also configurable, and fully independent of each other (marking for restock no longer requires the lock combo to also be held)
- Config section names are now all in English for consistency
- Quick-stack and restock on-screen messages are now localized (English, Portuguese-Brazilian, Spanish)
- Removed the on-screen action hint that was tied to the old fixed hotkey registration

## 0.1.2
- Set the website link (GitHub repo) in the package metadata
- Trimmed the README down to a quick reference; full docs moved to the wiki

## 0.1.1
- Declared BepInExPack_Valheim as an explicit dependency (was only pulled transitively through Jotunn)

## 0.1.0
- Initial release
- Quick-stack (`Shift + E`), item lock (`Alt + left-click`), and restock (`Ctrl + E`, mark items with `Alt + Ctrl + left-click`)
- Crafting and building using materials from nearby chests
- Repair-all at crafting stations in one click
- Automatic fuel/ingredient pulling and output collection for fireplaces, smelters, charcoal kilns, and cooking stations
- Automatic feeding of tameable animals from nearby chests
