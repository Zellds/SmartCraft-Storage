// Test doubles for the external Unity/Valheim boundary. The production search
// and count cache are compiled unchanged into this executable. These doubles
// model hierarchy, layers and point overlaps; they do not run Unity physics.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Component
    {
        public static int VagonParentSearches;
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();

        public T GetComponentInParent<T>() where T : Component
        {
            if (typeof(T) == typeof(Vagon)) VagonParentSearches++;
            for (var current = gameObject; current != null; current = current.Parent)
            {
                var component = current.GetComponent<T>();
                if (component != null) return component;
            }
            return null;
        }
    }

    public sealed class GameObject
    {
        private readonly List<Component> _components = new List<Component>();
        public readonly Transform transform = new Transform();
        public GameObject Parent;
        public int layer;

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T { gameObject = this };
            _components.Add(component);
            if (component is Collider collider) Physics.Colliders.Add(collider);
            return component;
        }

        public T GetComponent<T>() where T : Component
        {
            foreach (var component in _components)
                if (component is T match) return match;
            return null;
        }
    }

    public sealed class Transform { public Vector3 position; }
    public sealed class Collider : Component { }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static class Time { public static float time; public static int frameCount; }
    public static class Mathf { public static int Min(int a, int b) => Math.Min(a, b); }
    public static class Debug { public static void LogWarning(string message) => Console.WriteLine(message); }

    public static class LayerMask
    {
        // Read from the installed game's TagManager (Steam build 25364265).
        private static readonly Dictionary<string, int> Layers = new Dictionary<string, int>
        {
            { "Default", 0 }, { "piece", 10 }, { "terrain", 11 }, { "item", 12 },
            { "static_solid", 15 }, { "piece_nonsolid", 16 }, { "Default_small", 20 }, { "vehicle", 28 }
        };

        public static int NameToLayer(string name) => Layers[name];
        public static int GetMask(params string[] names)
        {
            int mask = 0;
            foreach (var name in names) mask |= 1 << NameToLayer(name);
            return mask;
        }
    }

    public static class Physics
    {
        public static readonly List<Collider> Colliders = new List<Collider>();
        public static int Queries;
        public static int LastMask;

        public static int OverlapSphereNonAlloc(Vector3 origin, float radius, Collider[] hits, int mask)
        {
            Queries++;
            LastMask = mask;
            int count = 0;
            foreach (var collider in Colliders)
            {
                if ((mask & (1 << collider.gameObject.layer)) == 0
                    || (collider.transform.position - origin).sqrMagnitude > radius * radius) continue;
                if (count == hits.Length) break;
                hits[count++] = collider;
            }
            return count;
        }
    }
}

public sealed class Player : UnityEngine.Component
{
    public static Player m_localPlayer;
    public long GetPlayerID() => 1;
}

public sealed class Vagon : UnityEngine.Component { public Container m_container; }
public sealed class TombStone : UnityEngine.Component { }

public sealed class Container : UnityEngine.Component
{
    public ZNetView m_nview = new ZNetView();
    public Inventory Inventory = new Inventory();
    public bool InUse;
    public bool Access = true;
    public Inventory GetInventory() => Inventory;
    public bool IsInUse() => InUse;
    public bool CheckAccess(long playerId) => Access;
}

public sealed class Inventory
{
    public bool HaveEmptySlot() => true;
    public int FindFreeStackSpace(string itemName, int worldLevel) => 0;
}

public sealed class ZNetView
{
    public bool Valid = true;
    public bool Owner;
    public readonly ZDO Data = new ZDO();
    public bool IsValid() => Valid;
    public bool IsOwner() => Owner;
    public void ClaimOwnership() => Owner = true;
    public ZDO GetZDO() => Data;
}

public sealed class ZDO { public int InUse; public int GetInt(int key) => InUse; }
public static class ZDOVars { public const int s_inUse = 1; }
public static class Game { public static int m_worldLevel; }
public static class PrivateArea
{
    public static bool Allowed = true;
    public static bool CheckAccess(UnityEngine.Vector3 position, float radius, bool flash) => Allowed;
}
