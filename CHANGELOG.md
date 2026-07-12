# Changelog

All notable changes to this project are documented in this file.

Format follows [Keep a Changelog](https://keepachangelog.com/). Versions use [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.5.18] — 2026-07-12

### Changed

- **ASE status cue placement** — lowered SHOOT / POTENTIAL HIT text under the weapon hint (18 px gap).

### Build

- Display version: `0.5.18QV`
- BepInPlugin semver: `0.5.18`

## [0.5.17] — 2026-07-12

### Changed

- **ASE status cue placement** — initial downward offset under weapon hint (12 px gap).

### Build

- Display version: `0.5.17QV`
- BepInPlugin semver: `0.5.17`

## [0.5.16] — 2026-07-12

### Fixed

- **Salvo no longer tied to on-screen ASE HUD** — hotkey uses threat/range gates by default; third-person and look-away no longer block launch. Optional **`RequireAseScreenShoot`** (default off) restores strict gun-cross-in-circle check.
- **`ShowAseInterceptRing`** replaces legacy `RenderAseCircle` config key — default **off** (old cfg value no longer forces ring on).

### Removed

- **`AllowLaunchWithoutAseShootCue`** — replaced by inverted **`RequireAseScreenShoot`**.

### Build

- Display version: `0.5.16QV`
- BepInPlugin semver: `0.5.16`

## [0.5.15] — 2026-07-12

### Added

- **`RenderAseCircle`** restored — optional ASE intercept ring with arc **SHOOT** / **POTENTIAL HIT** labels on the circumference (default **off**). Status cue under weapon hint remains on `RenderRadialText`.

### Build

- Display version: `0.5.15QV`
- BepInPlugin semver: `0.5.15`

## [0.5.14] — 2026-07-12

### Added

- **`AllowLaunchWithoutAseShootCue`** (Fire Control, default `false`) — when off, hotkey/auto salvo requires ASE **SHOOT** alignment; when on, salvo may fire on POTENTIAL HIT geometry only.

### Fixed

- **ASE status cue styling** — cue is parented under weapon `hint` (local layout, same font/size as vanilla hint); color/alpha from TrueNotch rectangle via `AseNotchStyle`.

### Build

- Display version: `0.5.14QV`
- BepInPlugin semver: `0.5.14`

## [0.5.13] — 2026-07-12

### Fixed

- **ASE status cue styling** — `SHOOT` / `POTENTIAL HIT` now uses legacy `UnityEngine.UI.Text` cloned from the weapon HUD hint (same font, size, color, and inherited canvas alpha) instead of a separate TMP/notch style.

### Build

- Display version: `0.5.13QV`
- BepInPlugin semver: `0.5.13`

## [0.5.12] — 2026-07-12

### Changed

- **ASE visuals** — intercept ring and radial arc labels removed. Cue is now `SHOOT` / `POTENTIAL HIT` status text placed directly under the weapon HUD hint (`OUT OF RANGE`, `TOO CLOSE`, …), using the TrueNotch rectangle color and transparency. Geometry/launch gates unchanged. Toggle: `RenderRadialText`.

### Build

- Display version: `0.5.12QV`
- BepInPlugin semver: `0.5.12`

## [0.5.11] — 2026-07-12

### Fixed

- **Root cause: short-range (~2 km) multi-threat salvo** — ASE/preview kinematic filters (CPA / closure / seeker-kind) could leave only one inbound in the fire queue while vanilla MWS still showed several. Long-range shots were mostly head-on and passed those filters, so salvo worked there.
  - Salvo queue now uses **raw MWS collect** (`GetSalvoScratch`): every known+unknown missile targeting self inside max range — no CPA/closure/seeker gates.
  - Same-frame **ripple fire** across every Ready station until ammo/threats exhausted, then cooldown wait.
  - Session keep-alive re-syncs from raw MWS before finishing.

### Build

- Display version: `0.5.11QV`
- BepInPlugin semver: `0.5.11`

## [0.5.10] — 2026-07-12

### Fixed

- **Short-range multi-missile salvo still needing extra hotkey presses** — full salvo path audit:
  - Committed launch gate no longer applies weapon max-range (only valid threat + distance).
  - Queued threats are never skipped for reload / soft gates; session waits for `Ready()`.
  - Station catalog for allocation no longer requires `Ready()` (ammo-eligible stations stay visible mid-fireInterval).
  - Launch budget refreshes as new inbound threats sync into the queue.
  - Session refuses to finish while unengaged threats and ammo remain (`TryKeepSessionAlive` on tick and on own-missile register).
  - Preview collect keeps the closest threats when the candidate list is full.

### Build

- Display version: `0.5.10QV`
- BepInPlugin semver: `0.5.10`

## [0.5.9] — 2026-07-12

### Fixed

- **ASE ring anchored on aircraft nose** — visual ring center reverts to inbound threat HUD markers (single: marker center + angular radius; swarm: centroid of threat markers). Gun cross is no longer used for ring placement.

### Build

- Display version: `0.5.9QV`
- BepInPlugin semver: `0.5.9`

## [0.5.8] — 2026-07-12

### Fixed

- **Multi-missile salvo sometimes needed extra hotkey presses** — transient launch blocks (station reload, timing gate) no longer skip queued threats; session auto-resumes while unengaged targets and ammo remain; first shot fires on the same hotkey frame.

### Build

- Display version: `0.5.8QV`
- BepInPlugin semver: `0.5.8`

## [0.5.7] — 2026-07-12

### Fixed

- **ASE ring oversized** — merged radius capped to 28% screen width; envelope sizing closure clamped so Doppler-boosted preview closure no longer inflates lead angle / ring diameter.
- **Single-threat ring on missile marker** — one inbound threat anchors the ring on the gun cross (velocity vector) with angular intercept radius only, not a bounding circle spanning gun cross → threat HUD icon.

### Build

- Display version: `0.5.7QV`
- BepInPlugin semver: `0.5.7`

## [0.5.6] — 2026-07-12

### Fixed

- **ASE ring late on crossing / Doppler shots** — preview now uses inbound-speed closure estimate (not raw LOS closure only), distance-scaled CPA tolerance, and forced inclusion inside `AsePreviewAppearDistanceM` (default 5 km).
- **Preview range floor** — ASE preview range is at least `AsePreviewAppearDistanceM` so the ring can appear from 5 km slant range.

### Added

- Config **`AsePreviewAppearDistanceM`** (default `5000`) in Engagement Envelope.

### Build

- Display version: `0.5.6QV`
- BepInPlugin semver: `0.5.6`

## [0.5.5] — 2026-07-12

### Changed

- **Hard-Kill hot-path performance** — per-frame caches for MWS threat collect (preview/engage), active weapon resolution, and eligible ammo count; salvo queue sync now filters only new threats instead of copying the full scratch list each tick.

### Build

- Display version: `0.5.5P`
- BepInPlugin semver: `0.5.5`

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
