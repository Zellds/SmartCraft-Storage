# Protected input regression fixture

This package compiles the touched production sources directly and supplies
small boundaries for Valheim, Unity, Harmony, configuration, and Epic Loot.
It exercises issue 8's protected crafting counts and removal, station input/fuel
routes, Epic Loot material callbacks, and lock persistence/cache invalidation.
Epic Loot Sacrifice has a separate fixture in `tests/EpicLootSacrifice.Tests`.

Run offline from the repository root:

```pwsh
dotnet run --project tests/ProtectedInputs.Tests/ProtectedInputs.Tests.csproj `
  -p:RestoreSources=C:/Users/EremesNG/.nuget/packages `
  -p:RestoreAdditionalProjectSources= -p:NuGetAudit=false
```

The first eight cases verify deterministic local protection of locked stacks
while unlocked stacks remain consumable. Three more cover station output
routing at the real call sites: a smelter's bar going to the chest that already
holds that item rather than the closest one, the same bar falling through to a
chest with room when the sorted one is full, and the fermenter asking about
room before it claims write access on a chest. Live Unity UI, relogs,
multiplayer and other mods' behavior still require an in-game check.

Ordering itself is unit-tested in `tests/NearbyContainers.RegressionTests`,
which links `Shared/OutputChests.cs` with no config stub in the way.
