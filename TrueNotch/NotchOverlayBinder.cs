using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.TrueNotch
{
    internal static class NotchOverlayBinder
    {
        private const float FallbackPrefabWidth = 48f;
        private static readonly Dictionary<int, float> PrefabWidths = new Dictionary<int, float>(4);

        internal static bool TryBind(Image? notchBox, out RectTransform rect, out float prefabWidth)
        {
            rect = null!;
            prefabWidth = FallbackPrefabWidth;
            if (notchBox == null)
                return false;

            rect = notchBox.rectTransform;
            if (rect == null)
                return false;

            prefabWidth = ResolvePrefabWidth(rect);
            return true;
        }

        internal static bool TryGetPrefabWidth(RectTransform rect, out float prefabWidth)
        {
            prefabWidth = FallbackPrefabWidth;
            if (rect == null)
                return false;

            prefabWidth = ResolvePrefabWidth(rect);
            return true;
        }

        internal static void Clear()
        {
            PrefabWidths.Clear();
        }

        private static float ResolvePrefabWidth(RectTransform rect)
        {
            int id = rect.GetInstanceID();
            if (PrefabWidths.TryGetValue(id, out float cached))
                return cached;

            float width = rect.sizeDelta.x;
            if (width <= 0f)
                width = rect.rect.width;
            if (width <= 0f)
                width = FallbackPrefabWidth;

            PrefabWidths[id] = width;
            return width;
        }
    }
}
