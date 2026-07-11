using NOCS.Core;

namespace NOCS.HardKill
{
    internal static class NocsAircraftBinder
    {
        private static Aircraft? _boundAircraft;

        internal static void HandleSetAircraft(Aircraft? aircraft)
        {
            UnbindMissileHandler();
            MwsThreatFilter.Unbind();
            HardKillController.ResetSession();

            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
                return;

            BindMissileHandler(aircraft!);
            MwsThreatFilter.Bind(aircraft!);
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
