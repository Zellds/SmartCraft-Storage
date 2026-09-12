# FAQ / Troubleshooting

## Do I need to update the mod every time Valheim updates?

Often, yes, though not for every tiny patch. This mod hooks into the
game's own internal code (private methods and fields, not a stable public
modding API) to implement its automations. If a game update renames or
restructures one of those internals, the specific feature that relies on
it can stop working until the mod is updated — usually without crashing
the game or the rest of the mod, since each automation is isolated.
Before assuming everything still works after a big game update, open the
game, test the main features, and check the BepInEx console for errors.
Also note that BepInEx and Jotunn (this mod's dependencies) usually need
to catch up to a game update first — if either of those isn't compatible
yet, the mod won't load at all.

## Does this work in multiplayer / on dedicated servers?

Yes. Every automation respects ward/permission rules exactly like opening
a chest manually would, and claims ownership of a chest's ZDO before
writing to it if another player currently owns it — the same mechanism
the game itself uses. On a dedicated server with no player logged in
nearby, automation for that area simply doesn't run (nothing is actively
simulated there), the same as vanilla behavior.

If the server also has this mod installed, its configuration wins: the
server's settings are pushed to every client on connect, so everyone
plays with the same radii and automations. Hotkeys are never synced. If
the server doesn't have the mod, nothing is synced and your own settings
apply as usual.

## Why are my settings locked (or ignored) on a server?

Because the server has this mod installed and sets them for everyone. Its
values are pushed to you on connect and the entries are locked in the
Configuration Manager unless you're a server admin. Your config file is
never written to, and your own values come back when you disconnect.
Hotkeys stay yours either way. Server admins can retune these live from
in-game: an admin's edits go back to the server and out to everyone else.

## Why isn't a specific chest being used by the automation?

Check, in order: is it within the configured radius? Is it currently open
by another player? Do you have access to it (public/private/group
permission)? Is it inside a ward you don't have access to, or outside a
ward that would otherwise grant you access? Any one of these will make
the mod skip that chest.

## Who gets the Cooking skill XP from auto-collected food?

Whoever owns the cooking station (usually whoever built it, or was first
to interact with it) — in multiplayer, not necessarily whoever is nearby
or supplied the raw ingredient. See the Stations page for more detail.

## Can I turn off just one automation and keep the rest?

Yes — every station behavior (refuel/collect, per station type) has its
own on/off toggle in the Configuration Manager, and the animal feeder has
its own toggle too. Quick-stack, restock, and crafting-from-chests aren't
individually toggleable since they only ever run when you actively press
a hotkey or interact with a crafting station.

## Does the animal feeder only feed animals I've already tamed?

No — see the Animal Feeding page. It also feeds wild, untamed animals
(and any tameable creature not currently alerted/fighting) as long as
compatible food is in a nearby chest. There's no toggle to restrict it to
tamed animals only.

## Does repair-all cost anything extra?

No — repairing has no resource cost in vanilla Valheim, and repair-all
doesn't change that. It just repairs everything the station can repair in
one click instead of requiring repeated clicks.
