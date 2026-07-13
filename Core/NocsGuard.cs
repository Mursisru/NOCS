using System;
using NuclearOption.Networking;
using UnityEngine;

namespace NOCS.Core
{
    internal static class NocsGuard
    {
        internal static bool IsValidUnit(Unit? unit)
        {
            try
            {
                return unit != null && !unit.disabled;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsValidMissile(Missile? missile)
        {
            try
            {
                return missile != null && !missile.disabled && missile.rb != null;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsLocalPlayerAircraft(Aircraft? aircraft)
        {
            if (!IsValidUnit(aircraft))
                return false;

            try
            {
                // Canonical once GameManager.SetLocalPlayer has run.
                if (GameManager.GetLocalAircraft(out Aircraft local) && local != null)
                    return local == aircraft;

                // MP spawn race: Aircraft.OnStartClient / CombatHUD.SetAircraft can run
                // before Player.OnStartLocalPlayer sets GameManager._localPlayer.
                // Match vanilla CheckIfLocalSim — Mirage IsLocalPlayer on the owner.
                Player? owner = aircraft!.Player;
                return owner != null && owner.IsLocalPlayer;
            }
            catch
            {
                return false;
            }
        }

        internal static bool CanMutateLocalWeapons(Aircraft? aircraft)
        {
            if (!IsLocalPlayerAircraft(aircraft))
                return false;

            try
            {
                // MountedMissile only sends CmdLaunchMissile with authority (or Rpc on server).
                return aircraft!.HasAuthority || aircraft.IsServer;
            }
            catch
            {
                return false;
            }
        }
    }
}
