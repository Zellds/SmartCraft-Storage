using BepInEx.Configuration;

namespace SmartCraftStorage.Config
{
    internal static class ModConfig
    {
        public static ConfigEntry<float> QuickStackRadius;
        public static ConfigEntry<float> CraftingChestRadius;
        public static ConfigEntry<bool> ShowAvailableAmounts;
        public static ConfigEntry<AmountFormat> AvailableAmountFormat;

        public static void Bind(ConfigFile config)
        {
            // Display preference, not gameplay: left per-player rather than admin-only,
            // the same way hotkeys are.
            ShowAvailableAmounts = config.Bind(
                "Crafting",
                "ShowAvailableAmounts",
                true,
                "Show how much of each ingredient you can actually spend, in brackets after the required amount (e.g. \"10(34)\"). Includes nearby chests wherever crafting from them is allowed.");

            AvailableAmountFormat = config.Bind(
                "Crafting",
                "AvailableAmountFormat",
                AmountFormat.Compact,
                "How that amount is written. Compact is the narrowest and abbreviates past a thousand: \"10(1.1k)\". Exact spells the number out in the same small brackets: \"10(1087)\". Spaced writes it at the game's own text size with a space in front: \"10 (1087)\" — the widest, and the one most likely to be shrunk to fit the slot. Ignored while ShowAvailableAmounts is off.");

            QuickStackRadius = config.Bind(
                "Radii",
                "QuickStackRadius",
                20f,
                new ConfigDescription(
                    "Radius (in meters) in which quick-stack and restock search for nearby chests.",
                    new AcceptableValueRange<float>(0f, 100f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingChestRadius = config.Bind(
                "Radii",
                "CraftingChestRadius",
                20f,
                new ConfigDescription(
                    "Radius (in meters) in which crafting/building considers items from nearby chests.",
                    new AcceptableValueRange<float>(0f, 100f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
    }
}
