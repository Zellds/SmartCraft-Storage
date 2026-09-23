using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : class => original;
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => base.GetHashCode();
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 up => new Vector3(0, 1, 0);
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x * b, a.y * b, a.z * b);
    }

    public struct Quaternion { public static Quaternion identity => new Quaternion(); }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } public static Vector2 operator *(Vector2 a, float b) => new Vector2(a.x * b, a.y * b); }

    public class Transform { public Vector3 position; public Transform transform => this; }
    public class Collider
    {
        public Container Container;
        public GameObject gameObject = new GameObject();
        public T GetComponentInParent<T>() where T : class => Container as T;
    }
    public static class Physics
    {
        public static int OverlapSphereNonAlloc(Vector3 origin, float radius, Collider[] hits, int mask)
        {
            int count = Math.Min(hits.Length, TestWorld.Colliders.Count);
            for (int i = 0; i < count; i++) hits[i] = TestWorld.Colliders[i];
            return count;
        }
    }
    public static class LayerMask
    {
        public static int GetMask(params string[] names) => 1;
        // No fixture here creates a vehicle-layer collider, so any distinct,
        // stable value keeps production's cart-fallback branch compiling and
        // correctly unreachable for these chest-only scenarios.
        public static int NameToLayer(string name) => -2;
    }
    public static class Mathf { public static int Min(int a, int b) => Math.Min(a, b); public static int CeilToInt(float value) => (int)Math.Ceiling(value); }
    public static class Time { public static float time; public static int frameCount; }
    public static class Debug { public static void Log(object message) { } public static void LogWarning(object message) { } public static void LogException(Exception error) { throw error; } }
    public static class Random { public static Vector2 insideUnitCircle => new Vector2(); }
    public enum KeyCode { None, LeftAlt, LeftControl }
    public sealed class GameObject
    {
        public string name;
        public int layer;
        public readonly Transform transform = new Transform();
        public ItemDrop Item;
        public T GetComponent<T>() where T : class => Item as T;
    }
}

// No fixture here creates a vehicle-layer collider without a Container, so this
// stub only needs to satisfy production's type reference; it is never populated
// or reached by GetComponentInParent<Vagon>() in these chest-only scenarios.
public sealed class Vagon
{
    public Container m_container;
}

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type, string methodName) { }
        public HarmonyPatch(Type type, string methodName, Type[] argumentTypes) { }
    }
}

namespace SmartCraftStorage.Config
{
    internal static class ModConfig
    {
        public static readonly Setting<float> CraftingChestRadius = new Setting<float>(10f);
        public static readonly Setting<bool> DebugLogging = new Setting<bool>(false);
        // The enum is source-linked from Shared/, so there is no stub copy to drift.
        public static readonly Setting<SmartCraftStorage.Shared.ChestOutputStrategy> ChestOutputStrategyConfig
            = new Setting<SmartCraftStorage.Shared.ChestOutputStrategy>(SmartCraftStorage.Shared.ChestOutputStrategy.PreferSorted);
    }
    internal sealed class Setting<T> { public T Value; public Setting(T value) { Value = value; } }
}

public sealed class ZDO
{
    public int InUse;
    public float Fuel;
    public bool Cheated;
    public int GetInt(int key) => InUse;
    public float GetFloat(int key) => Fuel;
    public bool GetBool(int key) => Cheated;
}

public sealed class ZNetView : UnityEngine.Object
{
    public bool Valid = true;
    public bool Owner = true;
    public ZDO Zdo = new ZDO();
    public Action<string, object[]> OnRpc;
    public bool IsValid() => Valid;
    public bool IsOwner() => Owner;
    public int ClaimCount;
    public void ClaimOwnership() { ClaimCount++; Owner = true; }
    public ZDO GetZDO() => Zdo;
    public void InvokeRPC(string name, params object[] args) => OnRpc?.Invoke(name, args);
}

public static class ZDOVars { public static int s_inUse; public static int s_fuel; public static int s_cheated; }
public sealed class TombStone { }

public sealed class Inventory
{
    private readonly List<ItemDrop.ItemData> _stacks = new List<ItemDrop.ItemData>();
    public Container Owner;
    public bool FailRemovals;
    // Settable so a "sorted but full" chest is expressible. The defaults match what
    // this double returned before output ordering existed.
    public bool EmptySlot = true;
    public int FreeStackSpace;
    public bool HaveEmptySlot() => EmptySlot;
    public int FindFreeStackSpace(string name, int worldLevel) => FreeStackSpace;
    public int CountItems(string name, int quality = -1, bool matchWorldLevel = true)
    {
        int total = 0;
        foreach (var item in _stacks)
            if ((name == null || item.m_shared.m_name == name) && (quality < 0 || item.m_quality == quality)
                && (!matchWorldLevel || item.m_worldLevel >= Game.m_worldLevel)) total += item.m_stack;
        return total;
    }
    public List<ItemDrop.ItemData> GetAllItems() => _stacks;
    public ItemDrop.ItemData GetItemAt(int x, int y) => _stacks.Count > 0 ? _stacks[0] : null;
    public ItemDrop.ItemData GetItem(string name)
    {
        foreach (var item in _stacks)
            if (item.m_shared.m_name == name && item.m_worldLevel >= Game.m_worldLevel) return item;
        return null;
    }
    public ItemDrop.ItemData AddStack(string name, int amount, int quality = 1, int worldLevel = 1)
    {
        var item = new ItemDrop.ItemData { m_stack = amount, m_quality = quality, m_worldLevel = (byte)worldLevel };
        item.m_shared.m_name = name;
        item.m_dropPrefab = new UnityEngine.GameObject { name = name + "Prefab" };
        _stacks.Add(item);
        return item;
    }
    public bool ContainsItem(ItemDrop.ItemData item) => _stacks.Contains(item);
    public bool RemoveItem(ItemDrop.ItemData item, int amount)
    {
        if (FailRemovals || !_stacks.Contains(item)) return false;
        int take = Math.Min(amount, item.m_stack);
        item.m_stack -= take;
        if (item.m_stack <= 0) _stacks.Remove(item);
        return true;
    }
    public void RemoveItem(string name, int amount, int quality = -1, bool matchWorldLevel = true)
    {
        foreach (var item in new List<ItemDrop.ItemData>(_stacks))
        {
            if (amount <= 0) break;
            if (item.m_shared.m_name != name || (quality >= 0 && item.m_quality != quality)
                || (matchWorldLevel && item.m_worldLevel < Game.m_worldLevel)) continue;
            int take = Math.Min(item.m_stack, amount);
            RemoveItem(item, take);
            amount -= take;
        }
    }
    public bool HaveItem(string name, bool matchWorldLevel = true) => CountItems(name, -1, matchWorldLevel) > 0;
    public void Changed() { Owner?.Save(); }
    public bool AddItem(UnityEngine.GameObject prefab, int amount)
    {
        var name = prefab.Item.m_itemData.m_shared.m_name;
        if (amount <= 0) return false;
        ItemDrop.ItemData existing = null;
        foreach (var item in _stacks) if (item.m_shared.m_name == name) { existing = item; break; }
        if (existing == null) AddStack(name, amount); else existing.m_stack += amount;
        Changed();
        return true;
    }
}

public sealed class ItemDrop
{
    public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
    public ItemData m_itemData = new ItemData();
    public ItemDrop() { gameObject.Item = this; }
    public static void OnCreateNew(ItemDrop item, bool cheated = false) { }
    public T GetComponent<T>() where T : class => this as T;
    public void SetStack(int amount) => m_itemData.m_stack = amount;
    public sealed class ItemData
    {
        public int m_stack;
        public int m_quality = 1;
        public byte m_worldLevel = 1;
        public SharedData m_shared = new SharedData();
        public Dictionary<string, string> m_customData = new Dictionary<string, string>();
        public UnityEngine.GameObject m_dropPrefab;
        public bool m_cheated;
    }
    public sealed class SharedData { public string m_name; }
}

public sealed class Container : UnityEngine.Object
{
    public readonly UnityEngine.Transform transform = new UnityEngine.Transform();
    public readonly Inventory Inventory;
    public ZNetView m_nview = new ZNetView();
    public bool SavedLocked;
    public Container() { Inventory = new Inventory { Owner = this }; }
    public Inventory GetInventory() => Inventory;
    public T GetComponent<T>() where T : class => null;
    public bool IsInUse() => false;
    public bool CheckAccess(long playerId) => true;
    public void Save()
    {
        var items = Inventory.GetAllItems();
        if (items.Count > 0) SavedLocked = SmartCraftStorage.ItemMarking.ItemFlags.IsLocked(items[0]);
    }
    public void ReloadLock(ItemDrop.ItemData item) => SmartCraftStorage.ItemMarking.ItemFlags.SetLocked(item, SavedLocked);
}

public sealed class Player : UnityEngine.Object
{
    public static Player m_localPlayer;
    public readonly UnityEngine.Transform transform = new UnityEngine.Transform();
    private readonly Inventory _inventory = new Inventory();
    public bool Crafting;
    public long GetPlayerID() => 1;
    public Inventory GetInventory() => _inventory;
    public object GetCurrentCraftingStation() => Crafting ? new object() : null;
    public bool InPlaceMode() => false;
}

public static class PrivateArea { public static bool CheckAccess(UnityEngine.Vector3 position, float radius, bool flash) => true; }
public sealed class Game
{
    public static int m_worldLevel = 1;
    public static readonly Game instance = new Game();
    public int ScaleDrops(ItemDrop.ItemData item, int amount) => amount;
}
public sealed class ObjectDB
{
    public static readonly ObjectDB instance = new ObjectDB();
    public UnityEngine.GameObject GetItemPrefab(string name) => null;
}

internal static class TestWorld
{
    public static readonly List<UnityEngine.Collider> Colliders = new List<UnityEngine.Collider>();
    public static void Reset()
    {
        Colliders.Clear();
        Player.m_localPlayer = new Player();
        UnityEngine.Time.time += 1f;
        UnityEngine.Time.frameCount++;
    }
    public static void ClearChests() { Colliders.Clear(); UnityEngine.Time.time += 1f; }
    public static Container CreateChest()
    {
        var chest = new Container();
        Colliders.Add(new UnityEngine.Collider { Container = chest });
        return chest;
    }
}
