using System;
using System.Collections.Generic;

public sealed class ItemConversion
{
    public ItemDrop m_from;
    public ItemDrop m_to;
    public int m_producedItems = 1;
}

public abstract class StationBase : UnityEngine.Object
{
    public string name = "test_station";
    public readonly UnityEngine.Transform transform = new UnityEngine.Transform();
    public ZNetView m_nview = new ZNetView();
    public int Progress;
    public abstract void ConfigureInput(string name);
    protected ItemDrop CreateItem(string name)
    {
        var item = new ItemDrop();
        item.gameObject.name = name + "Prefab";
        item.m_itemData.m_shared.m_name = name;
        item.m_itemData.m_dropPrefab = item.gameObject;
        return item;
    }
}

public sealed class CookingStation : StationBase
{
    public readonly List<ItemConversion> m_conversion = new List<ItemConversion>();
    public bool m_useFuel;
    public ItemDrop m_fuelItem;
    public float m_maxFuel;
    public int GetFreeSlot() => Progress == 0 ? 0 : -1;
    public float GetFuel() => Progress;
    public bool IsItemAllowed(string prefabName) => true;
    public ItemDrop.ItemData FindCookableItem(Inventory inventory)
    {
        foreach (var conversion in m_conversion)
        {
            var item = inventory.GetItem(conversion.m_from.m_itemData.m_shared.m_name);
            if (item != null) return item;
        }
        return null;
    }
    public bool HaveDoneItem() => false;
    public void OnInteract(Player player) { }
    public override void ConfigureInput(string name)
    {
        m_conversion.Add(new ItemConversion { m_from = CreateItem(name) });
        m_nview.OnRpc = (_, __) => Progress++;
    }
    public void ConfigureFuel(string name)
    {
        m_useFuel = true;
        m_fuelItem = CreateItem(name);
        m_maxFuel = 1;
        m_nview.OnRpc = (_, __) => Progress++;
    }
}

public sealed class Smelter : StationBase
{
    public readonly List<ItemConversion> m_conversion = new List<ItemConversion>();
    public string m_name;
    public int m_maxOre = 1;
    public float m_maxFuel;
    public ItemDrop m_fuelItem;
    public int GetQueueSize() => Progress;
    public float GetFuel() => Progress;
    public bool IsItemAllowed(string prefabName) => true;
    public ItemDrop.ItemData FindCookableItem(Inventory inventory)
    {
        foreach (var conversion in m_conversion)
        {
            var item = inventory.GetItem(conversion.m_from.m_itemData.m_shared.m_name);
            if (item != null) return item;
        }
        return null;
    }
    public ItemConversion OutputConversion;
    public ItemConversion GetItemConversion(string ore) => OutputConversion;
    // The collect path needs a conversion to produce; kiln detection keys on a
    // conversion producing a prefab named "Coal", which this deliberately is not.
    public void ConfigureOutput(string oreName, string barName)
    {
        OutputConversion = new ItemConversion { m_from = CreateItem(oreName), m_to = CreateItem(barName) };
    }
    public override void ConfigureInput(string name)
    {
        m_conversion.Add(new ItemConversion { m_from = CreateItem(name) });
        m_nview.OnRpc = (_, __) => Progress++;
    }
    public void ConfigureKiln(string name)
    {
        m_name = "charcoal_kiln_test";
        var wood = CreateItem(name);
        wood.gameObject.name = "Wood";
        var coal = CreateItem("coal");
        coal.gameObject.name = "Coal";
        m_conversion.Add(new ItemConversion { m_from = wood, m_to = coal });
        m_nview.OnRpc = (_, __) => Progress++;
    }
    public void ConfigureFuel(string name)
    {
        m_maxOre = 0;
        m_maxFuel = 1;
        m_fuelItem = CreateItem(name);
        m_nview.OnRpc = (_, __) => Progress++;
    }
}

public sealed class Fermenter : StationBase
{
    public enum Status { Empty, Ready }
    public readonly List<ItemConversion> m_conversion = new List<ItemConversion>();
    public float m_fermentationDuration;
    public bool m_hasRoof = true;
    public bool m_exposed;
    public int m_delayedTapItem;
    public bool m_delayedTapItemCheated;
    public readonly EffectList m_spawnEffects = new EffectList();
    public readonly UnityEngine.Transform m_outputPoint = new UnityEngine.Transform();
    public Status GetStatus() => Status.Empty;
    public bool IsItemAllowed(ItemDrop.ItemData item) => true;
    public ItemDrop.ItemData FindCookableItem(Inventory inventory)
    {
        foreach (var conversion in m_conversion)
        {
            var item = inventory.GetItem(conversion.m_from.m_itemData.m_shared.m_name);
            if (item != null) return item;
        }
        return null;
    }
    public ItemConversion OutputConversion;
    public ItemConversion GetItemConversion(int hash) => OutputConversion;
    public override void ConfigureInput(string name)
    {
        m_conversion.Add(new ItemConversion { m_from = CreateItem(name) });
        m_nview.OnRpc = (_, __) => Progress++;
    }
    // What the tap path needs to have something to store.
    public void ConfigureOutput(string name)
    {
        m_delayedTapItem = 1;
        OutputConversion = new ItemConversion { m_from = CreateItem(name + "base"), m_to = CreateItem(name) };
    }
}

public sealed class Fireplace : StationBase
{
    public ItemDrop m_fuelItem;
    public int m_maxFuel = 1;
    public override void ConfigureInput(string name)
    {
        m_fuelItem = CreateItem(name);
        m_nview.OnRpc = (_, __) => { Progress++; m_nview.Zdo.Fuel++; };
    }
}

public sealed class EffectList { public void Create(UnityEngine.Vector3 position, UnityEngine.Quaternion rotation) { } }
public static class PlayerProfile { public static bool s_bypassCheatChecks; }
public static class StringExtensions { public static int GetStableHashCode(this string value) => value.GetHashCode(); }

namespace SmartCraftStorage.Stations
{
    internal enum KilnFeedStrategy { NearestFirst, LeastFuelFirst }
    internal sealed class Setting<T> { public T Value; public Setting(T value) { Value = value; } }
    internal static class StationConfig
    {
        public static readonly Setting<bool> CookingStationAutoRefuel = new Setting<bool>(true);
        public static readonly Setting<bool> CookingStationAutoCollect = new Setting<bool>(false);
        public static readonly Setting<float> CookingStationRadius = new Setting<float>(10);
        public static readonly Setting<bool> SmelterAutoRefuel = new Setting<bool>(true);
        public static readonly Setting<bool> KilnAutoRefuel = new Setting<bool>(true);
        public static readonly Setting<int> KilnWoodBuffer = new Setting<int>(1);
        public static readonly Setting<float> SmelterKilnRadius = new Setting<float>(10);
        public static readonly Setting<bool> KilnRegularWoodOnly = new Setting<bool>(false);
        public static readonly Setting<int> KilnMaxCoalInChest = new Setting<int>(100);
        public static readonly Setting<bool> KilnAutoCollect = new Setting<bool>(false);
        public static readonly Setting<bool> SmelterAutoCollect = new Setting<bool>(false);
        public static readonly Setting<KilnFeedStrategy> KilnFeedStrategyConfig = new Setting<KilnFeedStrategy>(KilnFeedStrategy.NearestFirst);
        public static readonly Setting<float> FermenterDurationOverride = new Setting<float>(0);
        public static readonly Setting<bool> FermenterAutoProcess = new Setting<bool>(true);
        public static readonly Setting<float> FermenterRadius = new Setting<float>(10);
        public static readonly Setting<bool> FireplaceAutoRefuel = new Setting<bool>(true);
        public static readonly Setting<float> FireplaceRadius = new Setting<float>(10);
    }
}
