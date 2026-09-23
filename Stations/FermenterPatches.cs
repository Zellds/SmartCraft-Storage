using HarmonyLib;
using SmartCraftStorage.Config;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    internal static class FermenterPatches
    {
        [HarmonyPatch(typeof(Fermenter), "SlowUpdate")]
        private static class AutoProcessPatch
        {
            private static void Postfix(Fermenter __instance)
            {
                try
                {
                    if (__instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner())
                    {
                        return;
                    }

                    float durationOverride = StationConfig.FermenterDurationOverride.Value;
                    if (durationOverride > 0f)
                    {
                        __instance.m_fermentationDuration = durationOverride;
                    }

                    if (!StationConfig.FermenterAutoProcess.Value)
                    {
                        return;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        return;
                    }

                    switch (__instance.GetStatus())
                    {
                        case Fermenter.Status.Empty:
                            TryRefuel(__instance, player);
                            break;
                        case Fermenter.Status.Ready:
                            __instance.m_nview.InvokeRPC("RPC_Tap");
                            break;
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            private static void TryRefuel(Fermenter fermenter, Player player)
            {
                // Mirrors the roof/rain checks Interact() does before accepting a base by
                // hand — adding one anyway would just sit there not progressing.
                if (!fermenter.m_hasRoof || fermenter.m_exposed)
                {
                    return;
                }

                foreach (var container in NearbyContainers.Find(fermenter.transform.position, StationConfig.FermenterRadius.Value, player))
                {
                    var chestInventory = container.GetInventory();
                    var item = FindCookableItem(fermenter, chestInventory);
                    if (item == null || !fermenter.IsItemAllowed(item))
                    {
                        continue;
                    }

                    if (!NearbyContainers.TryClaimWriteAccess(container))
                    {
                        continue;
                    }

                    // RPC_AddItem takes a name hash, not the prefab name string (unlike
                    // RPC_AddOre on Smelter) — matches vanilla's own AddItem() call.
                    int nameHash = item.m_dropPrefab.name.GetStableHashCode();
                    bool cheated = item.m_cheated;
                    if (UnlockedInventory.RemoveItem(chestInventory, item, 1) != 1)
                    {
                        continue;
                    }
                    fermenter.m_nview.InvokeRPC("RPC_AddItem", nameHash, cheated);
                    return;
                }
            }

            private static ItemDrop.ItemData FindCookableItem(Fermenter fermenter, Inventory inventory)
            {
                foreach (var conversion in fermenter.m_conversion)
                {
                    if (conversion.m_from == null)
                    {
                        continue;
                    }
                    var item = UnlockedInventory.FindItem(inventory, conversion.m_from.m_itemData.m_shared.m_name);
                    if (item != null)
                    {
                        return item;
                    }
                }
                return null;
            }
        }

        [HarmonyPatch(typeof(Fermenter), "DelayedTap")]
        private static class CollectRedirectPatch
        {
            private static bool Prefix(Fermenter __instance)
            {
                try
                {
                    if (!StationConfig.FermenterAutoProcess.Value)
                    {
                        return true;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        return true;
                    }

                    var conversion = __instance.GetItemConversion(__instance.m_delayedTapItem);
                    if (conversion == null || conversion.m_to == null)
                    {
                        return true;
                    }

                    __instance.m_spawnEffects.Create(__instance.m_outputPoint.transform.position, Quaternion.identity);

                    int remaining = conversion.m_producedItems;
                    string itemName = conversion.m_to.m_itemData.m_shared.m_name;

                    foreach (var container in OutputChests.OrderForOutput(
                                 NearbyContainers.Find(__instance.transform.position, StationConfig.FermenterRadius.Value, player),
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
                        chestInventory.AddItem(conversion.m_to.gameObject, remaining);
                        int added = chestInventory.CountItems(itemName) - before;
                        if (added > 0)
                        {
                            remaining -= added;
                        }
                    }

                    if (remaining <= 0)
                    {
                        return false;
                    }

                    // Leftover with no chest space: drop it, exactly like vanilla does.
                    bool cheated = (__instance.m_delayedTapItemCheated || __instance.m_nview.GetZDO().GetBool(ZDOVars.s_cheated)) && !PlayerProfile.s_bypassCheatChecks;
                    float heightStep = 0.3f;
                    for (int i = 0; i < remaining; i++)
                    {
                        var position = __instance.m_outputPoint.position + Vector3.up * (heightStep * i);
                        var spawned = Object.Instantiate(conversion.m_to, position, Quaternion.identity);
                        ItemDrop.OnCreateNew(spawned, cheated);
                    }

                    return false;
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
