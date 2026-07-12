using NOCS.Core;

namespace NOCS.HardKill
{
    internal static class NocsAircraftBinder
    {
        private static Aircraft? _boundAircraft;

        internal static void HandleSetAircraft(Aircraft? aircraft)
        {
            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
            {
                if (_boundAircraft == null)
                    return;

                // Do not tear down on a false-negative race if the currently bound
                // airframe is still owned by the local Mirage player.
                if (_boundAircraft.Player != null && _boundAircraft.Player.IsLocalPlayer)
                    return;

                UnbindMissileHandler();
                MwsThreatFilter.Unbind();
                HardKillController.ResetSession();
                return;
            }

            if (_boundAircraft == aircraft)
                return;

            UnbindMissileHandler();
            MwsThreatFilter.Unbind();
            HardKillController.ResetSession();
            BindMissileHandler(aircraft!);
            MwsThreatFilter.Bind(aircraft!);
        }

        /// <summary>
        /// Lazy rebind after GameManager._localPlayer becomes available — compensates for
        /// a CombatHUD.SetAircraft that was rejected during the MP spawn race.
        /// </summary>
        internal static void EnsureBound(Aircraft aircraft)
        {
            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
                return;

            if (_boundAircraft == aircraft)
                return;

            HandleSetAircraft(aircraft);
        }

        internal static void SafeUnbind()
        {
            UnbindMissileHandler();
            MwsThreatFilter.Unbind();
        }

        private static void BindMissileHandler(Aircraft aircraft)
        {
            if (!NocsGuard.IsValidUnit(aircraft))
                return;

            UnbindMissileHandler();
            _boundAircraft = aircraft;
            aircraft.onRegisterMissile += OnRegisterMissileHandler;
        }

        private static void UnbindMissileHandler()
        {
            Aircraft? prev = _boundAircraft;
            _boundAircraft = null;

            if (prev == null || prev.disabled)
                return;

            try
            {
                prev.onRegisterMissile -= OnRegisterMissileHandler;
            }
            catch
            {
            }
        }

        private static void OnRegisterMissileHandler(Missile missile)
        {
            HardKillController.HandleOwnMissileRegistered(missile);
        }
    }
}
