using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SmartCraftStorage.Shared
{
    internal static class NearbyContainers
    {
        // Find() sits on Inventory.CountItems/HaveItem, which the crafting and building
        // UI call every frame — once per requirement of the selected recipe, and once
        // per recipe in the whole list every time the crafting panel refreshes. A full
        // sphere query per call is far too expensive, so the result is cached for a
        // fraction of a second: chests are not placed, destroyed or opened that fast,
        // and the inventories themselves are always read live from the cached list.
        private const float CacheSeconds = 0.25f;

        // Re-sweep if the origin moved more than 0.5m, so the set stays honest as the
        // player walks along a wall of chests.
        private const float CacheMoveToleranceSqr = 0.25f;

        // One slot per distinct origin/radius in play: the player plus a few stations
        // updating on their own timers, which would otherwise evict each other.
        private const int CacheSlots = 8;

        // Chests sit on "piece"; physical vehicle colliders on "vehicle". The rest are
        // included as cheap insurance for containers on other layers — what matters is
        // that terrain, characters, dropped items, hitboxes and trigger volumes never
        // reach GetComponentInParent.
        private static int _queryMask;
        private static bool _queryMaskResolved;
        private static int _vehicleLayer = -1;

        private static int QueryMask
        {
            get
            {
                if (_queryMaskResolved)
                {
                    return _queryMask;
                }

                _queryMask = LayerMask.GetMask("piece", "piece_nonsolid", "vehicle", "Default", "static_solid", "Default_small");
                _vehicleLayer = LayerMask.NameToLayer("vehicle");
                _queryMaskResolved = true;

                if (_queryMask == 0)
                {
                    // A game update renamed the layers. Falling back to every layer is
                    // slow but correct; silently finding no chests at all is not.
                    Debug.LogWarning("[SmartCraft-Storage] None of the expected collision layers exist; "
                        + "falling back to querying all layers. Nearby-chest lookups will be slower than usual.");
                    _queryMask = ~0;
                }

                return _queryMask;
            }
        }

        // Sized from what a built-up base actually returns - a 20m sweep there
        // comes back with roughly 400 colliders. Starting under that just makes
        // the first sweep run the query twice before it grows.
        private static Collider[] _hits = new Collider[512];
        private const int MaxHits = 8192;

        private static readonly CacheEntry[] Cache = CreateCache();
        private static readonly HashSet<Container> Seen = new HashSet<Container>();
        private static readonly DistanceComparer Comparer = new DistanceComparer();

        public static List<Container> Find(Vector3 origin, float radius, Player player)
        {
            var entry = GetCached(origin, radius);
            if (entry != null)
            {
                PruneDestroyed(entry.Containers);
                return entry.Containers;
            }

            // A different set of chests means any cached chest total is about a set
            // that no longer applies.
            ChestCountCache.Invalidate();

            entry = TakeOldestSlot();
            entry.Origin = origin;
            entry.Radius = radius;
            entry.Time = Time.time;

            var result = entry.Containers;
            result.Clear();
            Seen.Clear();

            long playerId = player.GetPlayerID();
            int hitCount = OverlapNearby(origin, radius);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hits[i];
                var container = hit.GetComponentInParent<Container>();
                if (container == null && hit.gameObject.layer == _vehicleLayer)
                {
                    // A cart's hold is on the excluded "item" layer, beside its
                    // vehicle colliders. The Vagon owns the reference to that hold.
                    // Avoid another ancestry walk for every wall, rock and tree.
                    var vagon = hit.GetComponentInParent<Vagon>();
                    if (vagon != null)
                    {
                        container = vagon.m_container;
                    }
                }
                if (container == null || !Seen.Add(container))
                {
                    continue;
                }
                if (!IsUsableBy(container, playerId))
                {
                    continue;
                }

                result.Add(container);
            }

            // Nearest first, so callers that stop at the first usable chest use the
            // closest one. Squared distance orders identically and skips the sqrt.
            Comparer.Origin = origin;
            result.Sort(Comparer);

            return result;
        }

        // Colliders from the last OverlapNearby call. Read it only after that call —
        // the buffer is replaced when it has to grow — and only until the next one.
        public static Collider[] Hits => _hits;

        // Shared sphere query: masked, and into a buffer that grows instead of
        // silently dropping colliders past its end. Returns how many of Hits are
        // valid; the buffer is only good until the next call.
        public static int OverlapNearby(Vector3 origin, float radius)
        {
            while (true)
            {
                int count = Physics.OverlapSphereNonAlloc(origin, radius, _hits, QueryMask);
                if (count < _hits.Length || _hits.Length >= MaxHits)
                {
                    return count;
                }

                // Buffer was filled exactly: assume it overflowed and retry bigger.
                _hits = new Collider[Mathf.Min(_hits.Length * 2, MaxHits)];
            }
        }

        // Inventory.AddItem logs "Trying to add item to occupied slot -1, -1" as an
        // error when the container has no room, instead of just returning false, so
        // ask first. This mirrors AddItem's own rule exactly rather than going
        // through CanAddItem: AddItem stacks only onto stacks at the *current* world
        // level (FindFreeStackItem compares m_worldLevel), and otherwise needs a free
        // slot, whereas CanAddItem measures stack space at the prefab's world level
        // and would still let the error through on a slot-full container.
        public static bool HasRoomFor(Inventory inventory, string itemName)
        {
            if (inventory == null)
            {
                return false;
            }

            return inventory.HaveEmptySlot() || inventory.FindFreeStackSpace(itemName, Game.m_worldLevel) > 0;
        }

        // Container normally checks its ZDO at most once per second. A station may
        // run in the gap, see an old local inventory and then become owner, which
        // can serialize that stale state over the chest's real contents. Refresh
        // while another peer still owns the ZDO, then claim ownership and hand the
        // caller the refreshed inventory.
        private static readonly System.Reflection.MethodInfo CheckForChangesMethod =
            AccessTools.Method(typeof(Container), "CheckForChanges");

        public static bool TryGetFreshWriteInventory(Container container, out Inventory inventory)
        {
            inventory = null;

            var player = Player.m_localPlayer;
            if (container == null || player == null
                || !IsUsableBy(container, player.GetPlayerID()))
            {
                return false;
            }

            var nview = container.m_nview;
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            // CheckForChanges loads the current ZDO only for a non-owner. Calling it
            // before ClaimOwnership is therefore essential: after claiming, the
            // container can save its old local inventory instead of loading it.
            if (!nview.IsOwner())
            {
                if (CheckForChangesMethod == null)
                {
                    Debug.LogWarning("[SmartCraftStorage] Cannot refresh a remote container before writing; "
                        + "skipping the write to avoid overwriting its inventory.");
                    return false;
                }

                try
                {
                    CheckForChangesMethod.Invoke(container, null);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[SmartCraftStorage] Could not refresh a remote container before writing; "
                        + "skipping the write to avoid overwriting its inventory. " + ex.Message);
                    return false;
                }
            }

            // Refreshing can reveal that somebody opened the chest or that access
            // changed after Find() ran, so validate and claim only after the reload.
            if (!TryClaimWriteAccess(container))
            {
                return false;
            }

            inventory = container.GetInventory();
            return inventory != null;
        }

        public static bool TryClaimWriteAccess(ZNetView nview)
        {
            if (nview == null || !nview.IsValid())
            {
                return false;
            }
            if (nview.IsOwner())
            {
                return true;
            }
            nview.ClaimOwnership();
            return nview.IsOwner();
        }

        // Every write this mod makes to a chest goes through here, so this is where
        // the multiplayer race is closed. ClaimOwnership() is not a lock — it always
        // succeeds — so the only thing keeping us out of a chest somebody else has
        // open is the in-use check, and by now that check may be stale: the container
        // list is cached for a fraction of a second, and the game itself only reloads
        // a container's ZDO once a second (Container.CheckForChanges). Another player
        // can open a chest, or a ward's permissions can change, between the search
        // and this write, so re-test rather than trusting what was true at search
        // time. Callers already skip to the next chest when this returns false.
        public static bool TryClaimWriteAccess(Container container)
        {
            var player = Player.m_localPlayer;
            if (container == null || player == null)
            {
                return false;
            }
            if (!IsUsableBy(container, player.GetPlayerID()))
            {
                return false;
            }

            if (!TryClaimWriteAccess(container.m_nview))
            {
                return false;
            }

            // Every container write in this mod passes through here, so this is the
            // one place that reliably catches "the chests just changed". Claiming
            // access does not guarantee a write follows, but clearing needlessly
            // only costs a recount, while missing one would show a wrong number.
            ChestCountCache.Invalidate();
            return true;
        }

        // The rules for "this chest is fair game", applied both when searching and
        // again immediately before writing, so the two can never drift apart.
        private static bool IsUsableBy(Container container, long playerId)
        {
            if (container == null || container.GetInventory() == null)
            {
                return false;
            }
            if (container.GetComponent<TombStone>() != null)
            {
                return false;
            }
            if (IsInUseByAnyone(container))
            {
                return false;
            }
            if (!container.CheckAccess(playerId))
            {
                return false;
            }
            return PrivateArea.CheckAccess(container.transform.position, 0f, false);
        }

        private static bool IsInUseByAnyone(Container container)
        {
            if (container.IsInUse())
            {
                return true;
            }
            return container.m_nview != null && container.m_nview.IsValid()
                && container.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
        }

        private static CacheEntry GetCached(Vector3 origin, float radius)
        {
            float now = Time.time;

            foreach (var entry in Cache)
            {
                if (entry.Radius == radius
                    && now - entry.Time <= CacheSeconds
                    && (entry.Origin - origin).sqrMagnitude <= CacheMoveToleranceSqr)
                {
                    return entry;
                }
            }

            return null;
        }

        private static CacheEntry TakeOldestSlot()
        {
            var oldest = Cache[0];
            foreach (var entry in Cache)
            {
                if (entry.Time < oldest.Time)
                {
                    oldest = entry;
                }
            }
            return oldest;
        }

        // A chest can be destroyed while its slot is still warm.
        private static void PruneDestroyed(List<Container> containers)
        {
            for (int i = containers.Count - 1; i >= 0; i--)
            {
                if (containers[i] == null)
                {
                    containers.RemoveAt(i);
                }
            }
        }

        private static CacheEntry[] CreateCache()
        {
            var cache = new CacheEntry[CacheSlots];
            for (int i = 0; i < cache.Length; i++)
            {
                cache[i] = new CacheEntry();
            }
            return cache;
        }

        private sealed class CacheEntry
        {
            public Vector3 Origin;
            public float Radius = -1f;
            public float Time = float.NegativeInfinity;
            public readonly List<Container> Containers = new List<Container>();
        }

        // A field on a reused instance instead of a lambda closing over the origin,
        // which would allocate on every call.
        private sealed class DistanceComparer : IComparer<Container>
        {
            public Vector3 Origin;

            public int Compare(Container a, Container b)
            {
                return (a.transform.position - Origin).sqrMagnitude
                    .CompareTo((b.transform.position - Origin).sqrMagnitude);
            }
        }
    }
}
