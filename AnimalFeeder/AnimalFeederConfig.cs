using BepInEx.Configuration;

namespace SmartCraftStorage.AnimalFeeder
{
    internal static class AnimalFeederConfig
    {
        public static ConfigEntry<float> AnimalFeederRadius;
        public static ConfigEntry<bool> AnimalAutoFeed;

        public static void Bind(ConfigFile config)
        {
            AnimalFeederRadius = config.Bind(
                "Animals",
                "AnimalFeederRadius",
                20f,
                new ConfigDescription(
                    "Radius (in meters) in which hungry tameable animals search nearby chests for food.",
                    new AcceptableValueRange<float>(0f, 100f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            AnimalAutoFeed = config.Bind("Animals", "AnimalAutoFeed", true,
                new ConfigDescription(
                    "Tameable animals (wild being tamed, or already tamed) automatically pull compatible food from nearby chests.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
    }
}
