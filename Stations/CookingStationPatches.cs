using System.Collections.Generic;
using HarmonyLib;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    internal static class CookingStationPatches
    {
        [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        private static class RefuelPatch
        {
            private static void Postfix(CookingStation __instance)
            {
                try
                {
                    if (!StationConfig.CookingStationAutoRefuel.Value
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

                    // UpdateCooking runs once a second on every cooking station in
                    // range. Check whether there is any room to fill before searching
                    // for chests, so an idle full station costs nothing.
                    bool wantsFood = __instance.GetFreeSlot() != -1;
                    bool wantsFuel = __instance.m_useFuel && __instance.m_fuelItem != null
                        && __instance.GetFuel() < __instance.m_maxFuel;
                    if (!wantsFood && !wantsFuel)
                    {
                        return;
                    }

                    var containers = new List<Container>(
                        NearbyContainers.Find(__instance.transform.position, StationConfig.CookingStationRadius.Value, player));

                    RefuelFood(__instance, containers);
                    RefuelFuel(__instance, containers);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            private static void RefuelFood(CookingStation station, List<Container> containers)
            {
                while (station.GetFreeSlot() != -1)
                {
                    bool pulled = false;

                    foreach (var container in containers)
                    {
                        var chestInventory = container.GetInventory();
                        var item = station.FindCookableItem(chestInventory);
                        if (item == null || !station.IsItemAllowed(item.m_dropPrefab.name))
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
                        station.m_nview.InvokeRPC("RPC_AddItem", prefabName, cheated);
                        pulled = true;
                        break;
                    }

                    if (!pulled)
                    {
                        break;
                    }
                }
            }

            private static void RefuelFuel(CookingStation station, List<Container> containers)
            {
                if (!station.m_useFuel || station.m_fuelItem == null)
                {
                    return;
                }

                string fuelName = station.m_fuelItem.m_itemData.m_shared.m_name;

                while (station.GetFuel() < station.m_maxFuel)
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
                        station.m_nview.InvokeRPC("RPC_AddFuel");
                        pulled = true;
                        break;
                    }

                    if (!pulled)
                    {
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        private static class AutoCollectTriggerPatch
        {
            private static void Postfix(CookingStation __instance)
            {
                try
                {
                    if (!StationConfig.CookingStationAutoCollect.Value
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

                    while (__instance.HaveDoneItem())
                    {
                        __instance.OnInteract(player);
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        [HarmonyPatch(typeof(CookingStation), "SpawnItem")]
        private static class CollectRedirectPatch
        {
            private static bool Prefix(CookingStation __instance, string name, int slot, Vector3 userPoint, bool cheated)
            {
                try
                {
                    if (!StationConfig.CookingStationAutoCollect.Value)
                    {
                        return true;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        return true;
                    }

                    var itemPrefab = ObjectDB.instance.GetItemPrefab(name);
                    if (itemPrefab == null)
                    {
                        return true;
                    }

                    var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                    if (itemDrop == null)
                    {
                        return true;
                    }

                    string itemName = itemDrop.m_itemData.m_shared.m_name;

                    foreach (var container in NearbyContainers.Find(__instance.transform.position, StationConfig.CookingStationRadius.Value, player))
                    {
                        // Ask before adding: Inventory.AddItem on a full container
                        // returns false *and* logs "Trying to add item to occupied
                        // slot -1, -1" as an error. Walking a row of full chests
                        // otherwise fills the log with errors that are not errors —
                        // and writing those out costs more than the check does.
                        var chestInventory = container.GetInventory();
                        if (!NearbyContainers.HasRoomFor(chestInventory, itemName))
                        {
                            continue;
                        }

                        if (!NearbyContainers.TryClaimWriteAccess(container))
                        {
                            continue;
                        }

                        if (chestInventory.AddItem(itemPrefab, 1))
                        {
                            return false;
                        }
                    }

                    return true;
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
