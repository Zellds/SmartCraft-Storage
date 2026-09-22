using BepInEx.Configuration;
using SmartCraftStorage.Shared;

namespace SmartCraftStorage.Config
{
    internal static class ModConfig
    {
        public static ConfigEntry<float> QuickStackRadius;
        public static ConfigEntry<float> CraftingChestRadius;
        public static ConfigEntry<bool> ShowAvailableAmounts;
        public static ConfigEntry<AmountFormat> AvailableAmountFormat;
        public static ConfigEntry<ChestOutputStrategy> ChestOutputStrategyConfig;
        public static ConfigEntry<bool> DebugLogging;

        public static void Bind(ConfigFile config)
        {
            // Display preference, not gameplay: left per-player rather than admin-only,
            // the same way hotkeys are.
            DebugLogging = config.Bind(
                "Debug",
                "DebugLogging",
                false,
                "Log extra detail about automated decisions (which chest was picked, why one was skipped, etc.) to help diagnose a report. Off by default since it's verbose.");
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

            // Shared by every station that stores what it produces, plus plant harvest,
            // so it lives here rather than in StationConfig.
            ChestOutputStrategyConfig = config.Bind(
                "Output",
                "ChestOutputStrategy",
                ChestOutputStrategy.PreferSorted,
                new ConfigDescription(
                    "Which nearby chest a station's finished product goes into. PreferSorted keeps your sorting: a chest that already holds that item beats a closer one, and the nearest chest is only used when no chest nearby holds it yet (or they're all full). Nearest always takes the closest chest with room, which is how this worked before.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
    }
}
