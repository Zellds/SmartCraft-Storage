using System.Collections.Generic;
using HarmonyLib;
using SmartCraftStorage.Config;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.PlantHarvest
{
    internal static class PlantHarvestPatch
    {
        // Unlike every station/animal patch in the mod, a ripe Pickable has no tick of
        // its own to hook into — so this is the one place we drive our own periodic
        // scan instead of piggybacking on a vanilla InvokeRepeating. Throttled here so
        // the sphere-cast doesn't run every frame.
        [HarmonyPatch(typeof(Player), "Update")]
        private static class AutoHarvestTriggerPatch
        {
            private const float ScanInterval = 3f;
            private static float _nextScanTime;

            private static void Postfix(Player __instance)
            {
                try
                {
                    if (!PlantHarvestConfig.PlantAutoHarvest.Value)
                    {
                        return;
                    }

                    if (__instance != Player.m_localPlayer)
                    {
                        return;
                    }

                    if (Time.time < _nextScanTime)
                    {
                        return;
                    }

                    _nextScanTime = Time.time + ScanInterval;

                    var hits = Physics.OverlapSphere(__instance.transform.position, PlantHarvestConfig.PlantHarvestRadius.Value);
                    var seen = new HashSet<Pickable>();

                    foreach (var hit in hits)
                    {
                        var pickable = hit.GetComponentInParent<Pickable>();
                        if (pickable == null || !seen.Add(pickable))
                        {
                            continue;
                        }

                        // m_harvestable is what separates a cultivated crop from a wild
                        // pickable (berry bush, mushroom, rock) in the game's own data.
                        // Not verifiable against the real prefab values from decompiled
                        // code alone, which is exactly why this feature ships off by
                        // default and marked as in test.
                        if (!pickable.m_harvestable || !pickable.CanBePicked())
                        {
                            continue;
                        }

                        pickable.Interact(__instance, false, false);
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        [HarmonyPatch(typeof(Pickable), "Drop")]
        private static class CollectRedirectPatch
        {
            private static bool Prefix(Pickable __instance, GameObject prefab, ref int stack)
            {
                try
                {
                    if (!PlantHarvestConfig.PlantAutoHarvest.Value)
                    {
                        return true;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        return true;
                    }

                    var itemDrop = prefab.GetComponent<ItemDrop>();
                    if (itemDrop == null)
                    {
                        return true;
                    }

                    int remaining = stack;
                    string itemName = itemDrop.m_itemData.m_shared.m_name;

                    foreach (var container in OutputChests.OrderForOutput(
                                 NearbyContainers.Find(__instance.transform.position, PlantHarvestConfig.PlantHarvestRadius.Value, player),
                                 itemName,
                                 ModConfig.ChestOutputStrategyConfig.Value))
                    {
                        if (remaining <= 0)
                        {
                            break;
                        }

                        // Ask about room before claiming the chest: AddItem on a full
                        // container logs "Trying to add item to occupied slot -1, -1" as
                        // an error rather than declining, and claiming ownership of a
                        // chest we then can't write to is needless churn on a server.
                        // Sorted output makes this matter — the sorted chest is the one
                        // that fills up, so it's now the first candidate tried.
                        var chestInventory = container.GetInventory();
                        if (!NearbyContainers.HasRoomFor(chestInventory, itemName))
                        {
                            continue;
                        }

                        if (!NearbyContainers.TryClaimWriteAccess(container))
                        {
                            continue;
                        }

                        int before = chestInventory.CountItems(itemName);
                        chestInventory.AddItem(prefab, remaining);
                        int added = chestInventory.CountItems(itemName) - before;
                        if (added > 0)
                        {
                            remaining -= added;
                        }
                    }

                    // Only what's left (if anything) drops on the ground when we let
                    // vanilla run, same partial-fallback the smelter/kiln collector uses.
                    stack = remaining;
                    return remaining > 0;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    return true;
                }
            }
        }
    }
}
