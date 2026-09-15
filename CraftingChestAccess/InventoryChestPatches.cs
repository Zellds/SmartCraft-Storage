using System;
using HarmonyLib;
using SmartCraftStorage.Config;
using SmartCraftStorage.Shared;

namespace SmartCraftStorage.CraftingChestAccess
{
    internal static class InventoryChestPatches
    {
        private static bool IsCraftingOrBuildingContext(Inventory instance, out Player player)
        {
            player = Player.m_localPlayer;
            if (player == null || instance != player.GetInventory())
            {
                return false;
            }

            return player.GetCurrentCraftingStation() != null || player.InPlaceMode();
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems), new[] { typeof(string), typeof(int), typeof(bool) })]
        private static class CountItemsPatch
        {
            private static void Postfix(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result)
            {
                try
                {
                    if (!IsCraftingOrBuildingContext(__instance, out var player))
                    {
                        return;
                    }

                    // Only the chest half is cached; __result already holds the
                    // player's own live count, so what you carry is never stale.
                    if (ChestCountCache.TryGet(name, quality, matchWorldLevel, out int cached))
                    {
                        __result += cached;
                        return;
                    }

                    int chestTotal = 0;
                    foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
                    {
                        chestTotal += container.GetInventory().CountItems(name, quality, matchWorldLevel);
                    }

                    ChestCountCache.Store(name, quality, matchWorldLevel, chestTotal);
                    __result += chestTotal;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveItem), new[] { typeof(string), typeof(bool) })]
        private static class HaveItemPatch
        {
            private static void Postfix(Inventory __instance, string name, bool matchWorldLevel, ref bool __result)
            {
                try
                {
                    if (__result || !IsCraftingOrBuildingContext(__instance, out var player))
                    {
                        return;
                    }

                    foreach (var container in NearbyChestCache.Get(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
                    {
                        if (container.GetInventory().HaveItem(name, matchWorldLevel))
                        {
                            __result = true;
                            return;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
        private static class RemoveItemPatch
        {
            private static void Prefix(Inventory __instance, string name, ref int amount, int itemQuality, bool worldLevelBased)
            {
                try
                {
                    if (!IsCraftingOrBuildingContext(__instance, out var player))
                    {
                        return;
                    }

                    int haveInInventory = SumMatchingStack(__instance, name, itemQuality);
                    if (amount <= haveInInventory)
                    {
                        return;
                    }

                    int remaining = amount - haveInInventory;
                    amount = haveInInventory;

                    foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
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
                        int haveInChest = SumMatchingStack(chestInventory, name, itemQuality);
                        int takeFromChest = Math.Min(remaining, haveInChest);

                        if (takeFromChest > 0)
                        {
                            chestInventory.RemoveItem(name, takeFromChest, itemQuality, worldLevelBased);
                            ChestCountCache.Invalidate();
                            remaining -= takeFromChest;
                            amount += takeFromChest;
                            NearbyChestCache.Invalidate();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            private static int SumMatchingStack(Inventory inventory, string name, int quality)
            {
                int total = 0;
                foreach (var item in inventory.GetAllItems())
                {
                    if (item.m_shared.m_name == name && (quality < 0 || item.m_quality == quality))
                    {
                        total += item.m_stack;
                    }
                }
                return total;
            }
        }
    }
}
