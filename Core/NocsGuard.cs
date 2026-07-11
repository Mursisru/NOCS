using UnityEngine;

namespace NOCS.Core
{
    internal static class NocsGuard
    {
        internal static bool IsValidUnit(Unit? unit)
        {
            return unit != null && !unit.disabled;
        }

        internal static bool IsValidMissile(Missile? missile)
        {
            return missile != null && !missile.disabled && missile.rb != null;
        }

        internal static bool IsLocalPlayerAircraft(Aircraft? aircraft)
        {
            if (!IsValidUnit(aircraft))
                return false;

            if (GameManager.GetLocalAircraft(out Aircraft local) && local != null)
                return local == aircraft;

            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            return hud != null && hud.aircraft == aircraft;
        }

        internal static bool CanMutateLocalWeapons(Aircraft? aircraft)
        {
            return IsLocalPlayerAircraft(aircraft);
        }
    }
}
