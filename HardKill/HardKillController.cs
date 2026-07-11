using System.Collections.Generic;
using NOCS.Util;
using NOCS.Config;
using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    internal static class HardKillController
    {
        private static readonly HardKillSession Session = new HardKillSession();
        private static readonly WeaponAllocationEngine Allocator = new WeaponAllocationEngine();

        private static AseCircleView? _aseView;
        private static Transform? _aseCanvas;

        internal static void RunTick(float dt)
        {
            if (!NocsConfigCache.HardKillEnabled)
            {
                _aseView?.SetVisible(false);
                return;
            }

            if (!GameManager.flightControlsEnabled)
            {
                _aseView?.SetVisible(false);
                return;
            }

            CombatHUD? combatHud = SceneSingleton<CombatHUD>.i;
            Aircraft? aircraft = combatHud?.aircraft;
            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
            {
                _aseView?.SetVisible(false);
                return;
            }

            ThreatEngagementLedger.PruneInvalid();

            WeaponStation? defensive = ResolveActiveDefensiveStation(aircraft!);
            SwarmInterceptSample swarmSample = UpdateAsePreview(aircraft!, defensive);

            if (!Session.Active
                && NocsHotKey.WasPressed(NocsConfigCache.HotKeyModifier, NocsConfigCache.HotKey)
                && Allocator.PrepareEngagement(aircraft!, defensive))
            {
                BeginSession(aircraft!);
            }

            if (!Session.Active)
                return;

            if (Allocator.QueueComplete)
            {
                if (Session.PendingOwnLaunches <= 0)
                    FinishSession(aircraft!);
                return;
            }

            int started = Allocator.RunSalvo(aircraft!, dt, defensive);
            if (started > 0)
                Session.PendingOwnLaunches += started;
        }

        internal static void HandleOwnMissileRegistered(Missile missile)
        {
            if (!Session.Active || !Session.RestorePending)
                return;

            if (missile == null || missile.owner == null)
                return;

            if (!NocsGuard.IsLocalPlayerAircraft(missile.owner as Aircraft))
                return;

            if (Session.PendingOwnLaunches > 0)
                Session.PendingOwnLaunches--;

            if (Session.PendingOwnLaunches > 0)
                return;

            if (!Allocator.QueueComplete)
                return;

            Aircraft? owner = missile.owner as Aircraft;
            if (!NocsGuard.IsLocalPlayerAircraft(owner))
                return;

            FinishSession(owner!);
        }

        internal static void ResetSession()
        {
            Session.Reset();
            Allocator.Reset();
            WeaponStationCatalog.InvalidateFrameCache();
        }

        internal static void DisposeViews()
        {
            ResetSession();
            ThreatEngagementLedger.Reset();
            TtiSmoothingTracker.Reset();
            if (_aseView != null)
            {
                _aseView.Dispose();
                _aseView = null;
            }

            _aseCanvas = null;
            AseCircleSprite.Invalidate();
        }

        private static void BeginSession(Aircraft aircraft)
        {
            if (!NocsGuard.CanMutateLocalWeapons(aircraft))
                return;

            if (!Session.SnapshotCaptured)
            {
                Session.SavedTargets = TargetSnapshotStore.Capture(aircraft);
                Session.SnapshotCaptured = true;
            }

            aircraft.weaponManager?.ClearTargetList();
            Allocator.RepointTargets(aircraft);

            Session.Active = true;
            Session.RestorePending = true;
            Session.PendingOwnLaunches = 0;
        }

        private static void FinishSession(Aircraft aircraft)
        {
            if (Session.SnapshotCaptured)
                TargetRestoreValidator.Restore(aircraft, Session.SavedTargets);

            Session.Reset();
            Allocator.Reset();
            _aseView?.SetVisible(false);
        }

        private static SwarmInterceptSample UpdateAsePreview(
            Aircraft aircraft,
            WeaponStation? defensive)
        {
            if (!NocsConfigCache.AseCircleEnabled)
            {
                _aseView?.SetVisible(false);
                return default;
            }

            IReadOnlyList<Missile> threats = MwsThreatFilter.GetScratch(aircraft, defensive);
            if (threats.Count == 0)
            {
                if (!Session.Active)
                    _aseView?.SetVisible(false);
                return default;
            }

            SwarmInterceptSample sample = SwarmInterceptGeometry.Compute(aircraft, threats, defensive);
            EnsureAseView();
            _aseView?.Apply(in sample, aircraft, defensive);
            return sample;
        }

        private static WeaponStation? ResolveActiveDefensiveStation(Aircraft aircraft)
        {
            return MwsThreatFilter.ResolveActiveWeapon(aircraft, preferred: null);
        }

        private static void EnsureAseView()
        {
            Transform? canvas = NocsHudPlacement.ResolveFlightHudCanvas();
            if (canvas == null)
                return;

            if (_aseView != null && _aseCanvas == canvas)
                return;

            _aseView?.Dispose();
            _aseCanvas = canvas;
            _aseView = new AseCircleView(canvas);
        }
    }
}
