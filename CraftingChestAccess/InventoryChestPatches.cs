using System;
using HarmonyLib;
using SmartCraftStorage.Config;
using SmartCraftStorage.ItemMarking;
using SmartCraftStorage.Shared;

namespace SmartCraftStorage.CraftingChestAccess
{
    internal static class InventoryChestPatches
    {
        private const string FoodPreparationStationName = "$piece_preptable";

        private static bool IsCraftingOrBuildingContext(Inventory instance, out Player player)
        {
            player = Player.m_localPlayer;
            if (player == null || instance != player.GetInventory())
            {
                return false;
            }

            return player.GetCurrentCraftingStation() != null
                || player.InPlaceMode()
                || IsFoodPreparationTableCraftingContext(player);
        }

        private static bool IsFoodPreparationTableCraftingContext(Player player)
        {
            var inventoryGui = InventoryGui.instance;
            return inventoryGui != null
                && InventoryGui.IsVisible()
                && inventoryGui.InCraftTab()
                && CraftingStation.HaveBuildStationInRange(FoodPreparationStationName, player.transform.position) != null;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetFirstRequiredItem))]
        private static class GetFirstRequiredItemPatch
        {
            // Variable-yield recipes use this result to derive their output from the
            // selected input's quality. CountItems can include chest items, but the
            // game's final GetItem lookup only searches the player inventory.
            private static void Postfix(Player __instance, Inventory inventory, Recipe recipe, int qualityLevel,
                ref int amount, ref int extraAmount, int craftMultiplier, ref ItemDrop.ItemData __result)
            {
                try
                {
                    if (__result != null || recipe == null || !recipe.m_requireOnlyOneIngredient
                        || recipe.m_resources == null
                        || !IsCraftingOrBuildingContext(inventory, out var player)
                        || player != __instance)
                    {
                        return;
                    }

                    var currentStation = player.GetCurrentCraftingStation();
                    foreach (var requirement in recipe.m_resources)
                    {
                        if (requirement == null || requirement.m_resItem == null
                            || (currentStation != null && currentStation.m_upgrader != requirement.m_upgraderResource)
                            || (currentStation == null && requirement.m_upgraderResource))
                        {
                            continue;
                        }

                        string itemName = requirement.m_resItem.m_itemData.m_shared.m_name;
                        int requiredAmount = requirement.GetAmount(qualityLevel) * craftMultiplier;
                        for (int quality = 0; quality <= requirement.m_resItem.m_itemData.m_shared.m_maxQuality; quality++)
                        {
                            if (inventory.GetItem(itemName, quality) != null)
                            {
                                continue;
                            }

                            var chestItem = FindNearbyChestItem(player, itemName, quality, requiredAmount);
                            if (chestItem == null)
                            {
                                continue;
                            }

                            amount = requiredAmount;
                            extraAmount = requirement.m_extraAmountOnlyOneIngredient;
                            __result = chestItem;
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            private static ItemDrop.ItemData FindNearbyChestItem(Player player, string itemName, int quality, int requiredAmount)
            {
                ItemDrop.ItemData candidate = null;
                int available = 0;
                foreach (var container in NearbyContainers.Find(
                             player.transform.position, ModConfig.CraftingChestRadius.Value, player))
                {
                    foreach (var item in container.GetInventory().GetAllItems())
                    {
                        if (!ItemFlags.IsLocked(item) && item.m_shared.m_name == itemName
                            && item.m_quality == quality && item.m_stack > 0
                            && item.m_worldLevel >= Game.m_worldLevel)
                        {
                            candidate ??= item;
                            available += item.m_stack;
                        }
                    }
                }

                return available >= requiredAmount ? candidate : null;
            }
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
                        chestTotal += UnlockedInventory.CountItems(container.GetInventory(), name, quality, matchWorldLevel);
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

                    foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
                    {
                        if (UnlockedInventory.HaveItem(container.GetInventory(), name, matchWorldLevel))
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

                    int haveInInventory = SumMatchingStack(__instance, name, itemQuality, worldLevelBased);
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
                        int haveInChest = UnlockedInventory.CountItems(chestInventory, name, itemQuality, worldLevelBased);
                        int takeFromChest = Math.Min(remaining, haveInChest);

                        if (takeFromChest > 0)
                        {
                            int removed = UnlockedInventory.RemoveItems(chestInventory, name, takeFromChest, itemQuality, worldLevelBased);
                            if (removed > 0)
                            {
                                ChestCountCache.Invalidate();
                                remaining -= removed;
                                amount += removed;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            private static int SumMatchingStack(Inventory inventory, string name, int quality, bool matchWorldLevel)
            {
                int total = 0;
                foreach (var item in inventory.GetAllItems())
                {
                    if (item.m_shared.m_name == name && (quality < 0 || item.m_quality == quality)
                        && (!matchWorldLevel || item.m_worldLevel >= Game.m_worldLevel))
                    {
                        total += item.m_stack;
                    }
                }
                return total;
            }
        }
    }
}
