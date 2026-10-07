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
        Cases.Add(("food preparation crafting counts and consumes nearby fish without carrying it", FoodPreparationCraftingUsesNearbyFish));
        Cases.Add(("food preparation crafting only uses nearby chests while its craft tab is active", FoodPreparationContextIsScopedToItsCraftTab));
        Cases.Add(("single ingredient fish recipe selects a chest fish for its output amount", SingleIngredientFishRecipeSelectsChestInput));
        Cases.Add(("cooking smelting fermenting and fireplace inputs skip locked first stacks", StationInputsSkipLockedStacks));
        Cases.Add(("station recipe ordering remains conversion-first", StationRecipeOrderingIsPreserved));
        Cases.Add(("cooking smelter and kiln fuel paths consume only eligible unlocked stacks", StationFuelPathsProtectLockedStacks));
        Cases.Add(("locked-only and rejected removals create no station progress", FailedStationRemovalCreatesNoProgress));
        Cases.Add(("Epic Loot list count name and exact callbacks protect locked stacks", EpicLootCallbacksProtectLockedStacks));
        Cases.Add(("ALT lock toggle saves and invalidates availability", AltTogglePersistsAndInvalidates));
        Cases.Add(("smelter output goes to the chest that already holds the bar", SmelterOutputPrefersTheSortedChest));
        Cases.Add(("smelter output falls through a full sorted chest to one with room", SmelterOutputFallsThroughAFullSortedChest));
        Cases.Add(("fermenter checks for room before claiming write access", FermenterChecksRoomBeforeClaimingWriteAccess));
        Cases.Add(("an obliterator is never treated as a chest", ObliteratorIsNeverAChest));
        Cases.Add(("a remote chest is reloaded before ownership is claimed", RemoteChestIsReloadedBeforeClaiming));
        Cases.Add(("a chest already owned is reloaded too, without claiming it again", OwnedChestIsReloadedWithoutClaimingItAgain));
        Cases.Add(("smelter auto-refuel skips blacklisted inputs by prefab, token or shown name", SmelterBlacklistSkipsListedInputs));
        Cases.Add(("smelter auto-refuel still pulls inputs that are not blacklisted", SmelterBlacklistKeepsOtherInputs));
        Cases.Add(("a disabled smelter-type station stops pulling from chests", DisabledStationStopsPulling));
        Cases.Add(("a disabled smelter-type station no longer redirects its output", DisabledStationOutputIsLeftToTheGame));
        Cases.Add(("turning one station off leaves the other smelter-type stations alone", DisabledStationLeavesOthersAlone));

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

    private static void FoodPreparationCraftingUsesNearbyFish()
    {
        var playerInventory = Player.m_localPlayer.GetInventory();
        var chest = TestWorld.CreateChest();
        var reservedFish = chest.GetInventory().AddStack("fish", 1, quality: 1, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(reservedFish, true);
        chest.GetInventory().AddStack("fish", 1, quality: 1, worldLevel: 3);
        CraftingStation.FoodPreparationTableInRange = true;
        InventoryGui.Visible = true;
        InventoryGui.instance.CraftTab = true;

        int available = playerInventory.CountItems("fish", 1, true);
        InvokeNested("SmartCraftStorage.CraftingChestAccess.InventoryChestPatches+CountItemsPatch", "Postfix",
            new object[] { playerInventory, "fish", 1, true, available }, args => available = (int)args[4]);
        Equal(1, available);

        int amount = 1;
        InvokeNested("SmartCraftStorage.CraftingChestAccess.InventoryChestPatches+RemoveItemPatch", "Prefix",
            new object[] { playerInventory, "fish", amount, 1, true }, args => amount = (int)args[2]);
        playerInventory.RemoveItem("fish", amount, 1, true);
        Equal(1, reservedFish.m_stack);
        Equal(1, chest.GetInventory().CountItems("fish", 1, true));
        Equal(0, UnlockedInventory.CountItems(chest.GetInventory(), "fish", 1, true));
        Equal(0, playerInventory.CountItems("fish", 1, true));
    }

    private static void FoodPreparationContextIsScopedToItsCraftTab()
    {
        var playerInventory = Player.m_localPlayer.GetInventory();
        var chest = TestWorld.CreateChest();
        chest.GetInventory().AddStack("fish", 1, quality: 1, worldLevel: 3);
        CraftingStation.FoodPreparationTableInRange = true;
        InventoryGui.Visible = true;
        InventoryGui.instance.CraftTab = false;

        int available = playerInventory.CountItems("fish", 1, true);
        InvokeNested("SmartCraftStorage.CraftingChestAccess.InventoryChestPatches+CountItemsPatch", "Postfix",
            new object[] { playerInventory, "fish", 1, true, available }, args => available = (int)args[4]);
        Equal(0, available);

        InventoryGui.instance.CraftTab = true;
        InventoryGui.Visible = false;
        available = playerInventory.CountItems("fish", 1, true);
        InvokeNested("SmartCraftStorage.CraftingChestAccess.InventoryChestPatches+CountItemsPatch", "Postfix",
            new object[] { playerInventory, "fish", 1, true, available }, args => available = (int)args[4]);
        Equal(0, available);

        InventoryGui.Visible = true;
        CraftingStation.FoodPreparationTableInRange = false;
        available = playerInventory.CountItems("fish", 1, true);
        InvokeNested("SmartCraftStorage.CraftingChestAccess.InventoryChestPatches+CountItemsPatch", "Postfix",
            new object[] { playerInventory, "fish", 1, true, available }, args => available = (int)args[4]);
        Equal(0, available);
    }

    private static void SingleIngredientFishRecipeSelectsChestInput()
    {
        var player = Player.m_localPlayer;
        var inventory = player.GetInventory();
        var chest = TestWorld.CreateChest();
        var reservedFish = chest.GetInventory().AddStack("fish", 1, quality: 1, worldLevel: 3);
        SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(reservedFish, true);
        var fish = chest.GetInventory().AddStack("fish", 1, quality: 2, worldLevel: 3);
        CraftingStation.FoodPreparationTableInRange = true;
        InventoryGui.Visible = true;
        InventoryGui.instance.CraftTab = true;

        var recipeItem = new ItemDrop();
        recipeItem.m_itemData.m_shared.m_name = "raw_fish";
        recipeItem.m_itemData.m_shared.m_maxQuality = 1;
        var recipe = new Recipe
        {
            m_requireOnlyOneIngredient = true,
            m_resources = new[]
            {
                new Piece.Requirement { m_resItem = new ItemDrop { m_itemData = { m_shared = { m_name = "fish", m_maxQuality = 2 } } } }
            }
        };

        ItemDrop.ItemData selected = null;
        int amount = 0;
        int extraAmount = 0;
        InvokeNested("SmartCraftStorage.CraftingChestAccess.InventoryChestPatches+GetFirstRequiredItemPatch", "Postfix",
            new object[] { player, inventory, recipe, 1, amount, extraAmount, 1, selected }, args =>
            {
                amount = (int)args[4];
                extraAmount = (int)args[5];
                selected = (ItemDrop.ItemData)args[7];
            });

        Equal(fish, selected);
        Equal(1, amount);
        Equal(0, extraAmount);
        Equal(2, selected.m_quality);

        int consumed = amount;
        InvokeNested("SmartCraftStorage.CraftingChestAccess.InventoryChestPatches+RemoveItemPatch", "Prefix",
            new object[] { inventory, selected.m_shared.m_name, consumed, selected.m_quality, true }, args => consumed = (int)args[2]);
        inventory.RemoveItem(selected.m_shared.m_name, consumed, selected.m_quality, true);
        Equal(1, reservedFish.m_stack);
        Equal(0, UnlockedInventory.CountItems(chest.GetInventory(), "fish", -1, true));
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

    private static void ObliteratorIsNeverAChest()
    {
        var obliterator = TestWorld.CreateChest();
        obliterator.IncineratorAncestor = new Incinerator();
        var chest = TestWorld.CreateChest();

        var found = NearbyContainers.Find(new UnityEngine.Vector3(), 10f, Player.m_localPlayer);

        Equal(1, found.Count);
        Equal(true, ReferenceEquals(found[0], chest));
        Equal(false, NearbyContainers.TryClaimWriteAccess(obliterator));
        Equal(0, obliterator.m_nview.ClaimCount);
    }

    private static void RemoteChestIsReloadedBeforeClaiming()
    {
        var remote = TestWorld.CreateChest();
        remote.m_nview.Owner = false;

        Equal(true, NearbyContainers.TryClaimWriteAccess(remote));
        Equal(1, remote.OwnedWhenRefreshed.Count);
        Equal(false, remote.OwnedWhenRefreshed[0]);
        Equal(1, remote.m_nview.ClaimCount);
    }

    // Owning a chest doesn't mean its inventory is loaded: the game re-instantiates
    // a player's base chests empty when they come back, and loads them a moment later.
    private static void OwnedChestIsReloadedWithoutClaimingItAgain()
    {
        var owned = TestWorld.CreateChest();

        Equal(true, NearbyContainers.TryClaimWriteAccess(owned));
        Equal(1, owned.OwnedWhenRefreshed.Count);
        Equal(0, owned.m_nview.ClaimCount);
    }

    private const string SmelterRefuelPatch = "SmartCraftStorage.Stations.SmelterPatches+RefuelPatch";

    private static void WithSmelterBlacklist(string list, Action body)
    {
        var setting = SmartCraftStorage.Stations.StationConfig.SmelterAutoRefuelBlacklist;
        string previous = setting.Value;
        setting.Value = list;
        try { body(); }
        finally { setting.Value = previous; }
    }

    private static void SmelterBlacklistSkipsListedInputs()
    {
        // The stub names the prefab "inputPrefab", the token "input" and the shown name "Shown input".
        foreach (var list in new[] { "inputPrefab", "INPUT", "$input", "Shown input", "other, inputPrefab ,more" })
        {
            WithSmelterBlacklist(list, () =>
            {
                TestWorld.ClearChests();
                var chest = TestWorld.CreateChest();
                chest.GetInventory().AddStack("input", 1, worldLevel: 3);
                var smelter = new Smelter();
                smelter.ConfigureInput("input");

                InvokeNested(SmelterRefuelPatch, "Postfix", new object[] { smelter }, _ => { });

                Equal(0, smelter.Progress);
                Equal(1, chest.GetInventory().CountItems("input"));
            });
        }
    }

    private static void SmelterBlacklistKeepsOtherInputs()
    {
        foreach (var list in new[] { "", "   ", "oat", "inputs, Shown other" })
        {
            WithSmelterBlacklist(list, () =>
            {
                TestWorld.ClearChests();
                var chest = TestWorld.CreateChest();
                chest.GetInventory().AddStack("input", 1, worldLevel: 3);
                var smelter = new Smelter();
                smelter.ConfigureInput("input");

                InvokeNested(SmelterRefuelPatch, "Postfix", new object[] { smelter }, _ => { });

                Equal(1, smelter.Progress);
                Equal(0, chest.GetInventory().CountItems("input"));
            });
        }
    }

    private static readonly (string Name, Func<SmartCraftStorage.Stations.Setting<bool>> Setting)[] SmelterTypeStations =
    {
        ("$piece_windmill", () => SmartCraftStorage.Stations.StationConfig.WindmillAutomation),
        ("$piece_spinningwheel", () => SmartCraftStorage.Stations.StationConfig.SpinningWheelAutomation),
        ("$piece_blastfurnace", () => SmartCraftStorage.Stations.StationConfig.BlastFurnaceAutomation),
        ("$piece_eitrrefinery", () => SmartCraftStorage.Stations.StationConfig.EitrRefineryAutomation),
    };

    private static void WithStationAutomation(Func<SmartCraftStorage.Stations.Setting<bool>> setting, bool value, Action body)
    {
        var entry = setting();
        bool previous = entry.Value;
        entry.Value = value;
        try { body(); }
        finally { entry.Value = previous; }
    }

    private static int PullFromChest(string stationName)
    {
        TestWorld.ClearChests();
        var chest = TestWorld.CreateChest();
        chest.GetInventory().AddStack("input", 1, worldLevel: 3);
        var smelter = new Smelter { m_name = stationName };
        smelter.ConfigureInput("input");

        InvokeNested(SmelterRefuelPatch, "Postfix", new object[] { smelter }, _ => { });

        return smelter.Progress;
    }

    private static void DisabledStationStopsPulling()
    {
        foreach (var station in SmelterTypeStations)
        {
            WithStationAutomation(station.Setting, false, () => Equal(0, PullFromChest(station.Name)));
            WithStationAutomation(station.Setting, true, () => Equal(1, PullFromChest(station.Name)));
        }
    }

    private static void DisabledStationOutputIsLeftToTheGame()
    {
        foreach (var station in SmelterTypeStations)
        {
            foreach (bool enabled in new[] { false, true })
            {
                WithStationAutomation(station.Setting, enabled, () =>
                {
                    TestWorld.ClearChests();
                    var chest = TestWorld.CreateChest();
                    var smelter = new Smelter { m_name = station.Name };
                    smelter.ConfigureOutput("ore", "iron");

                    WithSmelterAutoCollect(() => Collect(smelter, "ore", 1));

                    Equal(enabled ? 1 : 0, chest.GetInventory().CountItems("iron"));
                });
            }
        }
    }

    private static void DisabledStationLeavesOthersAlone()
    {
        var all = new List<string> { "$piece_smelter" };
        foreach (var station in SmelterTypeStations) all.Add(station.Name);

        foreach (var off in SmelterTypeStations)
        {
            WithStationAutomation(off.Setting, false, () =>
            {
                foreach (var name in all)
                {
                    Equal(name == off.Name ? 0 : 1, PullFromChest(name));
                }
            });
        }
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
