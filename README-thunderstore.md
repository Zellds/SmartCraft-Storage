# SmartCraft-Storage

A personal Valheim 1.0 mod that combines storage and station automation
into one cohesive package: mass-stashing items into nearby chests,
crafting/building with materials pulled straight from chests without
opening them, keeping fireplaces, smelters, charcoal kilns and cooking
stations fueled and self-collecting, automatically feeding tameable
animals from a nearby chest, and repairing all your gear at once.

Requires [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
and [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/).

**Install this on your client, not the server.** Every feature runs
client-side and is gated on there being a local player — a dedicated
server never has one, so installing it there alone won't enable anything.
Each player who wants the automation installs it on their own client; the
server doesn't need it at all.

- **Quick-stack:** Press `Shift + E` to stash matching items into nearby chests.
- **Lock and restock:** `Alt + left-click` locks an item so quick-stack never moves it. `Alt + Ctrl + left-click` marks it for restock instead.
- **Restock:** Press `Ctrl + E` to refill every marked item to a full stack from nearby chests, even from zero.
- **Crafting and building:** Use materials from nearby chests within a configurable radius (default 20m) — no need to open them.
- **Available amounts:** Every ingredient shows what you can actually spend in brackets after the amount it needs — `10(34)` — in both the crafting panel and the build HUD, in a compact, exact or spaced format.
- **Repair-all:** The station's Repair button fixes every repairable equipped item in one click instead of one at a time.
- **Fuel and ingredients:** Fireplaces, smelters, charcoal kilns, and cooking stations pull fuel/ingredients from nearby chests and store their output automatically — each behavior toggleable on its own.
- **Sorted output:** What a station produces goes into a chest that already holds that item, so sorted storage stays sorted — the bars keep landing in your iron chest even when an emptier one is closer. Falls back to the nearest chest with room when nothing nearby holds it yet (`ChestOutputStrategy`).
- **Beehives:** Honey is harvested automatically as soon as it's ready and stored in a nearby chest, no need to visit the hive.
- **Fermenter:** Automatically pulls any mead/potion base from nearby chests and stores the finished product once ready.
- **Animal feeding:** Automatically feeds hungry tameable animals from nearby chests, taming or already-tamed.
- **Plant harvest (in test, off by default):** Optionally auto-harvests ripe crops near the player into a nearby chest.
- **Epic Loot compatibility (optional):** If [Epic Loot](https://valheim.thunderstore.io/package/RandyKnapp/EpicLoot/) is also installed, the Enchanter can use materials from nearby chests too. On supported versions, `Alt + left-click` also protects marked equipment from Sacrifice, including equipment you carry.

## Hotkeys

Defaults below — every hotkey is fully configurable in the [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/),
all as full key combos you set by clicking the value and pressing whatever
combo you want. The lock and restock-mark combos are independent of each
other. They work with a free cursor (no need to have a chest open).

| Hotkey (default) | Action |
|---|---|
| `Shift + E` | **Quick-stack**: stashes items from your inventory into nearby chests that already contain that item. Locked (🔒) and equipped items are never moved. |
| `Ctrl + E` | **Restock**: pulls from nearby chest(s) enough of each item marked for restock to fill a full stack in your inventory — even if you currently have none of that item. |
| `Alt + left-click` on an inventory item | Toggles the **lock** (🔒) on that stack — locked player stacks are not quick-stacked, and locked chest stacks are reserved from crafting and automatic station inputs. |
| `Alt + Ctrl + left-click` on an inventory item | Toggles the **restock** mark (🔵) on that item — defines the list the restock hotkey uses. |

Both modifier-clicks replace the normal click (they don't open/move the
item) only while the modifier is held.

Lock and restock marks support [ExtraSlots](https://github.com/shudnal/ExtraSlots)
and [MyLittleUI](https://github.com/shudnal/MyLittleUI). Marks follow items between
the inventory and extra slots, keep their size when icons are scaled, and avoid
duplicate marks when Shield Me Bruh is installed. These mods are optional.

Messages shown by quick-stack and restock are localized (English,
Portuguese-Brazilian, Spanish so far) based on your in-game language.

## Links

[GitHub](https://github.com/Zellds/SmartCraft-Storage) · [@urano_jpg](https://x.com/urano_jpg)

## Full documentation

Detailed docs for every feature, the full configuration reference, and a
FAQ/troubleshooting page live on the
[wiki](https://thunderstore.io/c/valheim/p/Zellds/SmartCraftStorage/wiki):
how nearby chests are chosen, station-by-station behavior, animal feeding
details, every config option with its default, and common questions
(multiplayer/ward behavior, does the mod need updating when the game
updates, etc).
