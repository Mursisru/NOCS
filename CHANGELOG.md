# Changelog

All notable changes to this project are documented in this file.

Format follows [Keep a Changelog](https://keepachangelog.com/). Versions use [Semantic Versioning](https://semver.org/).

## [0.0.0] — 2026-07-11

### Added

- Initial **BepInEx 5** port of NO Countermeasures Supporter (NOCS).
- **TrueNotch HUD** — jam-boundary notch width overlay.
- **Hard-Kill APS** — swarm ASE bounding circle, salvo hotkey, track restore.
- **Warning TTI** — smoothed MWS time-to-impact labels.
- **BepInEx Configuration Manager** bindings (`com.at747.nocs.bepinex.cfg`) for all former `mod_config.ini` settings.
- Harmony patches: `FlightHud.Awake`, `ThreatItem.AnimateItem`, `CombatHUD.SetAircraft`.

### Build

- Display version: `0.0.0P`
- BepInPlugin semver: `0.0.0`
