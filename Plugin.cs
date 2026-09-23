using BepInEx;
using HarmonyLib;
using Jotunn.Utils;

namespace SmartCraftStorage
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // Optional: if Epic Loot is present, we register as one of its inventory providers
    // (see Integrations/EpicLootProvider.cs). Soft so the mod still loads fine without
    // it; declaring it still orders our Awake() after Epic Loot's when both are present.
    [BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
    // Gameplay settings are bound as admin-only so a server dictates them to its
    // clients. IfOnServer limits that to servers actually running this mod; the
    // default (Always) would also lock and reset them for players joining a server
    // without it, leaving them stuck on defaults.
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.zellds.smartcraftstorage";
        public const string PluginName = "SmartCraft-Storage";
        public const string PluginVersion = "0.9.0";

        internal static Harmony HarmonyInstance;

        private void Awake()
        {
            SmartCraftStorage.Config.ModConfig.Bind(Config);
            SmartCraftStorage.Stations.StationConfig.Bind(Config);
            SmartCraftStorage.AnimalFeeder.AnimalFeederConfig.Bind(Config);
            SmartCraftStorage.Hotkeys.HotkeyConfig.Bind(Config);
            SmartCraftStorage.PlantHarvest.PlantHarvestConfig.Bind(Config);

            SmartCraftStorage.Translations.ModTranslations.Setup();
            SmartCraftStorage.Integrations.EpicLootProvider.Setup();

            HarmonyInstance = new Harmony(PluginGuid);
            HarmonyInstance.PatchAll();
            SmartCraftStorage.Integrations.EpicLootSacrificeProtection.Setup(HarmonyInstance);

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
