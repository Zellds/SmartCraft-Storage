# Configuration

All options live in BepInEx's [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/),
split into sections. Every option also has its own description inside the
Configuration Manager itself.

On a server that also has this mod installed, every section except
**Hotkeys** and **Crafting** is set by the server: its values are pushed
to each client on connect and shown locked. Those two are display and
input preferences, so they always stay yours. Your config file is not
modified and your own values come back when you disconnect. Solo play,
and servers without the mod, use your own settings throughout.

## Hotkeys

| Option | Default | Description |
|---|---|---|
| `QuickStackShortcut` | Shift + E | Full key combo for quick-stack — click the value and press the combo you want |
| `RestockShortcut` | Ctrl + E | Full key combo for restock — click the value and press the combo you want |
| `LockClickShortcut` | Alt | Key(s) held while left-clicking an item to toggle its lock |
| `RestockMarkClickShortcut` | Alt + Ctrl | Key(s) held while left-clicking an item to mark it for restock — independent of `LockClickShortcut`, does not require it to also be held |

## Crafting (display)

| Option | Default | Description |
|---|---|---|
| `ShowAvailableAmounts` | on | Show how much of each ingredient you can actually spend, in brackets after the required amount (e.g. `10(34)`), in both the crafting panel and the build HUD |
| `AvailableAmountFormat` | `Compact` | How that amount is written — see below. Ignored while `ShowAvailableAmounts` is off |

The ingredient slot is only so wide, so the label is shrunk to fit it whichever format
you pick. The formats differ in how much shrinking they ask for:

| Format | Looks like | Notes |
|---|---|---|
| `Compact` | `10(1.1k)` | Narrowest. Abbreviates past a thousand — `1.1k`, `25k`, `2.1M` |
| `Exact` | `10(1087)` | The number in full, in the same small brackets |
| `Spaced` | `10 (1087)` | The number in full at the game's own text size. Widest, and the most likely to be shrunk on a crowded recipe |

## Radii (storage/restock/crafting-from-chest)

| Option | Default | Description |
|---|---|---|
| `QuickStackRadius` | 20m | Radius in which quick-stack and restock search for chests |
| `CraftingChestRadius` | 20m | Radius in which crafting/building considers items from nearby chests |

## Stations (radii and per-behavior on/off)

| Option | Default | Description |
|---|---|---|
| `FireplaceRadius` | 10m | Radius for fireplaces/torches/hearths |
| `SmelterKilnRadius` | 10m | Radius shared between smelters and charcoal kilns |
| `CookingStationRadius` | 10m | Radius for cooking stations |
| `BeehiveRadius` | 10m | Radius in which beehives search for a chest to store honey |
| `FermenterRadius` | 10m (max 25m) | Radius in which fermenters search for bases and store the finished product |
| `FireplaceAutoRefuel` | on | Fireplaces automatically pull fuel |
| `SmelterAutoRefuel` | on | Smelters automatically pull ore/fuel |
| `SmelterAutoCollect` | on | Smelters store their output in a chest |
| `KilnAutoRefuel` | on | Kilns automatically pull wood |
| `KilnAutoCollect` | on | Kilns store/redirect the coal they produce |
| `CookingStationAutoRefuel` | on | Cooking stations automatically pull raw food/fuel |
| `CookingStationAutoCollect` | on | Cooking stations collect and store on their own |
| `BeehiveAutoCollect` | off | Beehives harvest honey on their own and store it in a chest |
| `FermenterAutoProcess` | on | Fermenters auto-pull a base and auto-collect the finished product |

## Charcoal kiln (kiln-specific tuning)

| Option | Default | Description |
|---|---|---|
| `KilnWoodBuffer` | 3 | Wood level kept in the internal queue (not the kiln's max capacity) |
| `KilnMaxCoalInChest` | 50 | Coal cap in nearby chests before pausing new wood pulls |
| `KilnFeedStrategy` | `LeastFuelFirst` | How to pick which nearby smelter to feed first: `LeastFuelFirst` or `Nearest` |
| `KilnRegularWoodOnly` | on | Only pull regular Wood, skipping Fine Wood/Core Wood (all convert at the same rate) |

## Animals (automatic feeding)

| Option | Default | Description |
|---|---|---|
| `AnimalFeederRadius` | 20m | Radius in which hungry tameable animals search nearby chests for food |
| `AnimalAutoFeed` | on | Tameable animals automatically pull compatible food from nearby chests |

## Fermenter (fermenter-specific tuning)

| Option | Default | Description |
|---|---|---|
| `FermenterDurationOverride` | 0 (off) | Override the fermenting time in seconds; `0` keeps the fermenter's own vanilla duration |

## Plant Harvest (In Test)

Off by default on purpose — see the note on the [Stations](3-Stations.md) page.

| Option | Default | Description |
|---|---|---|
| `PlantHarvestRadius` | 10m (max 25m) | Radius around the player in which ripe crops are auto-harvested |
| `PlantAutoHarvest` | **off** | Automatically harvests ripe cultivated crops near the player into the nearest chest |
