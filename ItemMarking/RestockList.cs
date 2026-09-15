using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartCraftStorage.ItemMarking
{
    internal static class RestockList
    {
        private const string CustomDataKey = "SmartCraft_RestockList";
        private const char Separator = '|';

        public static bool Contains(Player player, string itemName)
        {
            return GetAll(player).Contains(itemName);
        }

        public static void Toggle(Player player, string itemName)
        {
            var current = new List<string>(GetAll(player));

            if (current.Contains(itemName))
            {
                current.Remove(itemName);
            }
            else
            {
                current.Add(itemName);
            }

            Save(player, current);
        }

        public static IReadOnlyList<string> GetAll(Player player)
        {
            if (player == null || !player.m_customData.TryGetValue(CustomDataKey, out var raw) || string.IsNullOrEmpty(raw))
            {
                return Array.Empty<string>();
            }

            return raw.Split(Separator);
        }

        // InventoryGrid.UpdateGui asks for this on every frame the inventory is
        // open, and splitting the string and building a set each time was the
        // largest per-frame allocation this mod made. The list only changes when
        // the player marks something, so rebuild only when the stored text does.
        private static string _cachedRaw;
        private static readonly HashSet<string> CachedSet = new HashSet<string>();

        public static HashSet<string> GetSet(Player player)
        {
            string raw = null;
            if (player != null)
            {
                player.m_customData.TryGetValue(CustomDataKey, out raw);
            }

            if (raw == _cachedRaw)
            {
                return CachedSet;
            }

            _cachedRaw = raw;
            CachedSet.Clear();

            if (!string.IsNullOrEmpty(raw))
            {
                foreach (var name in raw.Split(Separator))
                {
                    CachedSet.Add(name);
                }
            }

            return CachedSet;
        }

        private static void Save(Player player, List<string> items)
        {
            if (player == null)
            {
                return;
            }

            player.m_customData[CustomDataKey] = string.Join(Separator.ToString(), items);
        }
    }
}
