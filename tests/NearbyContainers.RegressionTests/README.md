# Nearby container regression tests

Run on Windows with the .NET SDK and .NET Framework 4.8:

```powershell
dotnet run --project tests/NearbyContainers.RegressionTests/NearbyContainers.RegressionTests.csproj -c Release
```

The executable returns a nonzero exit code if a test fails. It links the real
`NearbyContainers.cs` and `ChestCountCache.cs` and substitutes only the external
Unity/Valheim boundary. No game assemblies or test framework packages are needed.
The main plugin project excludes these test sources from its assembly.

## Cart fixture evidence

The fixture comes from a read-only inspection of a local Valheim installation,
Steam build **25364265**, on 2026-09-17:

- `Assets/GameElements/Cart/Cart.prefab` is in SoftRef bundle `c4210710`.
- The root `Cart` has a `Vagon` component whose serialized `m_container` points
  to the `Container` component on the child named `Container`.
- `Cart/Container` has an enabled, non-trigger `MeshCollider` on layer 12 (`item`).
- Wheel and body colliders, including `Cart/Vagon/colliders/*`, are on layer 28
  (`vehicle`); the hold is not their ancestor.
- Layer names were read from the installed game's `TagManager`. The component
  types were resolved through the `MonoScript` entries in bundle `86c3d76e`.

The layer mask added in commit `cfc7ba8` for 0.5.0 excludes `item`, so the old
parent-only lookup no longer reaches the cart inventory. The first regression
test returned zero containers before the fix and one afterward. Resolving
`Vagon.m_container` preserves the narrow physics query without scanning all
dropped items or recursively searching arbitrary children.

## Coverage and limits

The 14 tests cover cart discovery, ordinary chests, distance ordering, duplicate
colliders, parent-container precedence, empty vehicles, access rules at discovery
and before writing, ownership, excluded layers, radius, cache reuse/expiry,
movement/radius invalidation, collider-buffer growth, and avoiding cart ancestry
searches on non-vehicle colliders. Access tests include
local and remote in-use state, player access, wards and tombstones.

The doubles model component ancestry, layer filtering and point overlaps.
They do not execute Unity physics, Unity's destroyed-object semantics, real
network replication, item transfers or other mods. Query counts protect the
cache contract; they are not an in-game performance benchmark. The fallback
adds a `Vagon` ancestry lookup only on uncached `vehicle` hits without a parent
`Container`. A scene with 600 static colliders must add zero such lookups; the
first cart patch performed 600. The vehicle layer is resolved once, alongside
the query mask.

## User-reported in-game comparison

On 2026-09-17, the user compared unmodified 0.6.0 with both local cart patches:

- Unmodified 0.6.0 quick-stacks to ships but does not discover the cart.
- Both fix1 and fix2 restore quick-stack to the cart.
- The brief stall affects manual mouse/Ctrl+click transfers, not quick-stack.
  It also occurs in unmodified 0.6.0. In that original-version test it disappeared
  after leaving and re-entering the world; the user reports the same stall
  behavior with both patched builds.

This supersedes the earlier report that quick-stack also stalled. It does not
establish a stall introduced by the cart patch. Fix2 remains the contribution
candidate because it retains cart support while avoiding cart ancestry searches
on non-vehicle colliders. The comparison is qualitative, not a frame-time
benchmark or a diagnosis of the intermittent manual-transfer stall.

## Remaining in-game checks

The comparison above confirms cart quick-stack in the user's environment. It
does not establish the following more specific cases; retain these checks for
additional coverage:

1. With only the plugin and its dependencies, put wood in a cart and carry more
   wood. Keep other chests outside the configured quick-stack radius. Use
   quick-stack beside the parked cart, then while pulling it. Both should move
   matching, unlocked items into the cart.
2. Move outside the radius and wait at least 0.25 seconds. The cart should no
   longer be eligible. Repeat with a full cart and with no matching items to
   confirm the usual quick-stack behavior.
3. Have another player open the cart. Quick-stack must leave it alone. Repeat
   with a ward that denies access; verify item counts on both clients.
4. Smoke-test ordinary chests and ship storage, plus restock and crafting from
   the cart, because they share the detector.
5. Repeat the cart scenario with the user's usual mods. If a conflict remains,
   isolate it with a separate profile.
6. Compare inventory transfers in the same dense base, at the same configured
   radius, against the unmodified release. Include drag/drop, Ctrl+click and
   quick-stack, both with the inventory alone and with crafting/building open.
   These releases have no built-in `Diagnostics/LogPerformance` setting; actual
   frame-time measurements require a game profiler or separate instrumentation.
   The mask, NonAlloc buffer and cache are retained, but this harness does not
   prove that in-game transfers are free of stalls.
