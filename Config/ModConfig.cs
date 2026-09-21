using BepInEx.Configuration;

namespace SmartCraftStorage.Config
{
    internal static class ModConfig
    {
        public static ConfigEntry<float> QuickStackRadius;
        public static ConfigEntry<float> CraftingChestRadius;
        public static ConfigEntry<bool> ShowAvailableAmounts;

        public static void Bind(ConfigFile config)
        {
            // Display preference, not gameplay: left per-player rather than admin-only,
            // the same way hotkeys are.
            ShowAvailableAmounts = config.Bind(
                "Crafting",
                "ShowAvailableAmounts",
                true,
                "Show how much of each ingredient you can actually spend, in brackets after the required amount (e.g. \"10(34)\"). Includes nearby chests wherever crafting from them is allowed.");

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
