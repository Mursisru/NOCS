# Changelog

All notable changes to this project are documented in this file.

Format follows [Keep a Changelog](https://keepachangelog.com/). Versions use [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.5.4] — 2026-07-12

### Fixed

- **Short-range multi-missile salvo** — after the first launch, close threats were dropped by `AbsoluteMin` / arm-dead-zone / range re-gates, so the session ended or stuck and a second hotkey was required (or did nothing):
  - Committed salvo uses soft `IsCommittedSalvoLaunchAllowed` (valid threat + weapon max range only).
  - Queued threats are no longer skipped for arm-dead-zone / AbsoluteMin during the salvo.
  - Threat collect no longer hard-filters `AbsoluteMinEngagementRange` (still available in config for future auto-engage).
  - Wait on station `Ready()` / fireInterval instead of closing the session early.

### Build

- Display version: `0.5.4QV`
- BepInPlugin semver: `0.5.4`

## [0.5.3] — 2026-07-12

### Fixed

- **Hard-Kill salvo** — single hotkey now chains multi-missile launches without a second press:
  - Reserve `PendingOwnLaunches` before synchronous `RegisterMissile` callback.
  - End session only when salvo queue is truly complete (not after first in-flight registration).
  - Threat queue seeded from the same MWS scratch list as the ASE preview (all visible inbound targets).
- **Multi-station launch** — per-shot gate and launch iterate every ready defensive station; budget counts ammo across all eligible stations (not only the current ready slot). Empty or reloading stations are skipped automatically.

### Build

- Display version: `0.5.3QV`
- BepInPlugin semver: `0.5.3`

## [0.5.2] — 2026-07-12

### Changed

- **Fire-control configuration** — Hard-Kill parameters moved into Configuration Manager sections:
  - `1. HUD Visuals` — `RenderAseCircle`, `RenderRadialText`, `AseVisualScale`
  - `2. Engagement Envelope` — `AbsoluteMaxEngagementRange`, `AbsoluteMinEngagementRange`, `AseMaxRangeFactor` (+ advanced envelope math)
  - `3. Fire Control & Geometry` — `AseGateToleranceAngle`, `LaunchCooldown` (default `0.35`), `MaxCpaMeters`
  - `4. Signal & Tracking` — `TtiSmoothingFactor`, `ClosureMinThreshold`, `EngageIrThreats`
- Config cache updates only on `SettingChanged` / mission startup (no per-frame `.Value` reads).

### Removed

- Bypass toggles `SafetyDistanceGate` and `AseCircleEnabled` (replaced by tolerance angle and independent HUD render flags).
- Unused `AseMetersToRefPx` and `MinClosureMps` (replaced by `ClosureMinThreshold`).

### Added

- **`EngageIrThreats`** — optional IR threat tracking/engage (default radar-only).
- Absolute engagement range floors/ceilings for the fire-control filter.
- **Configuration Manager fallback** — `Ctrl+F10` toggles CM when Nuclear Option blocks the default hotkey path.

### Fixed

- **Configuration Manager (Nuclear Option)** — document `HideManagerGameObject = true` in `BepInEx.cfg` so CM Update/OnGUI survives scene load.

### Build

- Display version: `0.5.2QV`
- BepInPlugin semver: `0.5.2`

## [0.0.0] — 2026-07-11

### Added

- Initial **BepInEx 5** release of NO Countermeasures Supporter (NOCS).
- **TrueNotch HUD** — jam-boundary notch width overlay.
- **Hard-Kill APS** — swarm ASE bounding circle, salvo hotkey, track restore.
- **Warning TTI** — smoothed MWS time-to-impact labels.
- **BepInEx Configuration Manager** bindings (`com.at747.nocs.bepinex.cfg`) for all settings.
- Harmony patches: `FlightHud.Awake`, `ThreatItem.AnimateItem`, `CombatHUD.SetAircraft`.

### Fixed

- **TrueNotch width** — jam gate width from angular physics via `NocsScreenScale.RadiansToPx`; stable under camera pitch/roll.
- **TrueNotch local scale** — screen px/local via `TransformPoint` instead of `lossyScale.x`.
- **Hard-Kill salvo** — `NocsHost.LateUpdate` tick chain so cooldown gate advances between launches on one hotkey press.
- **`NocsAircraftBinder`** — skip `ResetSession` when `CombatHUD.SetAircraft` re-binds the same local aircraft.

### Build

- Display version: `0.0.0` (initial BepInEx port)
- BepInPlugin semver: `0.0.0`
