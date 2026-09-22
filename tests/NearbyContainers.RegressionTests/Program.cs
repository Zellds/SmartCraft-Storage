using System;
using System.Collections.Generic;
using SmartCraftStorage.Shared;
using UnityEngine;

internal static class Program
{
    private static int Main()
    {
        var tests = new Action[]
        {
            FindsCartThroughVehicleCollider,
            FindsOrdinaryChestsAndSortsNearestFirst,
            DeduplicatesCartColliders,
            PrefersTheCollidersOwnContainer,
            SkipsVehiclesWithoutAContainer,
            RejectsUnavailableCarts,
            RechecksAccessBeforeClaimingACachedCart,
            ClaimsAnAvailableCart,
            KeepsItemAndTerrainLayersExcluded,
            ExcludesCartsOutsideTheRadius,
            ReusesTheSweepUntilTheCacheExpires,
            RefreshesAfterPlayerMovementOrRadiusChange,
            FindsCartBeyondTheInitialHitBuffer,
            DoesNotSearchStaticPiecesForCarts,
            PrefersAChestThatAlreadyHoldsTheItemOverACloserOne,
            KeepsNearestFirstOrderInsideEachGroup,
            FallsBackToNearestWhenNoChestHoldsTheItem,
            IgnoresQualityWhenMatchingOutput,
            SkipsDestroyedContainersWhenOrdering,
            NearestStrategyHandsBackTheSearchResultUnchanged,
            OrderingDoesNotMutateTheSharedSearchCache
        };
        int failures = 0;
        foreach (var test in tests)
        {
            ResetScene();
            try
            {
                test();
                Console.WriteLine("PASS: " + test.Method.Name);
            }
            catch (Exception error)
            {
                failures++;
                Console.Error.WriteLine("FAIL: " + test.Method.Name + ": " + error.Message);
            }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void FindsCartThroughVehicleCollider()
    {
        var cart = CreateCart(2f);
        var found = Find();
        Expect(found.Count == 1 && ReferenceEquals(found[0], cart),
            "Expected the nearby cart's inventory through its vehicle collider; found " + found.Count + " containers.");
    }

    private static void FindsOrdinaryChestsAndSortsNearestFirst()
    {
        var chest = At(6f, "piece").AddComponent<Container>();
        At(6f, "piece", chest.gameObject).AddComponent<Collider>();
        var cart = CreateCart(2f);
        var found = Find();
        Expect(found.Count == 2 && ReferenceEquals(found[0], cart) && ReferenceEquals(found[1], chest),
            "Expected both inventories, with the nearer cart first.");
    }

    private static void DeduplicatesCartColliders()
    {
        var cart = CreateCart(2f);
        var body = At(2f, "Default", cart.gameObject.Parent);
        for (int i = 0; i < 16; i++) At(2f, "vehicle", body).AddComponent<Collider>();
        var found = Find();
        Expect(found.Count == 1 && ReferenceEquals(found[0], cart), "A cart must appear only once.");
    }

    private static void PrefersTheCollidersOwnContainer()
    {
        var cart = CreateCart(2f);
        var directContainer = cart.gameObject.Parent.AddComponent<Container>();
        var found = Find();
        Expect(found.Count == 1 && ReferenceEquals(found[0], directContainer),
            "The existing parent-container lookup must take precedence over the cart fallback.");
    }

    private static void SkipsVehiclesWithoutAContainer()
    {
        At(2f, "vehicle").AddComponent<Collider>();
        var emptyVagon = At(3f, "vehicle");
        emptyVagon.AddComponent<Vagon>();
        emptyVagon.AddComponent<Collider>();
        CreateCart(4f).Inventory = null;
        Expect(Find().Count == 0, "Unrelated vehicles, empty Vagon references and missing inventories must be skipped.");
    }

    private static readonly Action<Container>[] DenyAccess =
    {
        cart => cart.InUse = true,
        cart => cart.m_nview.Data.InUse = 1,
        cart => cart.Access = false,
        cart => PrivateArea.Allowed = false,
        cart => cart.gameObject.AddComponent<TombStone>()
    };

    private static void RejectsUnavailableCarts()
    {
        for (int i = 0; i < DenyAccess.Length; i++)
        {
            ResetScene();
            DenyAccess[i](CreateCart(2f));
            Expect(Find().Count == 0, "Search must apply the existing access rule " + i + " to carts.");
        }
    }

    private static void RechecksAccessBeforeClaimingACachedCart()
    {
        for (int i = 0; i < DenyAccess.Length; i++)
        {
            ResetScene();
            var cart = CreateCart(2f);
            var found = Find();
            Expect(found.Count == 1, "The cart must initially be discoverable.");
            DenyAccess[i](cart);
            Expect(!NearbyContainers.TryClaimWriteAccess(found[0]) && !cart.m_nview.Owner,
                "Write access must recheck rule " + i + " without claiming a now-unavailable cart.");
        }
    }

    private static void ClaimsAnAvailableCart()
    {
        var cart = CreateCart(2f);
        var found = Find();
        Expect(found.Count == 1 && NearbyContainers.TryClaimWriteAccess(found[0]) && cart.m_nview.Owner,
            "A discovered, available cart must allow the normal ownership claim.");
    }

    private static void KeepsItemAndTerrainLayersExcluded()
    {
        foreach (var layer in new[] { "item", "terrain" })
        {
            var excluded = At(1f, layer);
            excluded.AddComponent<Container>();
            excluded.AddComponent<Collider>();
        }
        var cart = CreateCart(2f);
        var found = Find();
        Expect(found.Count == 1 && ReferenceEquals(found[0], cart), "Only the cart should be discovered.");
        Expect((Physics.LastMask & LayerMask.GetMask("item", "terrain")) == 0,
            "Restoring carts must not broaden the physics query to items or terrain.");
    }

    private static void ExcludesCartsOutsideTheRadius()
    {
        CreateCart(12f);
        Expect(Find().Count == 0, "A cart outside the search radius must stay excluded.");
    }

    private static void ReusesTheSweepUntilTheCacheExpires()
    {
        CreateCart(2f);
        Find();
        // Boundary query counts verify the performance requirement, not how Find
        // implements its cache. No new physics sweeps should occur while warm.
        for (int i = 0; i < 1000; i++) Find();
        Expect(Physics.Queries == 1, "Repeated warm searches must reuse a single physics sweep.");
        Physics.Colliders.Clear();
        Time.time += 0.3f;
        Expect(Find().Count == 0 && Physics.Queries == 2, "An expired cache must resweep the scene.");
    }

    private static void RefreshesAfterPlayerMovementOrRadiusChange()
    {
        CreateCart(2f);
        Find();
        var moved = NearbyContainers.Find(new Vector3(0.6f, 0f, 0f), 10f, Player.m_localPlayer);
        Expect(moved.Count == 1 && Physics.Queries == 2, "Player movement must refresh the search.");
        var narrowed = NearbyContainers.Find(new Vector3(0.6f, 0f, 0f), 0.5f, Player.m_localPlayer);
        Expect(narrowed.Count == 0 && Physics.Queries == 3, "Changing the radius must refresh the search.");
    }

    private static void FindsCartBeyondTheInitialHitBuffer()
    {
        for (int i = 0; i < 600; i++) At(1f, "piece").AddComponent<Collider>();
        var cart = CreateCart(2f);
        var found = Find();
        Expect(found.Count == 1 && ReferenceEquals(found[0], cart),
            "A crowded scene must not lose a cart beyond the initial collider buffer.");
    }

    private static void DoesNotSearchStaticPiecesForCarts()
    {
        var layers = new[] { "piece", "piece_nonsolid", "Default", "static_solid", "Default_small" };
        for (int i = 0; i < 600; i++) At(1f, layers[i % layers.Length]).AddComponent<Collider>();
        var chest = At(2f, "piece").AddComponent<Container>();
        chest.gameObject.AddComponent<Collider>();
        var found = Find();
        Expect(found.Count == 1 && ReferenceEquals(found[0], chest), "Ordinary chest discovery must keep working.");
        Expect(Component.VagonParentSearches == 0,
            "A scene without vehicle colliders must add no cart ancestry searches; observed " + Component.VagonParentSearches + ".");
    }

    // --- Output ordering: which nearby chest a station's product goes into ---

    private static void PrefersAChestThatAlreadyHoldsTheItemOverACloserOne()
    {
        CreateChest(2f);
        var ironChest = CreateChest(8f, "$item_iron");

        var ordered = OrderForOutput("$item_iron");

        Expect(ordered.Count == 2 && ReferenceEquals(ordered[0], ironChest),
            "Expected the further chest already holding iron to be tried first.");
    }

    private static void KeepsNearestFirstOrderInsideEachGroup()
    {
        var farEmpty = CreateChest(9f);
        var nearIron = CreateChest(4f, "$item_iron");
        var nearEmpty = CreateChest(3f);
        var farIron = CreateChest(7f, "$item_iron");

        var ordered = OrderForOutput("$item_iron");

        Expect(ordered.Count == 4
               && ReferenceEquals(ordered[0], nearIron) && ReferenceEquals(ordered[1], farIron)
               && ReferenceEquals(ordered[2], nearEmpty) && ReferenceEquals(ordered[3], farEmpty),
            "Expected both iron chests first, nearest-first inside each group.");
    }

    private static void FallsBackToNearestWhenNoChestHoldsTheItem()
    {
        var near = CreateChest(3f, "$item_coal");
        var far = CreateChest(8f, "$item_wood");

        var ordered = OrderForOutput("$item_iron");

        Expect(ordered.Count == 2 && ReferenceEquals(ordered[0], near) && ReferenceEquals(ordered[1], far),
            "Expected the untouched nearest-first order when no chest holds the item.");
    }

    private static void IgnoresQualityWhenMatchingOutput()
    {
        CreateChest(2f);
        var upgraded = CreateChest(8f);
        upgraded.Inventory.Holding("$item_bronzesword", 3);

        var ordered = OrderForOutput("$item_bronzesword");

        Expect(ordered.Count == 2 && ReferenceEquals(ordered[0], upgraded),
            "Expected the match to ignore quality; station output always carries the prefab's.");
    }

    private static void SkipsDestroyedContainersWhenOrdering()
    {
        var iron = CreateChest(8f, "$item_iron");
        var candidates = new List<Container> { null, CreateChest(2f), iron };

        var ordered = OutputChests.OrderForOutput(candidates, "$item_iron", ChestOutputStrategy.PreferSorted);

        Expect(ordered.Count == 2 && ReferenceEquals(ordered[0], iron),
            "Expected a destroyed (null) candidate to be dropped rather than thrown on.");
    }

    private static void NearestStrategyHandsBackTheSearchResultUnchanged()
    {
        CreateChest(2f);
        CreateChest(8f, "$item_iron");
        var found = Find();

        var ordered = OutputChests.OrderForOutput(found, "$item_iron", ChestOutputStrategy.Nearest);

        Expect(ReferenceEquals(ordered, found),
            "Expected Nearest to hand the caller's own list straight back, copying nothing.");
    }

    private static void OrderingDoesNotMutateTheSharedSearchCache()
    {
        var near = CreateChest(2f);
        var far = CreateChest(8f, "$item_iron");

        OutputChests.OrderForOutput(Find(), "$item_iron", ChestOutputStrategy.PreferSorted);

        // Inside the 0.25s window this is the same cached list every other feature reads.
        var cached = Find();
        Expect(cached.Count == 2 && ReferenceEquals(cached[0], near) && ReferenceEquals(cached[1], far),
            "Ordering for output must not reorder the shared nearest-first search cache.");
    }

    private static List<Container> OrderForOutput(string itemName)
        => OutputChests.OrderForOutput(Find(), itemName, ChestOutputStrategy.PreferSorted);

    private static Container CreateChest(float distance, params string[] itemNames)
    {
        var chest = At(distance, "piece").AddComponent<Container>();
        At(distance, "piece", chest.gameObject).AddComponent<Collider>();
        foreach (var itemName in itemNames)
        {
            chest.Inventory.Holding(itemName);
        }
        return chest;
    }

    private static List<Container> Find() => NearbyContainers.Find(new Vector3(), 10f, Player.m_localPlayer);

    private static void ResetScene()
    {
        // Expire the production cache through its normal clock boundary.
        Time.time += 1f;
        Time.frameCount++;
        Physics.Colliders.Clear();
        Physics.Queries = 0;
        Component.VagonParentSearches = 0;
        PrivateArea.Allowed = true;
        Player.m_localPlayer = new GameObject().AddComponent<Player>();
    }

    private static Container CreateCart(float distance)
    {
        // Cart.prefab in Steam build 25364265: Vagon on the root references the
        // Container child (item layer). Physical colliders are vehicle siblings.
        var root = At(distance, "Default");
        var vagon = root.AddComponent<Vagon>();
        var hold = At(distance, "item", root);
        vagon.m_container = hold.AddComponent<Container>();
        hold.AddComponent<Collider>();
        At(distance, "vehicle", root).AddComponent<Collider>();
        return vagon.m_container;
    }

    private static GameObject At(float distance, string layer, GameObject parent = null)
    {
        var obj = new GameObject { layer = LayerMask.NameToLayer(layer), Parent = parent };
        obj.transform.position = new Vector3(distance, 0f, 0f);
        return obj;
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
