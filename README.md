# NOCS (NO Countermeasures Supporter)

[Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[BepInEx 5](https://docs.bepinex.dev/)
[Version](https://github.com/Mursisru/NOCS)
[License: MIT](LICENSE)

---

## Critical warnings

> [!IMPORTANT]
> **BepInEx 5 (x64) required** — install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) before this mod.

> [!WARNING]
> **Hard-Kill APS changes weapon targeting** — pressing the engagement hotkey captures your current track list and launches one defensive missile per incoming threat. Targets are restored after the salvo completes.

> [!TIP]
> **Configuration Manager recommended** — in-game UI for `com.at747.nocs.bepinex.cfg`. After game updates, delete `BepInEx\cache\harmony_interop_cache.dat` if patches behave oddly.

BepInEx 5 plugin for the flight sim **Nuclear Option** with two independent HUD systems: passive TrueNotch radar-jam width and active Hard-Kill APS with a single swarm ASE intercept circle.

**Plugin GUID:** `com.at747.nocs.bepinex`

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
- **Hard-Kill APS (active):** One **swarm ASE intercept circle** for all incoming guided threats — center is not pinned to a single missile marker; radius expands to encompass every threat envelope. `SHOOT` / `POSSIBLE HIT` cues require the velocity vector inside **all** per-threat guarantee zones.
- **Warning TTI (MWS labels):** Smoothed time-to-impact suffix on threat list rows (e.g. `Missile [ARH] 4.2 km [8.6s]`), with `[IMPACT]` at zero.
- **Salvo engine:** Launch budget capped by total pylon ammo; iterates all ready stations; restores the previous track list after the salvo.

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
| **Right Shift** + `/` | `RightShift` + `Slash` | Start Hard-Kill salvo |


**Salvo behaviour:** one outgoing defensive missile per queued incoming threat, capped by available pylon ammo. Previous track list and active station are restored when the salvo completes.

---



## Configuration (BepInEx Configuration Manager)

All settings are exposed through **BepInEx.Configuration** (`Config.Bind` in `Config/NocsBepInConfig.cs`). Use [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) in-game, or edit:

```text
BepInEx\config\com.at747.nocs.bepinex.cfg
```



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


| Key                      | Default           | Description                                                     |
| ------------------------ | ----------------- | --------------------------------------------------------------- |
| `Enabled`                | `true`            | Master switch for Hard-Kill APS                                 |
| `AseCircleEnabled`       | `true`            | Show the swarm ASE intercept circle and cue labels              |
| `AutoEngage`             | `false`           | Automatic salvo without hotkey                                  |
| `HotKeyModifier`         | `RightShift`      | Engagement modifier key                                         |
| `HotKey`                 | `Slash`           | Engagement fire key                                             |
| `WeaponPriority`         | `IR_First`        | `IR_First` or `ARH_First`                                       |
| `WeaponFilterMode`       | `AntiMissileOnly` | Station filter mode                                             |
| `SafetyDistanceGate`     | `true`            | Require velocity vector inside all threat envelopes for `SHOOT` |
| `LaunchCooldown`         | `0.05`            | Minimum delay between salvo launches (seconds)                  |
| `MaxCpaMeters`           | `50.0`            | Max CPA distance for threat inclusion                           |
| `AsePreviewRangeFactor`  | `1.0`             | ASE preview range multiplier                                    |
| `AseMaxRangeFactor`      | `1.0`             | Engage window range multiplier                                  |
| `MaxManeuverWindow`      | `4.5`             | Max maneuver time for envelope (seconds)                        |
| `AseInterceptConfidence` | `0.99`            | Intercept confidence target (0.5–0.999)                         |
| `AseSensitivityBias`     | `1.0`             | Global radius multiplier                                        |
| `DefaultMaxTurnG`        | `15.0`            | Fallback g-limit when prefab data missing                       |




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