using BepInEx.Configuration;

namespace SmartCraftStorage.Stations
{
    internal enum KilnFeedStrategy
    {
        LeastFuelFirst,
        Nearest
    }

    internal static class StationConfig
    {
        public static ConfigEntry<float> FireplaceRadius;
        public static ConfigEntry<float> SmelterKilnRadius;
        public static ConfigEntry<float> CookingStationRadius;

        public static ConfigEntry<bool> FireplaceAutoRefuel;
        public static ConfigEntry<bool> SmelterAutoRefuel;
        public static ConfigEntry<bool> SmelterAutoCollect;
        public static ConfigEntry<bool> KilnAutoRefuel;
        public static ConfigEntry<bool> KilnAutoCollect;
        public static ConfigEntry<bool> CookingStationAutoRefuel;
        public static ConfigEntry<bool> CookingStationAutoCollect;

        public static ConfigEntry<int> KilnWoodBuffer;
        public static ConfigEntry<int> KilnMaxCoalInChest;
        public static ConfigEntry<KilnFeedStrategy> KilnFeedStrategyConfig;

        public static void Bind(ConfigFile config)
        {
            FireplaceRadius = config.Bind(
                "Stations",
                "FireplaceRadius",
                10f,
                new ConfigDescription(
                    "Radius (in meters) in which fireplaces/torches/hearths search nearby chests for fuel.",
                    new AcceptableValueRange<float>(0f, 100f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SmelterKilnRadius = config.Bind(
                "Stations",
                "SmelterKilnRadius",
                10f,
                new ConfigDescription(
                    "Radius (in meters) shared between smelters and charcoal kilns to search nearby chests.",
                    new AcceptableValueRange<float>(0f, 100f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CookingStationRadius = config.Bind(
                "Stations",
                "CookingStationRadius",
                10f,
                new ConfigDescription(
                    "Radius (in meters) in which cooking stations search nearby chests for raw food.",
                    new AcceptableValueRange<float>(0f, 100f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            FireplaceAutoRefuel = config.Bind("Stations", "FireplaceAutoRefuel", true,
                new ConfigDescription("Fireplaces/torches automatically pull fuel from nearby chests.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SmelterAutoRefuel = config.Bind("Stations", "SmelterAutoRefuel", true,
                new ConfigDescription("Smelters automatically pull ore/fuel from nearby chests.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SmelterAutoCollect = config.Bind("Stations", "SmelterAutoCollect", true,
                new ConfigDescription("Smelters store the produced bar in the nearest chest instead of dropping it on the ground.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            KilnAutoRefuel = config.Bind("Stations", "KilnAutoRefuel", true,
                new ConfigDescription("Charcoal kilns automatically pull wood from nearby chests.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            KilnAutoCollect = config.Bind("Stations", "KilnAutoCollect", true,
                new ConfigDescription("Charcoal kilns store the coal they produce (or feed nearby smelters first) instead of dropping it on the ground.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            CookingStationAutoRefuel = config.Bind("Stations", "CookingStationAutoRefuel", true,
                new ConfigDescription("Cooking stations automatically pull raw food (and their own fuel, if applicable) from nearby chests.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            CookingStationAutoCollect = config.Bind("Stations", "CookingStationAutoCollect", true,
                new ConfigDescription("Cooking stations collect finished food on their own and store it in the nearest chest, without needing to interact.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            KilnWoodBuffer = config.Bind(
                "Charcoal Kiln",
                "KilnWoodBuffer",
                3,
                new ConfigDescription(
                    "Wood level the kiln tries to keep in its internal queue (not its max capacity).",
                    new AcceptableValueRange<int>(1, 50),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            KilnMaxCoalInChest = config.Bind(
                "Charcoal Kiln",
                "KilnMaxCoalInChest",
                50,
                new ConfigDescription(
                    "The kiln stops pulling new wood once nearby chest(s) already hold this much coal combined.",
                    new AcceptableValueRange<int>(1, 9999),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            KilnFeedStrategyConfig = config.Bind(
                "Charcoal Kiln",
                "KilnFeedStrategy",
                KilnFeedStrategy.LeastFuelFirst,
                new ConfigDescription("How the kiln picks which nearby smelter to feed first with the coal it produces.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
    }
}
