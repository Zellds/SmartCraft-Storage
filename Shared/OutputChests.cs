using System.Collections.Generic;

namespace SmartCraftStorage.Shared
{
    internal enum ChestOutputStrategy
    {
        PreferSorted,
        Nearest
    }

    // Stations deposit what they produce into a nearby chest. Nearest-first is the
    // cheapest answer but not the useful one: a player who keeps an iron chest and a
    // coal chest wants the bars in the iron chest, not in whatever happened to be built
    // closest to the smelter. This reorders the candidates so a chest that already holds
    // the item is tried first, which is enough to keep sorted storage sorted — the chest
    // the player put the first bar in wins every bar after it.
    internal static class OutputChests
    {
        // Two reusable buffers rather than a fresh list per call: every caller sits on a
        // station tick, the same reason NearbyContainers keeps one DistanceComparer
        // instance instead of allocating a lambda per search.
        private static readonly List<Container> Ordered = new List<Container>();
        private static readonly List<Container> Unsorted = new List<Container>();

        // Candidates arrive nearest-first from NearbyContainers.Find. Returns them
        // grouped so chests already holding itemName come first, still nearest-first
        // within each group, and never drops a candidate: a station that finds no sorted
        // chest, or finds them all full, still falls back to the nearest one with room.
        //
        // The returned list is a shared buffer, good only until the next call — the same
        // contract as NearbyContainers.Hits. Finish iterating before calling again. Under
        // Nearest the caller's own list is handed straight back and the buffer is never
        // touched, so that strategy behaves exactly as it did before this existed.
        public static List<Container> OrderForOutput(
            List<Container> candidates, string itemName, ChestOutputStrategy strategy)
        {
            if (candidates == null
                || candidates.Count == 0
                || strategy != ChestOutputStrategy.PreferSorted
                || string.IsNullOrEmpty(itemName))
            {
                return candidates;
            }

            Ordered.Clear();
            Unsorted.Clear();

            // A stable partition, not a sort. The input is already nearest-first, so
            // splitting it in two preserves that order for free. Sorting on a
            // (holds-it, distance) comparer would instead re-read every inventory
            // O(n log n) times, and List.Sort isn't stable, so distance would have to be
            // re-derived as a tie-break as well.
            for (int i = 0; i < candidates.Count; i++)
            {
                var container = candidates[i];
                // Find() prunes destroyed chests as it hands its list out, but a chest
                // can be destroyed while that list is still warm.
                if (container == null)
                {
                    continue;
                }

                if (HoldsItem(container.GetInventory(), itemName))
                {
                    Ordered.Add(container);
                }
                else
                {
                    Unsorted.Add(container);
                }
            }

            for (int i = 0; i < Unsorted.Count; i++)
            {
                Ordered.Add(Unsorted[i]);
            }
            Unsorted.Clear();

            return Ordered;
        }

        // Hand-rolled rather than CountItems/HaveItem: those sum every matching stack
        // instead of stopping at the first hit, and they filter on the current world
        // level, so a chest holding the same item from another world level would read as
        // unsorted and quietly lose the player's sorting. GetAllItems returns the backing
        // list, so walking it allocates nothing — unlike the GetAllItems().FindAll(...)
        // match quick-stack uses, which is fine on a keypress but not once a second per
        // station.
        //
        // Quality is deliberately not compared, unlike that quick-stack match: quick-stack
        // moves a stack the player already owns, which may be an upgraded tool, whereas
        // station output is always spawned fresh from a prefab. Comparing it here could
        // only ever lose the chest the player sorted the item into.
        private static bool HoldsItem(Inventory inventory, string itemName)
        {
            if (inventory == null)
            {
                return false;
            }

            var items = inventory.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item != null && item.m_shared.m_name == itemName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
