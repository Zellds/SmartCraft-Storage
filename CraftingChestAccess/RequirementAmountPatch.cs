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

                KeepInsideSlot(amountText);
                amountText.text = required + " <size=70%>(" + Abbreviate(available) + ")</size>";
            }
            catch (System.Exception ex)
            {
                _disabled = true;
                Debug.LogError("[SmartCraft-Storage] Failed to show available amounts; the feature is now off "
                    + "for this session. Set ShowAvailableAmounts=false to silence this.");
                Debug.LogException(ex);
            }
        }

        // The label is only as wide as the ingredient slot, and TMP's default overflow
        // mode draws past that rect instead of clipping it, so a pair like "4 (1652)"
        // runs over the neighbouring ingredient. There is no character limit to raise —
        // the limit is the rect width in pixels. Auto-sizing makes TMP shrink the line
        // until it fits that rect, down to 60% of the prefab size, and leaves anything
        // short enough at the original size.
        private static void KeepInsideSlot(TMP_Text label)
        {
            if (label.enableAutoSizing)
            {
                return;
            }

            // A rect this narrow would force every label to the minimum size, which
            // would look worse than the overflow. Leave TMP alone and let the smaller
            // brackets do what they can.
            if (label.rectTransform.rect.width < 20f)
            {
                return;
            }

            // Read the size before auto-sizing is on: from then on fontSize reports
            // whatever TMP last computed for the current text, not the prefab value.
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = label.fontSize * 0.6f;
            // Without this the space before the bracket is a wrap point, and TMP would
            // break the line rather than shrink it.
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.richText = true;
            label.enableAutoSizing = true;
        }

        // Rows are pooled and reused, so the settings above outlive this feature being
        // switched off mid-session. That is harmless: a bare required amount is short
        // enough to render at fontSizeMax, which is the size the prefab shipped with.

        // Five and six digit stacks are normal once chests are counted, and every digit
        // costs width. Abbreviating past four gives the label a bounded worst case.
        private static string Abbreviate(int amount)
        {
            if (amount < 10000)
            {
                return amount.ToString();
            }

            if (amount < 1000000)
            {
                return (amount / 1000) + "k";
            }

            return (amount / 1000000) + "M";
        }
    }
}
