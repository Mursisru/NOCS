using System.Collections.Generic;
using UnityEngine;

namespace NOCS.HardKill
{
    internal static class AseCircleSprite
    {
        private const float PixelsPerUnit = 100f;
        private const int DiameterBucket = 8;
        private const int MaxCached = 24;
        private const int MinTextureSize = 256;
        private const int MaxTextureSize = 2048;
        private const int TextureSizeBucket = 64;
        private const float MaxRadiusScreenFrac = 0.28f;
        private const int MinRingTexPx = 3;

        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>(MaxCached);
        private static readonly List<int> CacheOrder = new List<int>(MaxCached);
        private static Sprite? _pinned;

        private static int _screenWidth = -1;
        private static int _screenHeight = -1;
        private static int _textureSize = MinTextureSize;
        private static int _resolutionStamp;

        internal static int ResolutionStamp => _resolutionStamp;

        internal static int SyncResolutionStamp()
        {
            EnsureGameResolution();
            return _resolutionStamp;
        }

        internal static Sprite Get(float diameterPx, float strokePx, byte fillAlpha = 255)
        {
            EnsureGameResolution();

            int dBucket = Mathf.Max(DiameterBucket, Mathf.RoundToInt(diameterPx / DiameterBucket) * DiameterBucket);
            int sBucket = Mathf.Max(1, Mathf.RoundToInt(strokePx));
            int aBucket = Mathf.Clamp(fillAlpha, (byte)1, (byte)255);
            int key = (_textureSize << 16) ^ ((dBucket << 8) | (sBucket & 0xFF)) ^ (aBucket << 24);

            if (Cache.TryGetValue(key, out Sprite? cached) && cached != null)
            {
                _pinned = cached;
                return cached;
            }

            float strokeFrac = ResolveStrokeFraction(diameterPx, strokePx);
            Sprite sprite = BuildRing(strokeFrac, (byte)aBucket);
            Remember(key, sprite);
            _pinned = sprite;
            return sprite;
        }

        internal static void Invalidate()
        {
            _pinned = null;
            for (int i = 0; i < CacheOrder.Count; i++)
            {
                if (!Cache.TryGetValue(CacheOrder[i], out Sprite? sprite) || sprite == null)
                    continue;

                if (sprite.texture != null)
                    Object.Destroy(sprite.texture);
                Object.Destroy(sprite);
            }

            Cache.Clear();
            CacheOrder.Clear();
        }

        private static float ResolveStrokeFraction(float diameterPx, float strokePx)
        {
            float safeDiameter = Mathf.Max(diameterPx, strokePx * 4f);
            float targetFrac = strokePx / safeDiameter;
            float minFrac = MinRingTexPx / (float)_textureSize;
            return Mathf.Clamp(targetFrac, minFrac, 0.35f);
        }

        private static void EnsureGameResolution()
        {
            if (!TryResolveGameResolution(out int width, out int height))
            {
                width = 1920;
                height = 1080;
            }

            if (width == _screenWidth && height == _screenHeight && _textureSize >= MinTextureSize)
                return;

            int nextSize = ComputeTextureSize(width, height);
            bool sizeChanged = nextSize != _textureSize;
            _screenWidth = width;
            _screenHeight = height;
            _textureSize = nextSize;
            unchecked
            {
                _resolutionStamp++;
            }

            if (sizeChanged)
                Invalidate();
        }

        private static bool TryResolveGameResolution(out int width, out int height)
        {
            width = Screen.width;
            height = Screen.height;
            if (width >= 640 && height >= 480)
                return true;

            if (!PlayerPrefs.HasKey("ScreenResolution"))
                return false;

            string raw = PlayerPrefs.GetString("ScreenResolution", string.Empty);
            if (string.IsNullOrEmpty(raw))
                return false;

            int x = raw.IndexOf('x');
            if (x <= 0 || x >= raw.Length - 1)
                return false;

            if (!int.TryParse(raw.Substring(0, x), out width))
                return false;
            if (!int.TryParse(raw.Substring(x + 1), out height))
                return false;

            return width >= 640 && height >= 480;
        }

        private static int ComputeTextureSize(int screenWidth, int screenHeight)
        {
            float maxDiameterPx = Mathf.Max(screenWidth, 1) * MaxRadiusScreenFrac * 2f;
            int bucketed = Mathf.CeilToInt(maxDiameterPx / TextureSizeBucket) * TextureSizeBucket;
            return Mathf.Clamp(bucketed, MinTextureSize, MaxTextureSize);
        }

        private static void Remember(int key, Sprite sprite)
        {
            while (CacheOrder.Count >= MaxCached)
            {
                int oldest = CacheOrder[0];
                CacheOrder.RemoveAt(0);

                if (!Cache.TryGetValue(oldest, out Sprite? old) || old == null)
                {
                    Cache.Remove(oldest);
                    continue;
                }

                if (old == _pinned)
                {
                    CacheOrder.Add(oldest);
                    break;
                }

                if (old.texture != null)
                    Object.Destroy(old.texture);
                Object.Destroy(old);
                Cache.Remove(oldest);
            }

            Cache[key] = sprite;
            CacheOrder.Add(key);
        }

        private static Sprite BuildRing(float strokeFrac, byte fillAlpha)
        {
            int size = _textureSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            float cx = (size - 1) * 0.5f;
            float outer = cx;
            float inner = outer * (1f - strokeFrac);
            if (inner < 0f)
                inner = 0f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cx;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * size + x] = r >= inner && r <= outer
                        ? new Color32(255, 255, 255, fillAlpha)
                        : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
