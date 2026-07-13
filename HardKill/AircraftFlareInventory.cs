using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    /// <summary>
    /// Flare count matching CombatHUD DisplayCountermeasureAmmo (sum of all FlareEjector.ammo).
    /// </summary>
    internal static class AircraftFlareInventory
    {
        private static Aircraft? _cachedAircraft;
        private static int _cachedFrame = -1;
        private static int _cachedCount = -1;
        private static FlareEjector[]? _cachedEjectors;

        internal static int CountRemainingFlares(Aircraft aircraft)
        {
            if (!NocsGuard.IsValidUnit(aircraft))
                return 0;

            int frame = Time.frameCount;
            if (ReferenceEquals(_cachedAircraft, aircraft)
                && _cachedFrame == frame
                && _cachedCount >= 0)
            {
                return _cachedCount;
            }

            if (!ReferenceEquals(_cachedAircraft, aircraft) || _cachedEjectors == null)
            {
                _cachedAircraft = aircraft;
                _cachedEjectors = aircraft!.GetComponentsInChildren<FlareEjector>(true);
            }

            int total = 0;
            FlareEjector[] ejectors = _cachedEjectors;
            for (int i = 0; i < ejectors.Length; i++)
            {
                FlareEjector? ejector = ejectors[i];
                if (ejector == null)
                    continue;

                // Match CountermeasureStation.CountAmmo / FlareEjector.GetAmmo.
                total += Mathf.Max(0, ejector.GetAmmo());
            }

            _cachedFrame = frame;
            _cachedCount = total;
            return total;
        }

        internal static void Invalidate()
        {
            _cachedFrame = -1;
            _cachedAircraft = null;
            _cachedCount = -1;
            _cachedEjectors = null;
        }
    }
}
