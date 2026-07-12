using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.HardKill
{
    internal static class CombatHudMarkerPlacement
    {
        private const int MaxCached = 8;

        private static readonly FieldInfo? MarkerLookupField = typeof(CombatHUD).GetField(
            "markerLookup",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PersistentID[] CachedIds = new PersistentID[MaxCached];
        private static readonly Vector2[] CachedCenters = new Vector2[MaxCached];
        private static readonly bool[] CachedHits = new bool[MaxCached];
        private static int _cachedCount;
        private static int _cachedFrame = -1;
        private static Dictionary<Unit, HUDUnitMarker>? _cachedLookup;
        private static int _lookupFrame = -1;

        internal static bool TryGetThreatMarkerScreenCenter(Unit unit, out Vector2 screenCenter)
        {
            screenCenter = default;
            if (unit == null)
                return false;

            int frame = Time.frameCount;
            PersistentID id = unit.persistentID;
            if (_cachedFrame == frame && id.IsValid)
            {
                for (int i = 0; i < _cachedCount; i++)
                {
                    if (CachedIds[i] != id)
                        continue;

                    if (!CachedHits[i])
                        return false;

                    screenCenter = CachedCenters[i];
                    return true;
                }
            }
            else if (_cachedFrame != frame)
            {
                _cachedFrame = frame;
                _cachedCount = 0;
            }

            bool hit = TryResolveUncached(unit, out screenCenter);
            if (_cachedCount < MaxCached && id.IsValid)
            {
                CachedIds[_cachedCount] = id;
                CachedCenters[_cachedCount] = screenCenter;
                CachedHits[_cachedCount] = hit;
                _cachedCount++;
            }

            return hit;
        }

        private static bool TryResolveUncached(Unit unit, out Vector2 screenCenter)
        {
            screenCenter = default;
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || !hud.MarkerExists(unit))
                return false;

            Dictionary<Unit, HUDUnitMarker>? lookup = ResolveLookup(hud);
            if (lookup == null)
                return false;

            if (!lookup.TryGetValue(unit, out HUDUnitMarker marker))
                return false;

            Image? image = marker.image;
            if (image == null || !image.enabled)
                return false;

            Vector3 pos = image.rectTransform.position;
            screenCenter = new Vector2(pos.x, pos.y);
            return true;
        }

        private static Dictionary<Unit, HUDUnitMarker>? ResolveLookup(CombatHUD hud)
        {
            int frame = Time.frameCount;
            if (_lookupFrame == frame && _cachedLookup != null)
                return _cachedLookup;

            _lookupFrame = frame;
            _cachedLookup = MarkerLookupField?.GetValue(hud) as Dictionary<Unit, HUDUnitMarker>;
            return _cachedLookup;
        }

        internal static void InvalidateFrameCache()
        {
            _cachedFrame = -1;
            _cachedCount = 0;
            _lookupFrame = -1;
            _cachedLookup = null;
        }
    }
}
