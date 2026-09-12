using BepInEx.Configuration;

namespace SmartCraftStorage.Config
{
    internal static class ModConfig
    {
        public static ConfigEntry<float> QuickStackRadius;
        public static ConfigEntry<float> CraftingChestRadius;

        public static void Bind(ConfigFile config)
        {
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
