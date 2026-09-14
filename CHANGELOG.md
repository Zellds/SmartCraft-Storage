# Changelog

## Unreleased
- Item counts from nearby chests are now reused for the rest of the frame instead of being recounted per query. The game asks the same question several times per frame by design — `SetupRequirement` counts an ingredient to colour the label and the bracket then counts it again, and `HaveRequirementItems` counts every ingredient once per quality level for every recipe whenever the crafting list refreshes. Only the chest half is cached: your own inventory is still counted live on every call, and the chest total is dropped at the end of the frame, when the set of nearby chests changes, and whenever this mod writes to a container
- Stations no longer make the game log `Trying to add item to occupied slot -1, -1` as an error while storing their output. `Inventory.AddItem` logs that when a container is full rather than just declining, so a row of full chests filled the log with errors that were not errors; the containers are now asked first, using the same rule `AddItem` applies (a free slot, or free stack space at the current world level)
- The restock-marked item list is no longer split and rebuilt into a set on every frame the inventory is open, which was this mod's largest per-frame allocation
- Multiplayer: a chest is now re-checked for being free, permitted and unwarded immediately before anything is written to it, instead of only when it was found. `ClaimOwnership()` always succeeds, so that check was the only thing keeping the automations out of a chest another player had just opened
- `scripts/package.ps1` builds the installable zip in one command; bumped the pinned BepInEx dependency to 5.4.2350
- Ingredient rows now show how much you can actually spend in brackets after the required amount (`10 (34)`), in both the crafting panel and the build HUD; toggle with the new `Crafting/ShowAvailableAmounts` option
- The nearby-chest lookup is now cached at the source (`NearbyContainers.Find`), per origin and radius, so every feature benefits rather than only the crafting/building path fixed in 0.4.1; the physics sweep is also restricted to the layers containers can be on and no longer allocates on every call
- Fireplaces already skipped the chest search when full; smelters, kilns and cooking stations now do too, so a base full of topped-up stations no longer sweeps for chests once a second each

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
