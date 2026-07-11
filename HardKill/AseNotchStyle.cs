using System.Collections.Generic;
using NOCS.TrueNotch;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.HardKill
{
    internal static class AseNotchStyle
    {
        private const float FallbackStrokePx = 2f;
        private const float MaxStrokePx = 4f;
        private const float StrokeScale = 2f;
        private const float LockPulseHz = 20f;
        private const float SearchPulseHz = 10f;

        private static readonly Dictionary<int, float> SpriteBorderAlphaCache = new Dictionary<int, float>(4);
        private static float _cachedStrokePx = -1f;

        internal static Color ResolveColor(Missile? threat)
        {
            if (TryGetNotchReference(out Image? notchBox) && notchBox != null)
            {
                Color color = notchBox.color;
                color.a *= ResolveInheritedCanvasGroupAlpha(notchBox.transform);
                return color;
            }

            return ResolveSeekerColor(threat);
        }

        internal static Color ResolveLabelColor(in Color ringColor)
        {
            Color color = ringColor;
            color.a *= ResolveRingPixelAlpha() / 255f;
            return color;
        }

        internal static Color ResolveLabelColorFromImage(Image notchBox)
        {
            Color color = notchBox.color;
            color.a *= ResolveInheritedCanvasGroupAlpha(notchBox.transform);
            color.a *= ResolveNotchBorderAlpha(notchBox);
            return color;
        }

        internal static byte ResolveRingPixelAlpha()
        {
            if (!TryGetNotchReference(out Image? notchBox) || notchBox == null)
                return 255;

            return (byte)Mathf.Clamp(
                Mathf.RoundToInt(ResolveNotchBorderAlpha(notchBox) * 255f),
                1,
                255);
        }

        internal static float ResolveStrokePx()
        {
            if (_cachedStrokePx > 0f)
                return _cachedStrokePx;

            float stroke = FallbackStrokePx;
            if (NotchTelemetryBridge.TryPeek(out _, out Image? notchBox) && notchBox != null)
                stroke = MeasureNotchStrokePx(notchBox);

            _cachedStrokePx = Mathf.Clamp(stroke, FallbackStrokePx, MaxStrokePx) * StrokeScale;
            return _cachedStrokePx;
        }

        internal static void ApplyImageStyle(Image target, Image? notchReference)
        {
            if (notchReference == null)
                return;

            target.pixelsPerUnitMultiplier = notchReference.pixelsPerUnitMultiplier;
            if (notchReference.material != null)
                target.material = notchReference.material;
        }

        internal static bool TryGetNotchReference(out Image? notchBox)
        {
            if (NotchTelemetryBridge.TryPeek(out _, out notchBox) && notchBox != null)
                return true;

            notchBox = null;
            return false;
        }

        private static Color ResolveSeekerColor(Missile? threat)
        {
            if (threat == null)
                return Color.green;

            float t = Time.timeSinceLevelLoad;
            if (threat.seekerMode == Missile.SeekerMode.activeLock)
                return Color.Lerp(Color.yellow, Color.red, Mathf.Sin(t * LockPulseHz) + 0.5f);

            if (threat.seekerMode == Missile.SeekerMode.activeSearch)
                return Color.Lerp(Color.green, Color.yellow, Mathf.Sin(t * SearchPulseHz) + 0.5f);

            return Color.green;
        }

        private static float ResolveInheritedCanvasGroupAlpha(Transform node)
        {
            float alpha = 1f;
            Transform? current = node;
            while (current != null)
            {
                CanvasGroup? group = current.GetComponent<CanvasGroup>();
                if (group != null)
                    alpha *= group.alpha;
                current = current.parent;
            }

            return Mathf.Clamp01(alpha);
        }

        private static float ResolveNotchBorderAlpha(Image notchBox)
        {
            Sprite? sprite = notchBox.sprite;
            if (sprite == null || sprite.texture == null)
                return 1f;

            int spriteId = sprite.GetInstanceID();
            if (SpriteBorderAlphaCache.TryGetValue(spriteId, out float cached))
                return cached;

            float alpha = SampleSpriteBorderAlpha(sprite);
            SpriteBorderAlphaCache[spriteId] = alpha;
            return alpha;
        }

        private static float SampleSpriteBorderAlpha(Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            if (tex == null || !tex.isReadable)
                return 1f;

            Rect rect = sprite.rect;
            if (rect.width < 4f || rect.height < 4f)
                return 1f;

            int xMid = (int)(rect.x + rect.width * 0.5f);
            int yTop = (int)(rect.y + rect.height - 2f);
            int yBottom = (int)(rect.y + 1f);
            int xLeft = (int)(rect.x + 1f);
            int xRight = (int)(rect.x + rect.width - 2f);

            float sum = 0f;
            int count = 0;
            TrySampleAlpha(tex, xMid, yTop, ref sum, ref count);
            TrySampleAlpha(tex, xMid, yBottom, ref sum, ref count);
            TrySampleAlpha(tex, xLeft, (int)(rect.y + rect.height * 0.5f), ref sum, ref count);
            TrySampleAlpha(tex, xRight, (int)(rect.y + rect.height * 0.5f), ref sum, ref count);

            if (count <= 0)
                return 1f;

            return Mathf.Clamp01(sum / count);
        }

        private static void TrySampleAlpha(Texture2D tex, int x, int y, ref float sum, ref int count)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height)
                return;

            sum += tex.GetPixel(x, y).a;
            count++;
        }

        private static float MeasureNotchStrokePx(Image notchBox)
        {
            Sprite? sprite = notchBox.sprite;
            if (sprite != null)
            {
                if (notchBox.type == Image.Type.Sliced || notchBox.type == Image.Type.Tiled)
                {
                    Vector4 border = sprite.border;
                    float borderPx = Mathf.Max(
                        Mathf.Max(border.x, border.y),
                        Mathf.Max(border.z, border.w));
                    if (borderPx >= 0.5f)
                    {
                        float ppu = sprite.pixelsPerUnit * notchBox.pixelsPerUnitMultiplier;
                        if (ppu <= 0.001f)
                            ppu = 100f;

                        float canvasScale = notchBox.canvas != null ? notchBox.canvas.scaleFactor : 1f;
                        return borderPx / ppu * canvasScale;
                    }
                }
            }

            return FallbackStrokePx;
        }
    }
}
