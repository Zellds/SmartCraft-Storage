using BepInEx.Configuration;

namespace SmartCraftStorage.PlantHarvest
{
    internal static class PlantHarvestConfig
    {
        public static ConfigEntry<float> PlantHarvestRadius;
        public static ConfigEntry<bool> PlantAutoHarvest;

        public static void Bind(ConfigFile config)
        {
            PlantHarvestRadius = config.Bind(
                "Plant Harvest (In Test)",
                "PlantHarvestRadius",
                10f,
                new ConfigDescription(
                    "Radius (in meters), around the player, in which ripe crops are harvested automatically and stored in a nearby chest.",
                    new AcceptableValueRange<float>(0f, 25f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PlantAutoHarvest = config.Bind(
                "Plant Harvest (In Test)",
                "PlantAutoHarvest",
                false,
                new ConfigDescription(
                    "Experimental: automatically harvests ripe cultivated crops near the player and stores them in a nearby chest (see ChestOutputStrategy). Off by default, test it before relying on it.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
    }
}
