using System.Collections.Generic;
using UnityEngine;

namespace SmartCraftStorage.Shared
{
    internal static class NearbyContainers
    {
        public static IEnumerable<Container> Find(Vector3 origin, float radius, Player player)
        {
            var result = new List<Container>();
            var hits = Physics.OverlapSphere(origin, radius);
            long playerId = player.GetPlayerID();

            foreach (var hit in hits)
            {
                var container = hit.GetComponentInParent<Container>();
                if (container == null || container.GetInventory() == null)
                {
                    continue;
                }
                if (container.GetComponent<TombStone>() != null)
                {
                    continue;
                }
                if (IsInUseByAnyone(container))
                {
                    continue;
                }
                if (!container.CheckAccess(playerId))
                {
                    continue;
                }
                if (!PrivateArea.CheckAccess(container.transform.position, 0f, false))
                {
                    continue;
                }
                if (!result.Contains(container))
                {
                    result.Add(container);
                }
            }

            // Nearest first, so callers that stop at the first usable chest use the
            // closest one. Squared distance orders identically and skips the sqrt.
            result.Sort((a, b) => (a.transform.position - origin).sqrMagnitude
                .CompareTo((b.transform.position - origin).sqrMagnitude));

            return result;
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

        public static bool TryClaimWriteAccess(Container container)
        {
            return TryClaimWriteAccess(container.m_nview);
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
    }
}
