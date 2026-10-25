using System.Collections.Generic;
using SmartCraftStorage.Config;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.QuickStack
{
    internal static class QuickStackService
    {
        public static void Execute(Player player)
        {
            var inventory = player.GetInventory();
            var containers = new List<Container>(NearbyContainers.Find(player.transform.position, ModConfig.QuickStackRadius.Value, player));

            if (containers.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$smartcraft_no_chest_nearby"));
                return;
            }

            int itemsMoved = 0;
            var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());

            foreach (var item in items)
            {
                if (item.m_equipped || SmartCraftStorage.ItemMarking.ItemFlags.IsLocked(item))
                {
                    continue;
                }

                itemsMoved += MoveItemIntoMatchingStacks(inventory, item, containers);
            }

            player.Message(MessageHud.MessageType.Center, itemsMoved > 0
                ? Localization.instance.Localize("$smartcraft_quickstack_success", itemsMoved.ToString())
                : Localization.instance.Localize("$smartcraft_quickstack_nothing"));
        }

        private static int MoveItemIntoMatchingStacks(Inventory playerInventory, ItemDrop.ItemData item, List<Container> containers)
        {
            int originalStack = item.m_stack;

            foreach (var container in containers)
            {
                if (item.m_stack <= 0)
                {
                    break;
                }

                var chestInventory = container.GetInventory();
                var existingMatches = chestInventory.GetAllItems().FindAll(i =>
                    i.m_shared.m_name == item.m_shared.m_name &&
                    i.m_quality == item.m_quality);

                if (existingMatches.Count == 0)
                {
                    continue;
                }

                if (!NearbyContainers.TryClaimWriteAccess(container))
                {
                    continue;
                }

                // Claiming reloads the chest, which recreates its items: the stacks
                // found above (and their positions) may no longer exist.
                existingMatches = chestInventory.GetAllItems().FindAll(i =>
                    i.m_shared.m_name == item.m_shared.m_name &&
                    i.m_quality == item.m_quality);

                if (existingMatches.Count == 0)
                {
                    continue;
                }

                foreach (var existingStack in existingMatches)
                {
                    if (item.m_stack <= 0)
                    {
                        break;
                    }

                    int freeSpace = existingStack.m_shared.m_maxStackSize - existingStack.m_stack;
                    if (freeSpace <= 0)
                    {
                        continue;
                    }

                    int amountToMove = Mathf.Min(freeSpace, item.m_stack);
                    int before = item.m_stack;
                    if (!chestInventory.MoveItemToThis(playerInventory, item, amountToMove, existingStack.m_gridPos.x, existingStack.m_gridPos.y)
                        || item.m_stack >= before)
                    {
                        // Merging can fail legitimately (Inventory.AddItem also compares
                        // m_worldLevel, which the match above does not), or because another
                        // mod vetoed the move. Either way this stack is not an option.
                        continue;
                    }
                }

                while (item.m_stack > 0)
                {
                    var newSlot = chestInventory.FindEmptySlot(false);
                    if (newSlot.x < 0)
                    {
                        break;
                    }

                    int amountToMove = Mathf.Min(item.m_shared.m_maxStackSize, item.m_stack);
                    int before = item.m_stack;
                    // MoveItemToThis can be vetoed by other mods (EquipmentSlots' ForeignMoveGuard
                    // returns false for items in its slot rows or padlocked cells) and then does
                    // not touch m_stack. Without this check the loop would spin forever on the
                    // same empty slot and freeze the game.
                    if (!chestInventory.MoveItemToThis(playerInventory, item, amountToMove, newSlot.x, newSlot.y)
                        || item.m_stack >= before)
                    {
                        DebugLog.Log($"[SmartCraft-Storage] Quick-stack: move of {item.m_shared.m_name} refused by the game or another mod; leaving it in place.");
                        return originalStack - item.m_stack;
                    }
                }
            }

            return originalStack - item.m_stack;
        }
    }
}
