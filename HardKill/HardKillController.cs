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

            if (!GameManager.GetLocalAircraft(out Aircraft aircraft) || aircraft == null)
            {
                _aseView?.SetVisible(false);
                return;
            }

            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
            {
                _aseView?.SetVisible(false);
                return;
            }

            // Recover bind if CombatHUD.SetAircraft raced before GameManager.SetLocalPlayer.
            NocsAircraftBinder.EnsureBound(aircraft);

            ThreatEngagementLedger.PruneInvalid();
            Allocator.IsHardwareSalvoLocked();
            TickPendingLaunchWatchdog();

            WeaponStation? defensive = ResolveActiveDefensiveStation(aircraft);
            SwarmInterceptSample sample = UpdateAsePreview(aircraft, defensive);
            _lastAseSample = sample;

            bool hotkeySalvoThisTick = false;
            bool locked = Allocator.IsHardwareSalvoLocked();
            bool shootOpen = HotTriggerGate.IsSalvoTriggerAllowed(in sample, defensive, aircraft);

            if (NocsHotKey.WasPressed(NocsConfigCache.HotKeyModifier, NocsConfigCache.HotKey))
            {
                // Geometry gate always uses the best available defensive station — never the
                // last-fired mount (short-range IR could falsely fail AllEnvelopesInWeaponRange).
                if (!locked && shootOpen)
                {
                    if (Session.Active)
                        Allocator.ExtendEngagement(aircraft, defensive);
                    else if (Allocator.PrepareEngagement(aircraft, defensive))
                        BeginSession(aircraft);

                    if (Session.Active && !Allocator.IsSalvoComplete)
                    {
                        Allocator.RunSalvo(aircraft, 0f, defensive);
                        hotkeySalvoThisTick = true;
                    }
                }
            }
            else if (NocsConfigCache.AutoEngage
                && !Session.Active
                && !locked
                && shootOpen)
            {
                if (Allocator.PrepareEngagement(aircraft, defensive))
                {
                    BeginSession(aircraft);
                    if (Session.Active && !Allocator.IsSalvoComplete)
                    {
                        Allocator.RunSalvo(aircraft, 0f, defensive);
                        hotkeySalvoThisTick = true;
                    }
                }
            }

            if (!Session.Active)
                return;

            Allocator.MaintainActiveEngagement(aircraft, defensive);

            if (Allocator.IsHardwareSalvoLocked())
            {
                // Keep session alive for remaining threats, but never spin fire while locked.
                if (Allocator.IsSalvoComplete)
                    Allocator.TryKeepSessionAlive(aircraft, defensive);
                return;
            }

            if (Allocator.IsSalvoComplete)
            {
                if (Allocator.TryKeepSessionAlive(aircraft, defensive))
                {
                    if (!hotkeySalvoThisTick
                        && HotTriggerGate.IsSalvoTriggerAllowed(in _lastAseSample, defensive, aircraft))
                        Allocator.RunSalvo(aircraft, dt, defensive);
                    return;
                }

                if (Session.PendingOwnLaunches <= 0)
                    FinishSession(aircraft);
                return;
            }

            if (!hotkeySalvoThisTick
                && HotTriggerGate.IsSalvoTriggerAllowed(in _lastAseSample, defensive, aircraft))
                Allocator.RunSalvo(aircraft, dt, defensive);
        }

        internal static int PendingOwnLaunchesCount => Session.PendingOwnLaunches;

        internal static void NotifySalvoLaunchCommitted()
        {
            Session.PendingOwnLaunches++;
            Session.PendingLaunchStamp = Time.time;
        }

        internal static void RollbackSalvoLaunchCommitted()
        {
            if (Session.PendingOwnLaunches > 0)
                Session.PendingOwnLaunches--;

            if (Session.PendingOwnLaunches <= 0)
                Session.PendingLaunchStamp = -1000f;
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

            if (Session.PendingOwnLaunches <= 0)
                Session.PendingLaunchStamp = -1000f;

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
            Allocator.Reset(clearHardwarePairLock: true);
            ThreatEngagementLedger.Reset();
            TrackingCmdDebounce.Reset();
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

        private static void TickPendingLaunchWatchdog()
        {
            if (Session.PendingOwnLaunches <= 0)
                return;

            if (Time.time - Session.PendingLaunchStamp < HardKillSession.PendingLaunchTimeoutSec)
                return;

            Session.PendingOwnLaunches = 0;
            Session.PendingLaunchStamp = -1000f;
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
            Session.PendingLaunchStamp = -1000f;
        }

        private static void FinishSession(Aircraft aircraft)
        {
            if (Session.SnapshotCaptured)
                TargetRestoreValidator.Restore(aircraft, Session.SavedTargets);

            Session.Reset();
            Allocator.Reset(clearHardwarePairLock: false);
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
