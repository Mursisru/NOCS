using System;
using System.Collections.Generic;
using NOCS.Config;
using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    internal struct WeaponStationEntry
    {
        internal byte Index;
        internal WeaponStation Station;
        internal SeekerKind Kind;
        internal float ArmDelay;
        internal bool IsIr;
        internal bool IsRadar;
    }

    internal static class WeaponStationCatalog
    {
        private static readonly List<WeaponStationEntry> Scratch = new List<WeaponStationEntry>(8);
        private static readonly Comparison<WeaponStationEntry> PriorityComparison = ComparePriority;

        private static Aircraft? _cachedAircraft;
        private static int _cachedFrame = -1;

        internal static IReadOnlyList<WeaponStationEntry> Build(Aircraft aircraft)
        {
            int frame = Time.frameCount;
            if (ReferenceEquals(_cachedAircraft, aircraft) && _cachedFrame == frame && Scratch.Count > 0)
                return Scratch;

            _cachedAircraft = aircraft;
            _cachedFrame = frame;
            Scratch.Clear();

            if (!NocsGuard.IsValidUnit(aircraft) || aircraft!.weaponStations == null)
                return Scratch;

            List<WeaponStation> stations = aircraft.weaponStations;
            for (int i = 0; i < stations.Count; i++)
            {
                WeaponStation? station = stations[i];
                if (!IsEligibleStation(station, aircraft))
                    continue;

                SeekerKind kind = SeekerParamCache.ResolveSeekerKind(station);
                Scratch.Add(new WeaponStationEntry
                {
                    Index = station!.Number,
                    Station = station,
                    Kind = kind,
                    ArmDelay = SeekerParamCache.GetMsnParams(station).ArmDelaySec,
                    IsIr = SeekerParamCache.IsIrSeeker(kind),
                    IsRadar = SeekerParamCache.IsRadarSeeker(kind),
                });
            }

            Scratch.Sort(PriorityComparison);
            return Scratch;
        }

        internal static int CountAvailableAmmo(Aircraft aircraft)
        {
            IReadOnlyList<WeaponStationEntry> stations = Build(aircraft);
            int total = 0;
            for (int i = 0; i < stations.Count; i++)
                total += stations[i].Station.Ammo;

            return total;
        }

        internal static bool HasLaunchableStation(Aircraft aircraft)
        {
            IReadOnlyList<WeaponStationEntry> stations = Build(aircraft);
            for (int i = 0; i < stations.Count; i++)
            {
                WeaponStation station = stations[i].Station;
                if (station.Ammo > 0 && station.Ready())
                    return true;
            }

            return false;
        }

        internal static void InvalidateFrameCache()
        {
            _cachedFrame = -1;
            _cachedAircraft = null;
            Scratch.Clear();
        }

        internal static bool IsEligibleStation(WeaponStation? station, Aircraft aircraft)
        {
            if (station == null || station.WeaponInfo == null)
                return false;

            if (!station.WeaponInfo.missile || station.Ammo <= 0)
                return false;

            if (station.SafetyIsOn(aircraft) || !station.Ready())
                return false;

            SeekerKind kind = SeekerParamCache.ResolveSeekerKind(station);
            if (kind == SeekerKind.None || kind == SeekerKind.Other)
                return false;

            if (NocsConfigCache.WeaponFilterMode == WeaponFilterMode.AntiMissileOnly)
                return station.WeaponInfo.effectiveness.antiMissile > 0f;

            return SeekerParamCache.IsIrSeeker(kind) || SeekerParamCache.IsRadarSeeker(kind);
        }

        private static int ComparePriority(WeaponStationEntry a, WeaponStationEntry b)
        {
            bool irFirst = NocsConfigCache.WeaponPriority == WeaponPriority.IR_First;
            if (a.IsIr != b.IsIr)
                return irFirst ? (a.IsIr ? -1 : 1) : (a.IsIr ? 1 : -1);

            return a.ArmDelay.CompareTo(b.ArmDelay);
        }
    }
}
