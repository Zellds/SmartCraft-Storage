# Stations

The station automations pull material from nearby chests on their own and
store the output instead of dropping it on the ground. Each has its own
radius and can be disabled individually (see the Configuration page).

Skill XP (Cooking) from automatic food collection always goes to whoever
owns the station (typically whoever built it or interacted with it first)
— in multiplayer, not necessarily whoever's nearby or supplied the
ingredient.

## Where the output goes

Every station below stores what it produces in a nearby chest. By default
(`ChestOutputStrategy` = `PreferSorted`) it picks a chest that **already holds
that item**, so sorted storage stays sorted — the bars keep going back to your
iron chest even when an emptier one is closer. Only when no nearby chest holds
the item yet, or they're all full, does the nearest chest with room get it, and
only when no chest has room at all does anything drop on the ground.

Set `ChestOutputStrategy` to `Nearest` for the old always-closest behavior. See
[Configuration](5-Configuration.md#output-which-chest-a-stations-product-goes-into).

## Fireplace, torch and hearth

Refuels fuel (wood, resin, etc.) by pulling from the nearest chest until
full, one unit at a time. It has no buffer/queue — it's just a fuel tank,
so it tops up to the max whenever there's room.

## Smelter (ore forge)

Automatically pulls ore and fuel from nearby chests and stores the
produced bar in a sorted-or-nearest chest with space. If no chest has room left,
the remainder drops on the ground as usual (default game behavior, with
no duplication or loss of what was already stored).

## Charcoal kiln

Same radius as the smelter (configured together). Unlike the fireplace, it
has a configurable buffer (default: 3) — it only keeps that much wood in
its internal queue instead of filling it all at once, letting ongoing
production finish before pulling more. It also has a configurable cap on
coal accumulated in nearby chests: past that cap, it stops pulling new
wood (without interrupting what's already processing). The coal it
produces first tries to feed nearby smelters that are low on fuel
(configurable strategy: prioritize the one with the least fuel, or the
nearest one); only the leftover goes to a chest.

By default it only pulls regular Wood from nearby chests, never Fine Wood
or Core Wood — the kiln converts all three to coal at the same rate, so
burning the better ones is a straight waste. If a chest only has Fine
Wood/Core Wood available, the kiln simply won't pull from it; keep some
regular Wood nearby, or turn off `KilnRegularWoodOnly` to let it use
whatever wood type is available.

## Cooking station (fire spit, cauldron, etc.)

Pulls raw food (and its own fuel, if the station uses one) from nearby
chests and cooks on its own. When an item finishes cooking, it's collected
automatically and stored in a sorted-or-nearest chest — no need to interact with
the station to take the finished food. Automatic collection goes through
the same code path as a manual interaction, so skill XP and yield bonuses
keep working normally (see the note above about who gets the XP).

## Beehive

As soon as a beehive has any honey ready, it's harvested automatically and
stored in a sorted-or-nearest chest with space — no need to walk up and interact.
If no nearby chest has room, the honey drops on the ground as usual
(default game behavior, nothing is lost). Has its own configurable radius.

## Fermenter

Covers every base it can process — mead, all the resistances, poison
resistance, anything defined by the fermenter itself, nothing hardcoded to
a specific recipe. When empty (and already covered/not exposed to rain, so
nothing is wasted on a base that can't progress yet), it pulls a
compatible base from the nearest chest. When ready, it taps itself and
stores the result in a sorted-or-nearest chest instead of dropping it on the
ground; if no chest has room, it falls back to the ground like usual.

Has its own radius (`FermenterRadius`, capped at 25m) and its own duration
override: `FermenterDurationOverride` lets you set how long (in seconds) a
fermenter takes to finish. Leave it at `0` to keep the fermenter's own
vanilla duration untouched — that's the default, and it's guaranteed to
match the real value since the mod simply doesn't touch it.

## Plant harvest (in test, off by default)

Unlike everything else on this page, this isn't a placed station — it
watches for ripe, cultivated crops near the player and harvests them
automatically into a sorted-or-nearest chest, the same partial-fallback-to-ground
behavior as everywhere else if no chest has room.

It's marked in test and shipped **off** on purpose: the game only exposes
one flag to tell a cultivated crop apart from a wild pickable (mushroom,
berry bush, loose rock), and that can't be double-checked outside of
actually playing with it. Turn on `PlantAutoHarvest` and watch what it
picks up before trusting it on a base you care about. Has its own radius,
`PlantHarvestRadius`, capped at 25m, measured from the player instead of
from a fixed structure.
