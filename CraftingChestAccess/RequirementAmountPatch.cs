using HarmonyLib;
using SmartCraftStorage.Config;
using TMPro;
using UnityEngine;

namespace SmartCraftStorage.CraftingChestAccess
{
    // Both the crafting panel and the build HUD render their ingredient rows through
    // InventoryGui.SetupRequirement, which prints the required amount and nothing else.
    // Inventory.CountItems is already patched to include nearby chests, so the number
    // the game looks up right there is what the player can actually spend — this only
    // puts it on screen, in brackets after the requirement.
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    internal static class RequirementAmountPatch
    {
        // This runs once per ingredient per frame. If it ever starts throwing (a game
        // update moves the label, say) logging every frame would cost more than the
        // feature is worth, so it reports once and takes itself out.
        private static bool _disabled;

        private static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, int quality, int craftMultiplier, bool __result)
        {
            if (_disabled || !__result || !ModConfig.ShowAvailableAmounts.Value)
            {
                return;
            }

            try
            {
                if (player == null || req == null || req.m_resItem == null)
                {
                    return;
                }

                var amountLabel = elementRoot.Find("res_amount");
                if (amountLabel == null)
                {
                    return;
                }

                var amountText = amountLabel.GetComponent<TMP_Text>();
                if (amountText == null)
                {
                    return;
                }

                int required = req.GetAmount(quality) * craftMultiplier;
                int available = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);

                amountText.text = required + " (" + available + ")";
            }
            catch (System.Exception ex)
            {
                _disabled = true;
                Debug.LogError("[SmartCraft-Storage] Failed to show available amounts; the feature is now off "
                    + "for this session. Set ShowAvailableAmounts=false to silence this.");
                Debug.LogException(ex);
            }
        }
    }
}
