using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartCraftStorage.Shared
{
    // Caches the *chest* half of an item count for the rest of the current frame.
    //
    // The game asks for the same count more than once per frame, by design:
    // InventoryGui.SetupRequirement counts an ingredient to colour the label, and
    // this mod then counts it again to print the amount in brackets; and
    // Player.HaveRequirementItems counts every ingredient once per quality level,
    // for every recipe, each time UpdateRecipeList refreshes the crafting panel —
    // which is one frame in which recipes sharing Wood or Stone ask the same
    // question over and over.
    //
    // Only the chest contribution is cached. The player's own inventory is counted
    // live by the game on every call, so what you carry is always exact. The chest
    // total is discarded when the frame ends, when the set of nearby chests is
    // re-swept, and whenever this mod writes to a container — the only ways it can
    // change between two calls inside one frame.
    internal static class ChestCountCache
    {
        private static readonly Dictionary<Query, int> Totals = new Dictionary<Query, int>(QueryComparer.Instance);
        private static int _frame = -1;

        public static bool TryGet(string name, int quality, bool matchWorldLevel, out int chestTotal)
        {
            ExpireIfFrameChanged();
            return Totals.TryGetValue(new Query(name, quality, matchWorldLevel), out chestTotal);
        }

        public static void Store(string name, int quality, bool matchWorldLevel, int chestTotal)
        {
            ExpireIfFrameChanged();
            Totals[new Query(name, quality, matchWorldLevel)] = chestTotal;
        }

        // Called wherever container contents or the container set can change.
        // Conservative on purpose: a needless clear costs one recount, a missed one
        // shows the player a number that is quietly wrong.
        public static void Invalidate()
        {
            if (Totals.Count > 0)
            {
                Totals.Clear();
            }
        }

        private static void ExpireIfFrameChanged()
        {
            int frame = Time.frameCount;
            if (frame != _frame)
            {
                _frame = frame;
                Invalidate();
            }
        }

        private readonly struct Query : IEquatable<Query>
        {
            private readonly string _name;
            private readonly int _quality;
            private readonly bool _matchWorldLevel;

            public Query(string name, int quality, bool matchWorldLevel)
            {
                _name = name;
                _quality = quality;
                _matchWorldLevel = matchWorldLevel;
            }

            public bool Equals(Query other)
            {
                return _quality == other._quality
                    && _matchWorldLevel == other._matchWorldLevel
                    && string.Equals(_name, other._name, StringComparison.Ordinal);
            }

            public override bool Equals(object obj) => obj is Query other && Equals(other);

            public override int GetHashCode()
            {
                int hash = _name != null ? _name.GetHashCode() : 0;
                hash = (hash * 397) ^ _quality;
                return (hash * 397) ^ (_matchWorldLevel ? 1 : 0);
            }
        }

        // An explicit comparer so the dictionary never falls back to the boxing
        // ObjectEqualityComparer path for the struct key.
        private sealed class QueryComparer : IEqualityComparer<Query>
        {
            public static readonly QueryComparer Instance = new QueryComparer();

            public bool Equals(Query a, Query b) => a.Equals(b);

            public int GetHashCode(Query query) => query.GetHashCode();
        }
    }
}
