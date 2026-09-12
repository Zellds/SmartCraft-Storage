# Configuration

All options live in BepInEx's [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/),
split into five sections. Every option also has its own description
inside the Configuration Manager itself.

On a server that also has this mod installed, every section except
**Hotkeys** is set by the server: its values are pushed to each client on
connect and shown locked. Your config file is not modified and your own
values come back when you disconnect. Solo play, and servers without the
mod, use your own settings throughout.

## Hotkeys

| Option | Default | Description |
|---|---|---|
| `QuickStackShortcut` | Shift + E | Full key combo for quick-stack — click the value and press the combo you want |
| `RestockShortcut` | Ctrl + E | Full key combo for restock — click the value and press the combo you want |
| `LockClickShortcut` | Alt | Key(s) held while left-clicking an item to toggle its lock |
| `RestockMarkClickShortcut` | Alt + Ctrl | Key(s) held while left-clicking an item to mark it for restock — independent of `LockClickShortcut`, does not require it to also be held |

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
| `FireplaceAutoRefuel` | on | Fireplaces automatically pull fuel |
| `SmelterAutoRefuel` | on | Smelters automatically pull ore/fuel |
| `SmelterAutoCollect` | on | Smelters store their output in a chest |
| `KilnAutoRefuel` | on | Kilns automatically pull wood |
| `KilnAutoCollect` | on | Kilns store/redirect the coal they produce |
| `CookingStationAutoRefuel` | on | Cooking stations automatically pull raw food/fuel |
| `CookingStationAutoCollect` | on | Cooking stations collect and store on their own |

## Charcoal kiln (kiln-specific tuning)

| Option | Default | Description |
|---|---|---|
| `KilnWoodBuffer` | 3 | Wood level kept in the internal queue (not the kiln's max capacity) |
| `KilnMaxCoalInChest` | 50 | Coal cap in nearby chests before pausing new wood pulls |
| `KilnFeedStrategy` | `LeastFuelFirst` | How to pick which nearby smelter to feed first: `LeastFuelFirst` or `Nearest` |

## Animals (automatic feeding)

| Option | Default | Description |
|---|---|---|
| `AnimalFeederRadius` | 20m | Radius in which hungry tameable animals search nearby chests for food |
| `AnimalAutoFeed` | on | Tameable animals automatically pull compatible food from nearby chests |
