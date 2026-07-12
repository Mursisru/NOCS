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
        private static SwarmInterceptSample _lastAseSample;

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
            Allocator.IsHardwareSalvoLocked();

            WeaponStation? defensive = ResolveActiveDefensiveStation(aircraft!);
            SwarmInterceptSample sample = UpdateAsePreview(aircraft!, defensive);
            _lastAseSample = sample;

            bool hotkeySalvoThisTick = false;
            if (NocsHotKey.WasPressed(NocsConfigCache.HotKeyModifier, NocsConfigCache.HotKey))
            {
                // Geometry gate always uses the best available defensive station — never the
                // last-fired mount (short-range IR could falsely fail AllEnvelopesInWeaponRange).
                if (!Allocator.IsHardwareSalvoLocked()
                    && HotTriggerGate.IsSalvoTriggerAllowed(in sample, defensive, aircraft!))
                {
                    if (Session.Active)
                        Allocator.ExtendEngagement(aircraft!, defensive);
                    else if (Allocator.PrepareEngagement(aircraft!, defensive))
                        BeginSession(aircraft!);

                    if (Session.Active && !Allocator.IsSalvoComplete)
                    {
                        Allocator.RunSalvo(aircraft!, 0f, defensive);
                        hotkeySalvoThisTick = true;
                    }
                }
            }

            if (!Session.Active)
                return;

            if (Allocator.IsHardwareSalvoLocked())
            {
                // Keep session alive for remaining threats, but never spin fire while locked.
                if (Allocator.IsSalvoComplete)
                    Allocator.TryKeepSessionAlive(aircraft!, defensive);
                return;
            }

            if (Allocator.IsSalvoComplete)
            {
                if (Allocator.TryKeepSessionAlive(aircraft!, defensive))
                {
                    if (!hotkeySalvoThisTick
                        && HotTriggerGate.IsSalvoTriggerAllowed(in _lastAseSample, defensive, aircraft!))
                        Allocator.RunSalvo(aircraft!, dt, defensive);
                    return;
                }

                if (Session.PendingOwnLaunches <= 0)
                    FinishSession(aircraft!);
                return;
            }

            if (!hotkeySalvoThisTick
                && HotTriggerGate.IsSalvoTriggerAllowed(in _lastAseSample, defensive, aircraft!))
                Allocator.RunSalvo(aircraft!, dt, defensive);
        }

        internal static void NotifySalvoLaunchCommitted()
        {
            Session.PendingOwnLaunches++;
        }

        internal static void HandleOwnMissileRegistered(Missile missile)
        {
            if (!Session.Active || !Session.RestorePending)
                return;

            if (missile == null || missile.owner == null)
                return;

            Aircraft? owner = missile.owner as Aircraft;
            if (!NocsGuard.IsLocalPlayerAircraft(owner))
                return;

            if (Session.PendingOwnLaunches > 0)
                Session.PendingOwnLaunches--;

            if (Session.PendingOwnLaunches > 0)
                return;

            if (!Allocator.IsSalvoComplete)
                return;

            WeaponStation? defensive = ResolveActiveDefensiveStation(owner!);
            if (Allocator.TryKeepSessionAlive(owner!, defensive))
                return;

            FinishSession(owner!);
        }

        internal static void ResetSession()
        {
            Session.Reset();
            Allocator.Reset();
            ThreatEngagementLedger.Reset();
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
            IReadOnlyList<Missile> threats = MwsThreatFilter.GetPreviewScratch(aircraft, defensive);
            if (threats.Count == 0)
            {
                _aseView?.SetVisible(false);
                return default;
            }

            SwarmInterceptSample sample = SwarmInterceptGeometry.Compute(aircraft, threats, defensive);
            if (!NocsConfigCache.RenderAseCircle && !NocsConfigCache.RenderRadialText)
            {
                _aseView?.SetVisible(false);
                return sample;
            }

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
