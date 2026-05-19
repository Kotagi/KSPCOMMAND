# KSP Web Map

KSP Web Map is a Kerbal Space Program 1.12.3 mod intended to expose live map and vessel data to a local browser UI.

Phase 10 is complete: schema v7 telemetry (v6-compatible) with a polished Vite + React Three Fiber 3D solar map (unified MapHud controls, camera framing, selection, labels, atmosphere, apsis markers, SOI LOD, quality presets, line decimation, vessel lerp) plus a 2D Canvas fallback. KSP captures read-only flight data on the main thread and serves the dashboard from the local mod folder.

## Project Layout

```text
artifacts/              Generated build and staged package output
web/                    Vite + TypeScript + React Three Fiber solar map (Phase 10)
GameData/
  KspWebMap/            Source package assets copied into staged builds
src/
  KspWebMap/
    Configuration/      Future mod configuration
    Core/               Addon lifecycle and service ownership
    DevTools/           Temporary in-game smoke-test UI
    Services/           Future long-running services
    Telemetry/          Future KSP data snapshots
docs/                   Architecture, operations, and roadmap docs
scripts/                Build, install, and uninstall helpers
```

## Requirements

- Kerbal Space Program 1.12.3
- Cursor or another editor
- PowerShell
- One C# build path:
  - .NET SDK with .NET Framework 4.7.2 targeting support, or
  - Windows .NET Framework compiler fallback at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
- Node.js 20+ and npm (for building `web/`; `build.ps1` runs this automatically)

## Quick Start

1. Use this KSP install folder:

   ```text
   C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program
   ```

2. Build the plugin:

   ```powershell
   .\scripts\build.ps1 -Clean -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
   ```

3. Install the mod into KSP:

   ```powershell
   .\scripts\install.ps1 -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
   ```

4. Launch KSP.
5. Start or load a flight.
6. Look for the **KSP Web Map - Dev Test** window.
7. Press **Say Hello World**.

Expected result: KSP displays `Hello World from KSP Web Map!` near the top of the screen.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Operations](docs/OPERATIONS.md)
- [Development](docs/DEVELOPMENT.md)
- [Roadmap](docs/ROADMAP.md)
