using System.Collections.Generic;
using HarmonyLib;
using SmartCraftStorage.Shared;

namespace SmartCraftStorage.Stations
{
    internal static class SmelterPatches
    {
        [HarmonyPatch(typeof(Smelter), "UpdateSmelter")]
        private static class RefuelPatch
        {
            private static void Postfix(Smelter __instance)
            {
                try
                {
                    bool isKiln = KilnDetection.IsKiln(__instance);
                    bool enabled = isKiln ? StationConfig.KilnAutoRefuel.Value : StationConfig.SmelterAutoRefuel.Value;
                    if (!enabled
                        || __instance.m_nview == null || !__instance.m_nview.IsValid()
                        || !__instance.m_nview.IsOwner())
                    {
                        return;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        return;
                    }

                    // UpdateSmelter runs once a second on every smelter and kiln in
                    // range. Check whether there is any room to fill before searching
                    // for chests, so a base full of topped-up smelters costs nothing.
                    bool wantsOre = __instance.GetQueueSize() < (isKiln ? StationConfig.KilnWoodBuffer.Value : __instance.m_maxOre);
                    bool wantsFuel = __instance.m_maxFuel > 0 && __instance.m_fuelItem != null
                        && __instance.GetFuel() < __instance.m_maxFuel;
                    if (!wantsOre && !wantsFuel)
                    {
                        return;
                    }

                    var containers = new List<Container>(
                        NearbyContainers.Find(__instance.transform.position, StationConfig.SmelterKilnRadius.Value, player));

                    RefuelOre(__instance, isKiln, containers);
                    RefuelFuel(__instance, containers);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            private static void RefuelOre(Smelter smelter, bool isKiln, List<Container> containers)
            {
                int targetQueue = isKiln ? StationConfig.KilnWoodBuffer.Value : smelter.m_maxOre;
                string coalName = isKiln ? KilnDetection.GetCoalItemName(smelter) : null;

                while (smelter.GetQueueSize() < targetQueue)
                {
                    if (isKiln && KilnCoalCapReached(coalName, containers))
                    {
                        break;
                    }

                    if (!TryPullOneOre(smelter, isKiln, containers))
                    {
                        break;
                    }
                }
            }

            private static bool TryPullOneOre(Smelter smelter, bool isKiln, List<Container> containers)
            {
                ItemDrop requiredWood = null;
                if (isKiln && StationConfig.KilnRegularWoodOnly.Value)
                {
                    requiredWood = KilnDetection.GetRegularWoodItem(smelter);
                }

                foreach (var container in containers)
                {
                    var chestInventory = container.GetInventory();

                    if (requiredWood != null)
                    {
                        string woodName = requiredWood.m_itemData.m_shared.m_name;
                        if (!chestInventory.HaveItem(woodName))
                        {
                            continue;
                        }

                        if (!NearbyContainers.TryClaimWriteAccess(container))
                        {
                            continue;
                        }

                        chestInventory.RemoveItem(woodName, 1);
                        smelter.m_nview.InvokeRPC("RPC_AddOre", requiredWood.gameObject.name, false);
                        return true;
                    }

                    var item = smelter.FindCookableItem(chestInventory);
                    if (item == null || !smelter.IsItemAllowed(item.m_dropPrefab.name))
                    {
                        continue;
                    }

                    if (!NearbyContainers.TryClaimWriteAccess(container))
                    {
                        continue;
                    }

                    string prefabName = item.m_dropPrefab.name;
                    bool cheated = item.m_cheated;
                    chestInventory.RemoveItem(item, 1);
                    smelter.m_nview.InvokeRPC("RPC_AddOre", prefabName, cheated);
                    return true;
                }

                return false;
            }

            private static void RefuelFuel(Smelter smelter, List<Container> containers)
            {
                if (smelter.m_maxFuel <= 0 || smelter.m_fuelItem == null)
                {
                    return;
                }

                string fuelName = smelter.m_fuelItem.m_itemData.m_shared.m_name;

                while (smelter.GetFuel() < smelter.m_maxFuel)
                {
                    bool pulled = false;

                    foreach (var container in containers)
                    {
                        var chestInventory = container.GetInventory();
                        if (!chestInventory.HaveItem(fuelName))
                        {
                            continue;
                        }

                        if (!NearbyContainers.TryClaimWriteAccess(container))
                        {
                            continue;
                        }

                        chestInventory.RemoveItem(fuelName, 1);
                        smelter.m_nview.InvokeRPC("RPC_AddFuel");
                        pulled = true;
                        break;
                    }

                    if (!pulled)
                    {
                        break;
                    }
                }
            }

            private static bool KilnCoalCapReached(string coalName, List<Container> containers)
            {
                if (coalName == null)
                {
                    return false;
                }

                int totalCoal = 0;
                foreach (var container in containers)
                {
                    totalCoal += container.GetInventory().CountItems(coalName);
                    if (totalCoal >= StationConfig.KilnMaxCoalInChest.Value)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(Smelter), "Spawn")]
        private static class CollectPatch
        {
            private static bool Prefix(Smelter __instance, string ore, ref int stack)
            {
                try
                {
                    bool isKiln = KilnDetection.IsKiln(__instance);
                    bool enabled = isKiln ? StationConfig.KilnAutoCollect.Value : StationConfig.SmelterAutoCollect.Value;
                    if (!enabled)
                    {
                        return true;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        return true;
                    }

                    var conversion = __instance.GetItemConversion(ore);
                    if (conversion == null || conversion.m_to == null)
                    {
                        return true;
                    }

                    int remaining = stack;

                    if (isKiln)
                    {
                        remaining = FeedNearbySmelters(__instance, remaining);
                    }

                    if (remaining <= 0)
                    {
                        stack = 0;
                        return false;
                    }

                    string itemName = conversion.m_to.m_itemData.m_shared.m_name;

                    foreach (var container in NearbyContainers.Find(__instance.transform.position, StationConfig.SmelterKilnRadius.Value, player))
                    {
                        if (remaining <= 0)
                        {
                            break;
                        }

                        if (!NearbyContainers.TryClaimWriteAccess(container))
                        {
                            continue;
                        }

                        var chestInventory = container.GetInventory();
                        int before = chestInventory.CountItems(itemName);
                        chestInventory.AddItem(conversion.m_to.gameObject, remaining);
                        int added = chestInventory.CountItems(itemName) - before;
                        if (added > 0)
                        {
                            remaining -= added;
                        }
                    }

                    stack = remaining;
                    return remaining > 0;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    return true;
                }
            }

            private static int FeedNearbySmelters(Smelter kiln, int amount)
            {
                string coalName = KilnDetection.GetCoalItemName(kiln);
                var candidates = new List<Smelter>();
                var seen = new HashSet<Smelter>();
                var kilnPosition = kiln.transform.position;
                int hitCount = NearbyContainers.OverlapNearby(kilnPosition, StationConfig.SmelterKilnRadius.Value);

                for (int i = 0; i < hitCount; i++)
                {
                    var smelter = NearbyContainers.Hits[i].GetComponentInParent<Smelter>();
                    if (smelter == null || smelter == kiln || !seen.Add(smelter) || KilnDetection.IsKiln(smelter))
                    {
                        continue;
                    }
                    if (smelter.m_fuelItem == null || smelter.GetFuel() >= smelter.m_maxFuel)
                    {
                        continue;
                    }
                    if (smelter.m_fuelItem.m_itemData.m_shared.m_name != coalName)
                    {
                        continue;
                    }
                    if (!PrivateArea.CheckAccess(smelter.transform.position, 0f, false))
                    {
                        continue;
                    }

                    candidates.Add(smelter);
                }

                if (candidates.Count == 0)
                {
                    return amount;
                }

                if (StationConfig.KilnFeedStrategyConfig.Value == KilnFeedStrategy.LeastFuelFirst)
                {
                    candidates.Sort((a, b) => a.GetFuel().CompareTo(b.GetFuel()));
                }
                else
                {
                    // Squared distance orders identically and skips the sqrt.
                    candidates.Sort((a, b) => (a.transform.position - kilnPosition).sqrMagnitude
                        .CompareTo((b.transform.position - kilnPosition).sqrMagnitude));
                }

                foreach (var smelter in candidates)
                {
                    while (amount > 0 && smelter.GetFuel() < smelter.m_maxFuel)
                    {
                        if (!NearbyContainers.TryClaimWriteAccess(smelter.m_nview))
                        {
                            break;
                        }

                        smelter.m_nview.InvokeRPC("RPC_AddFuel");
                        amount--;
                    }

                    if (amount <= 0)
                    {
                        break;
                    }
                }

                return amount;
            }
        }
    }
}
