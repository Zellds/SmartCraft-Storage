using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCraftStorage.ItemMarking
{
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class SlotOverlayPatch
    {
        private static readonly Dictionary<InventoryElement, Image> LockedOverlays = new Dictionary<InventoryElement, Image>();
        private static readonly Dictionary<InventoryElement, Image> RestockOverlays = new Dictionary<InventoryElement, Image>();

        private static Sprite _lockedSprite;
        private static Sprite LockedSprite => _lockedSprite != null ? _lockedSprite : (_lockedSprite = CreateBorderSprite(new Color(1f, 0.35f, 0.1f, 0.9f)));

        private static Sprite _restockSprite;
        private static Sprite RestockSprite => _restockSprite != null ? _restockSprite : (_restockSprite = CreateCornerDotSprite(new Color(0.2f, 0.6f, 1f, 0.95f)));

        private static void Postfix(InventoryGrid __instance)
        {
            try
            {
                if (__instance.m_inventory == null)
                {
                    return;
                }

                int width = __instance.m_inventory.GetWidth();
                var localPlayer = Player.m_localPlayer;
                var restockNames = localPlayer != null ? RestockList.GetSet(localPlayer) : null;

                foreach (var element in __instance.m_elements)
                {
                    if (LockedOverlays.TryGetValue(element, out var lockedOv) && lockedOv != null)
                    {
                        lockedOv.enabled = false;
                    }
                    if (RestockOverlays.TryGetValue(element, out var restockOv) && restockOv != null)
                    {
                        restockOv.enabled = false;
                    }
                }

                foreach (var item in __instance.m_inventory.GetAllItems())
                {
                    var element = __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, width);
                    if (element == null)
                    {
                        continue;
                    }

                    var lockedOverlay = GetOrCreateOverlay(element, LockedOverlays, LockedSprite, "SmartCraft_LockedOverlay");
                    lockedOverlay.enabled = ItemFlags.IsLocked(item);

                    bool restockMarked = restockNames != null && restockNames.Contains(item.m_shared.m_name);
                    var restockOverlay = GetOrCreateOverlay(element, RestockOverlays, RestockSprite, "SmartCraft_RestockOverlay");
                    restockOverlay.enabled = restockMarked;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        internal static Image GetOrCreateOverlay(InventoryElement element, Dictionary<InventoryElement, Image> cache, Sprite sprite, string overlayName)
        {
            if (cache.TryGetValue(element, out var existing) && existing != null)
            {
                return existing;
            }

            var go = new GameObject(overlayName);
            go.transform.SetParent(element.m_icon.transform, worldPositionStays: false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.type = Image.Type.Sliced;

            cache[element] = image;
            return image;
        }

        internal static Sprite CreateBorderSprite(Color color, int size = 64, int thickness = 5)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onBorder = x < thickness || x >= size - thickness || y < thickness || y >= size - thickness;
                    pixels[y * size + x] = onBorder ? color : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(thickness, thickness, thickness, thickness));
        }

        internal static Sprite CreateCornerDotSprite(Color color, int size = 64, int dotRadius = 10)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = new Vector2(size - dotRadius - 4, dotRadius + 4);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = distance <= dotRadius ? color : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
