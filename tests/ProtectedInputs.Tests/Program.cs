using System;
using System.Collections.Generic;
using System.Reflection;
using SmartCraftStorage.Shared;

internal static class Program
{
    private static readonly List<(string Name, Action Body)> Cases = new List<(string, Action)>();

    private static int Main()
    {
        Cases.Add(("locked chest stacks are excluded while free stacks remain consumable", LockedStacksAreReserved));
        Cases.Add(("crafting count and removal include only free chest stacks", CraftingUsesOnlyFreeChestStacks));
        Cases.Add(("cooking smelting fermenting and fireplace inputs skip locked first stacks", StationInputsSkipLockedStacks));
        Cases.Add(("station recipe ordering remains conversion-first", StationRecipeOrderingIsPreserved));
        Cases.Add(("cooking smelter and kiln fuel paths consume only eligible unlocked stacks", StationFuelPathsProtectLockedStacks));
        Cases.Add(("locked-only and rejected removals create no station progress", FailedStationRemovalCreatesNoProgress));
        Cases.Add(("Epic Loot list count name and exact callbacks protect locked stacks", EpicLootCallbacksProtectLockedStacks));
        Cases.Add(("ALT lock toggle saves and invalidates availability", AltTogglePersistsAndInvalidates));
        Cases.Add(("smelter output goes to the chest that already holds the bar", SmelterOutputPrefersTheSortedChest));
        Cases.Add(("smelter output falls through a full sorted chest to one with room", SmelterOutputFallsThroughAFullSortedChest));
        Cases.Add(("fermenter checks for room before claiming write access", FermenterChecksRoomBeforeClaimingWriteAccess));

        int failed = 0;
        foreach (var test in Cases)
        {
            TestWorld.Reset();
            try
            {
                test.Body();
                Console.WriteLine("PASS " + test.Name);
            }
            catch (Exception error)
            {
                failed++;
                Console.Error.WriteLine("FAIL " + test.Name + ": " + error.Message);
            }
        }

        Console.WriteLine($"{Cases.Count - failed}/{Cases.Count} passed");
        return failed == 0 ? 0 : 1;
    }

    private static void StationRecipeOrderingIsPreserved()
    {
        AssertConversionOrder("SmartCraftStorage.Stations.CookingStationPatches+RefuelPatch", new CookingStation());
        AssertConversionOrder("SmartCraftStorage.Stations.SmelterPatches+RefuelPatch", new Smelter());
        AssertConversionOrder("SmartCraftStorage.Stations.FermenterPatches+AutoProcessPatch", new Fermenter());
    }

    private static void AssertConversionOrder(string patchType, StationBase station)
    {
        TestWorld.ClearChests();
        var chest = TestWorld.CreateChest();
        chest.GetInventory().AddStack("second", 1, worldLevel: 3);
        chest.GetInventory().AddStack("first", 1, worldLevel: 3);
        station.ConfigureInput("first");
        station.ConfigureInput("second");
        InvokeNested(patchType, "Postfix", new object[] { station }, _ => { });
        Equal(0, chest.GetInventory().CountItems("first"));
        Equal(1, chest.GetInventory().CountItems("second"));
    }

    private static void StationFuelPathsProtectLockedStacks()
    {
        var cooking = new CookingStation();
        cooking.ConfigureFuel("fuel");
        AssertFuelConsumesOnlyFree("SmartCraftStorage.Stations.CookingStationPatches+RefuelPatch", cooking, "fuel");

        var smelter = new Smelter();
        smelter.ConfigureFuel("fuel");
        AssertFuelConsumesOnlyFree("SmartCraftStorage.Stations.SmelterPatches+RefuelPatch", smelter, "fuel");

        TestWorld.ClearChests();
        var chest = TestWorld.CreateChest();
        var locked = chest.GetInventory().AddStack("wood", 1, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(locked, true);
        chest.GetInventory().AddStack("wood", 1, worldLevel: 3);
        var kiln = new Smelter();
        kiln.ConfigureKiln("wood");
        InvokeNested("SmartCraftStorage.Stations.SmelterPatches+RefuelPatch", "Postfix", new object[] { kiln }, _ => { });
        Equal(1, locked.m_stack);
        Equal(0, UnlockedInventory.CountItems(chest.GetInventory(), "wood"));
        Equal(1, kiln.Progress);
    }

    private static void AssertFuelConsumesOnlyFree(string patchType, StationBase station, string name)
    {
        TestWorld.ClearChests();
        var chest = TestWorld.CreateChest();
        var locked = chest.GetInventory().AddStack(name, 1, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(locked, true);
        chest.GetInventory().AddStack(name, 1, worldLevel: 3);
        InvokeNested(patchType, "Postfix", new object[] { station }, _ => { });
        Equal(1, locked.m_stack);
        Equal(0, UnlockedInventory.CountItems(chest.GetInventory(), name));
        Equal(1, station.Progress);
    }

    private static void FailedStationRemovalCreatesNoProgress()
    {
        var station = new CookingStation();
        station.ConfigureInput("input");
        TestWorld.ClearChests();
        var chest = TestWorld.CreateChest();
        var locked = chest.GetInventory().AddStack("input", 1, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(locked, true);
        InvokeNested("SmartCraftStorage.Stations.CookingStationPatches+RefuelPatch", "Postfix", new object[] { station }, _ => { });
        Equal(0, station.Progress);

        TestWorld.ClearChests();
        chest = TestWorld.CreateChest();
        chest.GetInventory().AddStack("input", 1, worldLevel: 3);
        chest.GetInventory().FailRemovals = true;
        InvokeNested("SmartCraftStorage.Stations.CookingStationPatches+RefuelPatch", "Postfix", new object[] { station }, _ => { });
        Equal(0, station.Progress);
    }

    private static void EpicLootCallbacksProtectLockedStacks()
    {
        var chest = TestWorld.CreateChest();
        var locked = chest.GetInventory().AddStack("rune", 20, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(locked, true);
        var free = chest.GetInventory().AddStack("rune", 5, worldLevel: 3);

        var items = (List<ItemDrop.ItemData>)InvokeReturn("SmartCraftStorage.Integrations.EpicLootProvider", "GetItems");
        Equal(1, items.Count);
        Equal(free, items[0]);
        Equal(5, (int)InvokeReturn("SmartCraftStorage.Integrations.EpicLootProvider", "CountItem", "rune"));
        Equal(3, (int)InvokeReturn("SmartCraftStorage.Integrations.EpicLootProvider", "RemoveItem", "rune", 3));
        Equal(2, free.m_stack);
        Equal(0, (int)InvokeReturn("SmartCraftStorage.Integrations.EpicLootProvider", "RemoveExactItem", locked, 1));
        Equal(1, (int)InvokeReturn("SmartCraftStorage.Integrations.EpicLootProvider", "RemoveExactItem", free, 1));
        Equal(20, locked.m_stack);
        Equal(1, free.m_stack);

        chest.GetInventory().FailRemovals = true;
        Equal(0, (int)InvokeReturn("SmartCraftStorage.Integrations.EpicLootProvider", "RemoveItem", "rune", 1));
        Equal(0, (int)InvokeReturn("SmartCraftStorage.Integrations.EpicLootProvider", "RemoveExactItem", free, 1));
        Equal(0, (int)InvokeReturn("SmartCraftStorage.Integrations.EpicLootProvider", "RemoveItem", null, 1));
        Equal(1, free.m_stack);
    }

    private static void AltTogglePersistsAndInvalidates()
    {
        var chest = TestWorld.CreateChest();
        var item = chest.GetInventory().AddStack("wood", 5);
        var grid = new InventoryGrid(chest.GetInventory(), item);
        ChestCountCache.Store("wood", -1, true, 5);
        ZInput.Held = UnityEngine.KeyCode.LeftAlt;

        Equal(false, (bool)InvokeReturn("SmartCraftStorage.ItemMarking.AltClickPatch", "Prefix", grid, new UIInputHandler()));
        Equal(true, SmartCraftStorage.ItemMarking.ItemFlags.IsLocked(item));
        Equal(false, ChestCountCache.TryGet("wood", -1, true, out _));
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(item, false);
        chest.ReloadLock(item);
        Equal(true, SmartCraftStorage.ItemMarking.ItemFlags.IsLocked(item));

        ChestCountCache.Store("wood", -1, true, 5);
        Equal(false, (bool)InvokeReturn("SmartCraftStorage.ItemMarking.AltClickPatch", "Prefix", grid, new UIInputHandler()));
        Equal(false, SmartCraftStorage.ItemMarking.ItemFlags.IsLocked(item));
        Equal(false, ChestCountCache.TryGet("wood", -1, true, out _));
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(item, true);
        chest.ReloadLock(item);
        Equal(false, SmartCraftStorage.ItemMarking.ItemFlags.IsLocked(item));
    }

    private static void StationInputsSkipLockedStacks()
    {
        AssertStationConsumesOnlyFree("SmartCraftStorage.Stations.CookingStationPatches+RefuelPatch", new CookingStation());
        AssertStationConsumesOnlyFree("SmartCraftStorage.Stations.SmelterPatches+RefuelPatch", new Smelter());
        AssertStationConsumesOnlyFree("SmartCraftStorage.Stations.FermenterPatches+AutoProcessPatch", new Fermenter());
        AssertStationConsumesOnlyFree("SmartCraftStorage.Stations.FireplacePatch", new Fireplace());
    }

    private static void AssertStationConsumesOnlyFree(string patchType, StationBase station)
    {
        TestWorld.ClearChests();
        var chest = TestWorld.CreateChest();
        var locked = chest.GetInventory().AddStack("input", 1, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(locked, true);
        chest.GetInventory().AddStack("input", 1, worldLevel: 0);
        chest.GetInventory().AddStack("input", 1, worldLevel: 3);
        station.ConfigureInput("input");

        InvokeNested(patchType, "Postfix", new object[] { station }, _ => { });

        Equal(1, locked.m_stack);
        Equal(1, chest.GetInventory().CountItems("input"));
        Equal(0, UnlockedInventory.CountItems(chest.GetInventory(), "input", -1, true));
        Equal(1, station.Progress);
    }

    private static void CraftingUsesOnlyFreeChestStacks()
    {
        var playerInventory = Player.m_localPlayer.GetInventory();
        var carriedLocked = playerInventory.AddStack("wood", 2, quality: 2, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(carriedLocked, true);
        Player.m_localPlayer.Crafting = true;
        var chest = TestWorld.CreateChest();
        var reserved = chest.GetInventory().AddStack("wood", 20, quality: 2, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(reserved, true);
        chest.GetInventory().AddStack("wood", 5, quality: 2, worldLevel: 3);

        int result = 2;
        InvokeNested("SmartCraftStorage.CraftingChestAccess.InventoryChestPatches+CountItemsPatch", "Postfix",
            new object[] { playerInventory, "wood", 2, true, result }, args => result = (int)args[4]);
        Equal(7, result);

        int amount = 5;
        InvokeNested("SmartCraftStorage.CraftingChestAccess.InventoryChestPatches+RemoveItemPatch", "Prefix",
            new object[] { playerInventory, "wood", amount, 2, true }, args => amount = (int)args[2]);
        playerInventory.RemoveItem("wood", amount, 2, true);
        Equal(20, reserved.m_stack);
        Equal(2, UnlockedInventory.CountItems(chest.GetInventory(), "wood", 2, true));
        Equal(0, playerInventory.CountItems("wood", 2, true));
    }

    // --- Output routing: which nearby chest a station's product lands in ---

    private static void SmelterOutputPrefersTheSortedChest()
    {
        var smelter = new Smelter();
        smelter.ConfigureOutput("ore", "iron");

        var plainChest = TestWorld.CreateChest();
        var ironChest = TestWorld.CreateChest();
        ironChest.GetInventory().AddStack("iron", 3);

        WithSmelterAutoCollect(() => Collect(smelter, "ore", 1));

        Equal(4, ironChest.GetInventory().CountItems("iron"));
        Equal(0, plainChest.GetInventory().CountItems("iron"));
    }

    private static void SmelterOutputFallsThroughAFullSortedChest()
    {
        var smelter = new Smelter();
        smelter.ConfigureOutput("ore", "iron");

        var fullIronChest = TestWorld.CreateChest();
        fullIronChest.GetInventory().AddStack("iron", 3);
        fullIronChest.GetInventory().EmptySlot = false;
        var plainChest = TestWorld.CreateChest();

        WithSmelterAutoCollect(() => Collect(smelter, "ore", 1));

        Equal(3, fullIronChest.GetInventory().CountItems("iron"));
        Equal(1, plainChest.GetInventory().CountItems("iron"));
    }

    private static void FermenterChecksRoomBeforeClaimingWriteAccess()
    {
        var fermenter = new Fermenter();
        fermenter.ConfigureOutput("mead");

        var fullChest = TestWorld.CreateChest();
        fullChest.GetInventory().EmptySlot = false;
        fullChest.m_nview.Owner = false;

        InvokeNested("SmartCraftStorage.Stations.FermenterPatches+CollectRedirectPatch", "Prefix",
            new object[] { fermenter }, _ => { });

        Equal(0, fullChest.m_nview.ClaimCount);
        Equal(false, fullChest.m_nview.Owner);
    }

    private static void Collect(Smelter smelter, string ore, int stack)
    {
        InvokeNested("SmartCraftStorage.Stations.SmelterPatches+CollectPatch", "Prefix",
            new object[] { smelter, ore, stack }, _ => { });
    }

    private static void WithSmelterAutoCollect(Action body)
    {
        var setting = SmartCraftStorage.Stations.StationConfig.SmelterAutoCollect;
        bool previous = setting.Value;
        setting.Value = true;
        try { body(); }
        finally { setting.Value = previous; }
    }

    private static void InvokeNested(string typeName, string method, object[] args, Action<object[]> readBack)
    {
        var type = typeof(NearbyContainers).Assembly.GetType(typeName, throwOnError: true);
        try
        {
            type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
        }
        catch (TargetInvocationException error) when (error.InnerException != null)
        {
            throw error.InnerException;
        }
        readBack(args);
    }

    private static object InvokeReturn(string typeName, string method, params object[] args)
    {
        var type = typeof(NearbyContainers).Assembly.GetType(typeName, throwOnError: true);
        try { return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args); }
        catch (TargetInvocationException error) when (error.InnerException != null) { throw error.InnerException; }
    }

    private static void LockedStacksAreReserved()
    {
        var inventory = new Inventory();
        var locked = inventory.AddStack("wood", 20, quality: 2, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(locked, true);
        inventory.AddStack("wood", 5, quality: 2, worldLevel: 3);

        Equal(5, UnlockedInventory.CountItems(inventory, "wood", 2, true));
        Equal(3, UnlockedInventory.RemoveItems(inventory, "wood", 3, 2, true));
        Equal(20, locked.m_stack);
        Equal(2, UnlockedInventory.CountItems(inventory, "wood", 2, true));
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!Equals(expected, actual))
        {
            throw new Exception($"Expected {expected}, got {actual}");
        }
    }
}
