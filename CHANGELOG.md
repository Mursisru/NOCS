# Changelog

All notable changes to this project are documented in this file.

Format follows [Keep a Changelog](https://keepachangelog.com/). Versions use [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.6.12] — 2026-07-13

### Build

- Display version: `0.6.12QV`
- BepInPlugin semver: `0.6.12`

## [0.5.47] — 2026-07-13

### Fixed

- **Inbound IR flare gate** — with `X = IrInterceptBlockedAboveFlares > 0`, engage inbound IR while HUD flares **&lt; X** (no longer requires `EngageIrThreats`). `X = 0` uses `EngageIrThreats` only.
- **Flare count** — matches game `FlareEjector.GetAmmo` sum (same as `CountermeasureStation.CountAmmo` / HUD ammo), cached per frame.
- **IR-first salvo** — while own IR ammo remains, failed IR launch waits (never skips threat / never falls back to radar mid-IR).
- **ASE station** — fire-control weapon resolve follows IR-first catalog (ignores unrelated player weapon selection).

### Build

- Display version: `0.5.47QV`
- BepInPlugin semver: `0.5.47`

## [0.5.46] — 2026-07-13

### Fixed

- **Separated flare gate from own-weapon priority**
  - `IrInterceptBlockedAboveFlares` (X): inbound IR threats only while remaining flares **&lt; X** (needs `EngageIrThreats`). `0` = no flare gate.
  - Own interceptor order is always **IR pylons first** while any IR ammo remains; radar pylons only after IR is empty. Flare count never switches station type.

### Build

- Display version: `0.5.46QV`
- BepInPlugin semver: `0.5.46`

## [0.5.45] — 2026-07-13

### Fixed

- **`IrInterceptBlockedAboveFlares` fail-safe** — flare reserve prefers radar for ARH/SARH only when radar ammo exists. IR pylons are always used when flares are at/below threshold, radar ammo is empty, or the inbound threat requires IR engagement (`EngageIrThreats`). Safe at any slider value (0 = off).

### Build

- Display version: `0.5.45QV`
- BepInPlugin semver: `0.5.45`

## [0.5.44] — 2026-07-13

### Fixed

- **IR-first salvo** — while IR pylon ammo remains, active station never falls back to radar (even when IR slots are reloading). `PickFirstReadyStation` now requires `Ready()`.
- **Salvo continuation** — after the 2-shot hardware window, session keeps pulling new RWR threats during pair lock; failed IR cooldown waits instead of skipping threats or ending early.
- **Flare reserve gate** — default `IrInterceptBlockedAboveFlares` is `0` (off). IR-first is restored out of the box; set the slider to enable flare conservation.

### Build

- Display version: `0.5.44QV`
- BepInPlugin semver: `0.5.44`

## [0.5.43] — 2026-07-13

### Added

- **`IrInterceptBlockedAboveFlares`** (section `4. Signal & Tracking`, default `4`, slider 0–128) — while remaining IR flares exceed this count, Hard-Kill skips IR pylons and uses radar intercept only. `0` disables the gate.

### Build

- Display version: `0.5.43QV`
- BepInPlugin semver: `0.5.43`

## [0.5.42] — 2026-07-13

### Fixed

- **Station priority** — IR pylons engage all allowed inbound (IR + ARH/SARH) while IR ammo remains; radar pylons only after IR exhausted, ARH/SARH only. Per-threat nose FOV unchanged (IR 80° / radar 50°).

### Build

- Display version: `0.5.42QV`
- BepInPlugin semver: `0.5.42`

## [0.5.41] — 2026-07-13

### Added

- **`EngageIrThreats`** (section `4. Signal & Tracking`, default `false`) — when enabled, Hard-Kill engages IR inbound on RWR with **80°** nose FOV; ARH/SARH remain **50°**. Mixed salvo picks IR interceptors for IR threats and radar stations for ARH/SARH.

### Build

- Display version: `0.5.41QV`
- BepInPlugin semver: `0.5.41`

## [0.5.40] — 2026-07-12

### Changed

- **POTENTIAL HIT cue** — shown only when RWR threat is in weapon reach, maneuver time remains, and aim is inside an expanded envelope margin (not merely “ASE visible but not SHOOT”). Otherwise cue hidden.

### Fixed

- **RWR-only anti-cheat gate** — HardKill threat collect, ASE sample, SHOOT/salvo gates require missiles on local `MissileWarning` lists (`knownMissiles` + RWR `unknownMissiles` from `LockedByMissile`). No salvo-scratch union outside RWR; no HUD/fire without RWR picture.

### Build

- Display version: `0.5.40QV`
- BepInPlugin semver: `0.5.40`

## [0.5.39] — 2026-07-12

### Fixed

- **Third-person zoom blocked salvo** — outside cockpit, SHOOT gate uses world velocity/nose aim instead of stale FlightHud gun-cross (canvas off in chase/relative; zoom misaligns screen envelopes). Cockpit + optional `RequireAseScreenShoot` unchanged.

### Build

- Display version: `0.5.39QV`
- BepInPlugin semver: `0.5.39`

## [0.5.38] — 2026-07-12

### Fixed

- **Manual hotkey dead (RightShift + /)** — `NocsHotKey.WasPressed` no longer blocks modifier combos when `Input.inputString` contains the same character on the key-down frame (Unity quirk). Chat guard kept only for bare keys without modifier.

### Build

- Display version: `0.5.38QV`
- BepInPlugin semver: `0.5.38`

## [0.5.37] — 2026-07-12

### Fixed

- **Ghost auto-salvo with AutoEngage off** — session continuation no longer calls `RunSalvo` on `shootOpen` alone unless the session was started by hotkey (`ManualEngagement`) or AutoEngage is still enabled. Auto path uses strict ASE sample (no salvo-only world-aim bypass).
- **Session start without target lock** — `TryBeginSession` fail-closed: WM track must succeed via `TryEstablishCurrentThreatTrack` before session activates; snapshot restored on failure.
- **Hotkey misfire in chat** — `NocsHotKey.WasPressed` ignores frames with `Input.inputString` (typed `/` etc.).

### Build

- Display version: `0.5.37QV`
- BepInPlugin semver: `0.5.37`

## [0.5.36] — 2026-07-12

### Fixed

- **Intermittent skipped incoming missile** — fire-control scratch now unions ASE preview with salvo MWS collect so threats visible to salvo queue also drive SHOOT geometry. Salvo raw collect keeps nearest eight in FOV (not first eight in scan order). Active session continues firing when preview drops a threat mid-salvo; salvo-only threats fall back to world-aim SHOOT when ASE sample is empty.

### Build

- Display version: `0.5.36QV`
- BepInPlugin semver: `0.5.36`

## [0.5.35] — 2026-07-12

### Fixed

- **Stability hardening** — fail-closed try/catch isolation on host tick, HUD root (TrueNotch vs HardKill separated), HardKill/ASE/launch/track/restore/binder, TrueNotch apply, Warning TTI, unit lookup, and NocsGuard local-player checks. Rate-limited `NocsDiagLog` errors; game API failures (`LaunchMount`, `CmdUpdateTrackingInfo`, WM track) no longer abort the frame.

### Build

- Display version: `0.5.35QV`
- BepInPlugin semver: `0.5.35`

## [0.5.34] — 2026-07-12

### Fixed

- **Close-range / swarm targets skipped for track** — salvo collect no longer drops threats inside `MinLaunchRangeMeters` (launch gate unchanged); preview ASE includes close inbound. WM track applied via `RepointTargets` before each launch attempt and each tick while session active (`MaintainActiveEngagement`).

### Build

- Display version: `0.5.34QV`
- BepInPlugin semver: `0.5.34`

## [0.5.33] — 2026-07-12

### Fixed

- **Track not clearing after launch** — restore pre-salvo target list immediately on `FinishSession` again; removed deferred restore / post-session engaged track hold. Launch-time capture via `EnsureThreatTracked` retained.

### Build

- Display version: `0.5.33QV`
- BepInPlugin semver: `0.5.33`

## [0.5.32] — 2026-07-12

### Fixed

- **Outgoing launch without incoming track** — `RepointTargets` now selects the APS station (`SetActiveStation`) before `AddTargetList`; shared `EnsureThreatTracked` verifies `CheckIsTarget` before `LaunchMount`. IR salvo path sends HQ tracking like radar. Deferred target restore while live engaged inbound threats remain; WM track maintained until threat dies.

### Build

- Display version: `0.5.32QV`
- BepInPlugin semver: `0.5.32`

## [0.5.31] — 2026-07-12

### Removed

- **Angular launch latency** — deferred `LaunchMount` by nose angle fully removed; launches fire immediately again.

### Build

- Display version: `0.5.31QV`
- BepInPlugin semver: `0.5.31`

## [0.5.30] — 2026-07-12

### Fixed

- **ASE status cue NRE spam** (`Player.log`) — guard destroyed `Text` / `RectTransform` before `color` / parent writes; try/catch fail-closed on cue apply.

### Removed (superseded by 0.5.31)

- ~~Angular launch latency~~ — removed in 0.5.31.

### Build

- Display version: `0.5.30QV`
- BepInPlugin semver: `0.5.30`

## [0.5.29] — 2026-07-12

### Fixed

- **Multiplayer Hard-Kill / TrueNotch dead after spawn** — `NocsGuard` no longer requires `GameManager.GetLocalAircraft` alone; MP spawn race uses `aircraft.Player.IsLocalPlayer` (same as vanilla `CheckIfLocalSim`). Lazy `NocsAircraftBinder.EnsureBound` recovers if `CombatHUD.SetAircraft` ran before `Player.OnStartLocalPlayer`. Weapon mutate still requires `HasAuthority` / `IsServer` (no spectator weapon writes).

### Build

- Display version: `0.5.29QV`
- BepInPlugin semver: `0.5.29`

## [0.5.28] — 2026-07-12

### Changed

- README badges (shields.io), consolidated alert blocks, Configuration Manager hotkey documented as **F1**.

### Build

- Display version: `0.5.28QV`
- BepInPlugin semver: `0.5.28`

## [0.5.27] — 2026-07-12

### Fixed

- **Session stall after failed `LaunchMount`** — ammo/callback verification + 2 s `PendingOwnLaunches` watchdog; rollback on silent launch failure.
- **Local-player fail-closed** — `NocsGuard` no longer falls back to `CombatHUD.aircraft` (spectator/observed craft).
- **Aircraft rebind** — hardware pair lock cleared on `ResetSession`; ledger reset retained.
- **`MinLaunchRangeMeters` / `AutoEngage`** — wired into collect + soft launch / auto SHOOT path (were dead config keys).
- **Harmony isolation** — `[HarmonyFinalizer]` on FlightHud / CombatHUD / ThreatItem patches.

### Changed

- `CmdUpdateTrackingInfo` debounced per threat ID per frame.
- Config cache clamps completed (`MaxTimingTickDt`, notch scales, Doppler bias, preview range factor).

### Performance

- Frame-cached hardware salvo lock, turn-G, empty weapon catalog, marker screen centers; Preview/Engage cache keys include defensive station; TrueNotch `ApplyLive` frame-deduped.

### Build

- Display version: `0.5.27QV`
- BepInPlugin semver: `0.5.27`

## [0.5.26] — 2026-07-12

### Fixed

- **Intermittent no-fire after SHOOT** — fire-control audit:
  - One shared `IsShootWindowOpen` for HUD cue and salvo trigger (no drift between “shows SHOOT” and “allows hotkey”).
  - Soft committed launch again (valid threat + station reach only) — removed double ASE / `MinLaunchRange` / arm-dead re-checks that silently skipped Ready stations.
  - Geometry gate always uses the best defensive station, not the last-fired IR mount.
  - SHOOT requires at least one unengaged salvo threat (no false cue on already-intercepted missiles).
  - When gun cross is on-screen but outside envelopes, world-aim cannot override (SHOOT stays honest).

### Build

- Display version: `0.5.26QV`
- BepInPlugin semver: `0.5.26`

## [0.5.25] — 2026-07-12

### Fixed

- **Early salvo before SHOOT geometry** — hotkey and committed launch now require the same intercept shoot window as the ASE cue (gun cross inside all envelopes, or velocity/nose world-aim fallback). No longer bypasses fire control when `RequireAseScreenShoot` is off. HUD cue visibility remains independent.

### Build

- Display version: `0.5.25QV`
- BepInPlugin semver: `0.5.25`

## [0.5.24] — 2026-07-12

### Fixed

- **Partial salvo stuck at 1/2** — when only one interceptor was fired and all engaged incoming missiles are gone, the pair counter now resets after the full 1.8 s cooldown (`_lastSalvoTime` stamped on every launch). Full two-round pairs still hard-lock until cooldown expires.

### Build

- Display version: `0.5.24QV`
- BepInPlugin semver: `0.5.24`

## [0.5.23] — 2026-07-12

### Fixed

- **Double intercept on one target** — engagement ledger now persists for live threats (cleared only on aircraft reset / dispose, or when the missile dies). Pair cooldown no longer wipes allocation memory; targets are reserved before launch with rollback on failed fire. Queue dedup uses `persistentID`; hotkey no longer runs two salvo steps in the same tick.

### Build

- Display version: `0.5.23QV`
- BepInPlugin semver: `0.5.23`

## [0.5.22] — 2026-07-12

### Fixed

- **Post-salvo re-fire blocked forever** — `ThreatEngagementLedger` now clears when the 1.8 s pair cooldown expires and when a session finishes, so the next pair or new engagement can allocate targets again.

### Build

- Display version: `0.5.22QV`
- BepInPlugin semver: `0.5.22`

## [0.5.21] — 2026-07-12

### Fixed

- **Hard freeze on large missile swarms** — failed launches no longer wrap the threat queue inside a single frame (`AdvanceQueue` → `FindNextEngageIndex` loop). Salvo step now uses forward-only advance + hard pass cap (`MaxQueue`).

### Build

- Display version: `0.5.21QV`
- BepInPlugin semver: `0.5.21`

## [0.5.20] — 2026-07-12

### Added

- **Hard MP APS balance (immutable)** — private const gates, not exposed in Configuration Manager:
  - Nose FOV cone **50°** (`FovConeAngleDeg`) — threats outside ±25° of aircraft forward are dropped **first**, before seeker/CPA/ASE, and never pollute priority.
  - Threat type — **ARH/SARH only** (IR/optical/unknown seekers rejected in preview, engage, and salvo collect).
  - Salvo window — max **2** launches per pair (`MaxSimultaneousTargets`), then hard **1.8 s** lock via `Time.time` (`SalvoCooldownSec`); lock survives session reset and is not overwritten while active.

### Fixed

- **FOV blinding** — out-of-cone missiles no longer enter scratch/queues or block frontal intercepts.
- **Salvo cooldown** — pair lock uses `_lastSalvoTime = Time.time` at the 2nd launch; clears only after full 1.8 s.
- **Deadlock after salvos** — `ThreatEngagementLedger.PruneInvalid` drops dead/disabled missiles; queue prunes invalid threats; hardware lock prevents premature `FinishSession`.
- **Interceptor ammo priority** — IR stations always expended before radar stations.
- **Target allocation** — each launch marks `IsIntercepted`; pair distributes across distinct unengaged threats.
- **ASE HUD FPS** — 2 px hysteresis on ring `sizeDelta` / position / arc layout to avoid Canvas rebuild spam.

### Changed

- CM renames (English): `MaxLaunchRangeMeters`, `MinLaunchRangeMeters`, `MissDistanceToleranceMeters`, `ManualLaunchAimTolerance`.

### Build

- Display version: `0.5.20QV`
- BepInPlugin semver: `0.5.20`

## [0.5.19] — 2026-07-12

### Removed

- **`EngageIrThreats`** config entry — Hard-Kill remains radar-only (ARH/SARH); IR threats are not engageable via CM.

### Build

- Display version: `0.5.19QV`
- BepInPlugin semver: `0.5.19`

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
