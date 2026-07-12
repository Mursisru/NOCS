# NOCS (NO Countermeasures Supporter)

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![BepInEx 5](https://img.shields.io/badge/Loader-BepInEx%205-orange)](https://docs.bepinex.dev/)
[![Version](https://img.shields.io/badge/Version-0.5.27QV-green)](https://github.com/Mursisru/NOCS/releases/tag/v0.5.27)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)
[![.NET Framework 4.8](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)](https://dotnet.microsoft.com/download/dotnet-framework/net48)

---

## Critical warnings

> [!IMPORTANT]
> **BepInEx 5 (x64) required** — install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) before this mod.

> [!WARNING]
> **Hard-Kill APS changes weapon targeting** — pressing the engagement hotkey (or `AutoEngage`) captures your track list and launches defensive interceptors. Targets are restored after the salvo. Code-fixed MP balance: nose FOV **50°**, **ARH/SARH** inbound only, max **2** launches per pair + **1.8 s** cooldown, one interceptor per live incoming missile.

> [!TIP]
> **Configuration Manager recommended** — open in-game with **F1** for `com.at747.nocs.bepinex.cfg`. If CM fails after a game update, set `HideManagerGameObject = true` in `BepInEx\config\BepInEx.cfg`. Delete `BepInEx\cache\harmony_interop_cache.dat` if patches behave oddly.

BepInEx 5 plugin for the flight sim **Nuclear Option** with two independent HUD systems: passive TrueNotch radar-jam width and active Hard-Kill APS with swarm ASE fire-control geometry.

**Plugin GUID:** `com.at747.nocs.bepinex` · **Display version:** `0.5.27QV` · **BepInPlugin semver:** `0.5.27`

---



## Table of contents

- [Critical warnings](#critical-warnings)
- [Features](#features)
- [Requirements](#requirements)
- [Player installation](#player-installation)
- [Controls & keybinds](#controls--keybinds)
- [Configuration (BepInEx Configuration Manager)](#configuration-bepinex-configuration-manager)
- [Project layout](#project-layout)
- [Changelog](#changelog)
- [Licence](#licence)

---



## Features

- **TrueNotch HUD (passive):** Resizes the radar notch indicator width from real `RadarParams.GetSignalStrength` jam boundaries. Notch center and rotation remain vanilla.
- **Hard-Kill APS (active):** Swarm ASE intercept geometry for all incoming radar-guided threats in the nose FOV. Optional intercept ring (`ShowAseInterceptRing`) and status cue under the weapon HUD hint (`RenderRadialText`).
- **SHOOT / POTENTIAL HIT cues:** Shared fire-control math for HUD and hotkey — not tied to whether the ring or cue is rendered.
  - **SHOOT** — velocity vector (gun cross) inside **all** per-threat guarantee envelopes, weapon reach OK, at least one unengaged salvo target.
  - **POTENTIAL HIT** — threat visible in ASE preview, but shoot window not open yet (aim outside envelopes, out of range, or target already engaged).
- **Warning TTI (MWS labels):** Smoothed time-to-impact suffix on threat list rows (e.g. `Missile [ARH] 4.2 km [8.6s]`), with `[IMPACT]` at zero.
- **Salvo engine:** IR interceptors expended before radar stations; launch budget capped by ammo and pair window; track list restored after session; engagement ledger prevents duplicate shots at the same live threat.

---



## Requirements

- **Nuclear Option** ([Steam](https://store.steampowered.com/app/2168680/Nuclear_Option/)).
- **BepInEx 5** (x64) in the game root (`BepInEx\core\` must exist for **build** reference paths).
- **[Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager)** (recommended) — in-game UI for plugin settings.

---



## Player installation

1. Install [BepInEx 5](https://github.com/bepinex/bepinex) for Nuclear Option.
2. Install [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (recommended).
3. Copy into:
  ```text
   Nuclear Option\BepInEx\plugins\NOCS\
  ```
  - `NOCS.dll`
4. Settings are stored in `BepInEx\config\com.at747.nocs.bepinex.cfg` (auto-created on first run). Edit in-game via **Configuration Manager**, or edit the `.cfg` file while the game is closed.
5. Do **not** rename the plugin folder or install duplicate copies under different names.

---



## Controls & keybinds

Active while Hard-Kill APS is enabled and threats are present. **US English keyboard layout.** Keybinds are editable in Configuration Manager.


| Keybind               | Default `KeyCode`      | Action                |
| --------------------- | ---------------------- | --------------------- |
| **Right Shift** + `/` | `RightShift` + `Slash` | Start / extend Hard-Kill salvo |


**Salvo behaviour:** up to **2** interceptors per pair (hard-coded), then **1.8 s** cooldown. Hotkey and `AutoEngage` both require the same **SHOOT** geometry gate as the HUD cue. Previous track list and active station are restored when the session finishes.

---



## Configuration (BepInEx Configuration Manager)

All settings are exposed through **BepInEx.Configuration** (`Config.Bind` in `Config/NocsBepInConfig.cs`). Use [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) in-game (**F1**), or edit:

```text
BepInEx\config\com.at747.nocs.bepinex.cfg
```

CM groups: **TrueNotchHUD**, **HardKillAPS**, **1. HUD Visuals**, **2. Engagement Envelope**, **3. Fire Control & Geometry**, **4. Signal & Tracking**, **WarningTTI**.



### TrueNotchHUD


| Key                | Default | Description                           |
| ------------------ | ------- | ------------------------------------- |
| `Enabled`          | `true`  | Master switch for notch width overlay |
| `SmoothTime`       | `0.05`  | Width smoothing time constant         |
| `ClampWidth`       | `false` | Clamp width to min/max scale          |
| `MinWidthScale`    | `1.0`   | Min scale when clamp enabled          |
| `MaxWidthScale`    | `1.0`   | Max scale when clamp enabled          |
| `NotchDopplerBias` | `0.0`   | Optional Doppler bias offset          |




### HardKillAPS


| Key                | Default      | Description |
| ------------------ | ------------ | ----------- |
| `Enabled`          | `true`       | Master switch for Hard-Kill APS |
| `AutoEngage`       | `false`      | Automatic salvo when **SHOOT** window is open (same gate as hotkey) |
| `HotKeyModifier`   | `RightShift` | Engagement modifier key |
| `HotKey`           | `Slash`      | Engagement fire key (`/` on US layout) |
| `WeaponPriority`   | `IR_First`   | Legacy enum — runtime always expends **IR before radar** |
| `WeaponFilterMode` | `AntiMissileOnly` | Station filter mode |

**1. HUD Visuals**

| Key                    | Default | Description |
| ---------------------- | ------- | ----------- |
| `ShowAseInterceptRing` | `false` | Optional ASE ring + arc SHOOT / POTENTIAL HIT labels |
| `RenderRadialText`     | `true`  | Status cue under weapon HUD hint |
| `AseVisualScale`       | `1.0`   | Ring visual scale |

**2. Engagement Envelope**

| Key                         | Default   | Description |
| --------------------------- | --------- | ----------- |
| `MaxLaunchRangeMeters`      | `15000`   | Absolute max intercept range (m) |
| `MinLaunchRangeMeters`      | `150`     | Absolute min engagement dead-zone (m) |
| `AseMaxRangeFactor`         | `1.0`     | Dynamic engage window multiplier |
| `AsePreviewRangeFactor`     | `1.0`     | ASE preview range multiplier |
| `AsePreviewAppearDistanceM` | `5000`    | Slant range (m) where ASE preview may first appear |
| `MaxManeuverWindow`         | `4.5`     | Max maneuver time for envelope (s) |
| `AseInterceptConfidence`    | `0.99`    | Intercept confidence target |
| `AseSensitivityBias`        | `1.0`     | Global envelope radius multiplier |
| `DefaultMaxTurnG`           | `15.0`    | Fallback g-limit when prefab data missing |
| `MinArmDistSlack`           | `1.0`     | Arm-distance slack multiplier |

**3. Fire Control & Geometry**

| Key                       | Default | Description |
| ------------------------- | ------- | ----------- |
| `ManualLaunchAimTolerance`| `0`     | Extra aim tolerance (deg). `0` = strict ASE; `180` = open gate |
| `RequireAseScreenShoot`   | `false` | When `true`, SHOOT requires on-screen gun cross (no velocity/nose world fallback) |
| `LaunchCooldown`          | `0.35`  | Inter-shot station wait inside a pair (s). Pair hardware lock fixed at **1.8 s** in code |
| `MissDistanceToleranceMeters` | `50` | Max CPA (m) for threat inclusion |
| `MaxTimingTickDt`         | `0`     | Optional salvo timing tick cap (`0` = off) |

**4. Signal & Tracking**

| Key                    | Default | Description |
| ---------------------- | ------- | ----------- |
| `ClosureMinThreshold`  | `0.1`   | Minimum closure speed floor (m/s) for TTI and range gates |
| `TtiSmoothingFactor`   | `0.08`  | TTI low-pass alpha (also under WarningTTI) |




### WarningTTI


| Key                  | Default | Description                                    |
| -------------------- | ------- | ---------------------------------------------- |
| `Enabled`            | `true`  | Append TTI suffix to MWS threat labels         |
| `TtiSmoothingFactor` | `0.08`  | Low-pass blend toward raw physics TTI (0.01–1) |


---



## Project layout

```text
NOCS/
├── NOCS.csproj
├── NOCS.sln
├── NocsPlugin.cs              # BepInPlugin entry, Config.Bind
├── NocsHost.cs                # DDOL host, mission-scene bootstrap
├── AppVersion.cs
├── CHANGELOG.md
├── Config/                    # BepInEx config bindings + cache
├── Core/                      # Bootstrap, guards, FlightHud patch
├── TrueNotch/                 # Passive notch HUD + Warning TTI
├── HardKill/                  # ASE circle, MWS filter, swarm geometry, weapon FSM
└── Util/                      # HUD placement, screen scale, hotkey helpers
```

---



## Changelog

See [CHANGELOG.md](CHANGELOG.md).

---



## Licence

MIT License (c) CopyRight ©Mursisru (2026) — see [LICENSE](LICENSE).
