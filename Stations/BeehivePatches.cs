using System.Collections.Generic;
using HarmonyLib;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    internal static class BeehivePatches
    {
        // Set right before an auto-triggered Extract() call, and consumed (read and
        // cleared) at the top of CollectRedirectPatch. Lets that patch tell apart an
        // automatic collection from the player manually pressing E on the hive.
        private static bool _autoTriggered;

        [HarmonyPatch(typeof(Beehive), "UpdateBees")]
        private static class AutoCollectTriggerPatch
        {
            private static void Postfix(Beehive __instance)
            {
                try
                {
                    if (!StationConfig.BeehiveAutoCollect.Value
                        || __instance.m_nview == null || !__instance.m_nview.IsValid()
                        || !__instance.m_nview.IsOwner())
                    {
                        return;
                    }

                    int honeyLevel = __instance.GetHoneyLevel();
                    if (honeyLevel > 0)
                    {
                        DebugLog.Log($"[SmartCraftStorage] Beehive: auto-trigger firing, honeyLevel={honeyLevel}, "
                            + $"position={__instance.transform.position}");

                        _autoTriggered = true;
                        __instance.Extract();
                        // Mirrors what a manual interaction does in Beehive.Interact(),
                        // so the "bees harvested" stat keeps tracking correctly.
                        Game.instance.IncrementPlayerStat(PlayerStatType.BeesHarvested, honeyLevel);
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        [HarmonyPatch(typeof(Beehive), "RPC_Extract")]
        private static class CollectRedirectPatch
        {
            private static bool Prefix(Beehive __instance, long caller)
            {
                // Always consume the flag exactly once per call, before any early
                // return, so it can never leak into an unrelated later call.
                bool isAutoTriggered = _autoTriggered;
                _autoTriggered = false;

                try
                {
                    if (!StationConfig.BeehiveAutoCollect.Value)
                    {
                        return true;
                    }

                    int honeyLevel = __instance.GetHoneyLevel();
                    if (honeyLevel <= 0)
                    {
                        return true;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        DebugLog.Log("[SmartCraftStorage] Beehive: RPC_Extract fired with no local "
                            + $"player (autoTriggered={isAutoTriggered}), falling back to vanilla drop.");
                        return true;
                    }

                    // Same per-unit scaling the vanilla method applies before instantiating
                    // each item, just summed into one stack instead of spawned individually.
                    int totalHoney = 0;
                    for (int i = 0; i < honeyLevel; i++)
                    {
                        totalHoney += Game.instance.ScaleDrops(__instance.m_honeyItem.m_itemData, 1);
                    }

                    int remaining = totalHoney;
                    string itemName = __instance.m_honeyItem.m_itemData.m_shared.m_name;
                    // On a partial automatic collection, undo only the exact stacks
                    // changed by AddItem. Removing by item name can instead remove
                    // honey that was already in a nearly-full chest.
                    var stored = new List<(Inventory inventory, ItemDrop.ItemData item, int amount)>();

                    var nearbyContainers = new List<Container>(NearbyContainers.Find(__instance.transform.position, StationConfig.BeehiveRadius.Value, player));
                    DebugLog.Log($"[SmartCraftStorage] Beehive: RPC_Extract autoTriggered={isAutoTriggered}, "
                        + $"honeyLevel={honeyLevel}, totalHoney={totalHoney}, foundContainers={nearbyContainers.Count}, "
                        + $"radius={StationConfig.BeehiveRadius.Value}, position={__instance.transform.position}");

                    foreach (var container in nearbyContainers)
                    {
                        if (remaining <= 0)
                        {
                            break;
                        }

                        // AddItem on a full container logs "Trying to add item to
                        // occupied slot -1, -1" as an error rather than just declining,
                        // so ask first — room for one is enough, the count either side
                        // already handles a partial add.
                        // A remote chest's local inventory can be up to a second
                        // behind its ZDO. Refresh it before claiming ownership, so a
                        // full chest is never written back from a stale empty view.
                        Inventory chestInventory;
                        if (!NearbyContainers.TryGetFreshWriteInventory(container, out chestInventory))
                        {
                            continue;
                        }

                        if (!NearbyContainers.HasRoomFor(chestInventory, itemName))
                        {
                            continue;
                        }

                        var stackSizesBefore = new Dictionary<ItemDrop.ItemData, int>();
                        foreach (var item in chestInventory.GetAllItems())
                        {
                            if (item.m_shared.m_name == itemName)
                            {
                                stackSizesBefore[item] = item.m_stack;
                            }
                        }

                        int before = chestInventory.CountItems(itemName);
                        chestInventory.AddItem(__instance.m_honeyItem.gameObject, remaining);
                        int added = chestInventory.CountItems(itemName) - before;
                        if (added > 0)
                        {
                            remaining -= added;

                            int tracked = 0;
                            foreach (var item in chestInventory.GetAllItems())
                            {
                                if (item.m_shared.m_name != itemName)
                                {
                                    continue;
                                }

                                int stackSizeBefore;
                                stackSizesBefore.TryGetValue(item, out stackSizeBefore);
                                int addedToStack = item.m_stack - stackSizeBefore;
                                if (addedToStack <= 0)
                                {
                                    continue;
                                }

                                int amountToUndo = Mathf.Min(addedToStack, added - tracked);
                                stored.Add((chestInventory, item, amountToUndo));
                                tracked += amountToUndo;
                                if (tracked >= added)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    DebugLog.Log($"[SmartCraftStorage] Beehive: after storage attempt, remaining={remaining}, "
                        + $"storedInto={stored.Count} container(s), autoTriggered={isAutoTriggered}");

                    if (remaining <= 0)
                    {
                        __instance.m_spawnEffect.Create(__instance.m_spawnPoint.position, Quaternion.identity);
                        __instance.ResetLevel();
                        DebugLog.Log("[SmartCraftStorage] Beehive: fully stored, no drop.");
                        return false;
                    }

                    if (isAutoTriggered)
                    {
                        // Couldn't fit everything nearby: undo whatever partial storage
                        // just happened and leave the hive's honey queued as-is instead
                        // of ever dropping any of it on the ground, where nobody may be
                        // around to notice it despawn.
                        foreach (var (inventory, item, amount) in stored)
                        {
                            inventory.RemoveItem(item, amount);
                        }

                        DebugLog.Log("[SmartCraftStorage] Beehive: auto-triggered, couldn't fit everything, "
                            + "rolled back partial storage, honey stays queued (no drop).");
                        return false;
                    }

                    // Manual interaction: keep whatever was already stored above, and
                    // drop only the leftover ourselves. RPC_Extract takes no adjustable
                    // count to hand back to vanilla, so falling through here would
                    // duplicate whatever was already stored.
                    DebugLog.Log($"[SmartCraftStorage] Beehive: NOT auto-triggered (manual path), "
                        + $"dropping {remaining} leftover on the ground.");
                    __instance.m_spawnEffect.Create(__instance.m_spawnPoint.position, Quaternion.identity);
                    for (int i = 0; i < remaining; i++)
                    {
                        var offset = Random.insideUnitCircle * 0.5f;
                        var position = __instance.m_spawnPoint.position + new Vector3(offset.x, 0.25f * i, offset.y);
                        var spawned = Object.Instantiate(__instance.m_honeyItem, position, Quaternion.identity).GetComponent<ItemDrop>();
                        spawned.SetStack(1);
                        ItemDrop.OnCreateNew(spawned);
                    }

                    __instance.ResetLevel();
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
