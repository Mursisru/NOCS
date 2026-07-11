using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.HardKill
{
    internal static class CombatHudMarkerPlacement
    {
        private static readonly FieldInfo? MarkerLookupField = typeof(CombatHUD).GetField(
            "markerLookup",
            BindingFlags.Instance | BindingFlags.NonPublic);

        internal static bool TryGetThreatMarkerScreenCenter(Unit unit, out Vector2 screenCenter)
        {
            screenCenter = default;
            if (unit == null)
                return false;

            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || !hud.MarkerExists(unit))
                return false;

            if (MarkerLookupField?.GetValue(hud) is not Dictionary<Unit, HUDUnitMarker> lookup)
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
    }
}
