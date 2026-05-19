# KSP Web Map

KSP Web Map is a Kerbal Space Program 1.12.3 mod intended to expose live map and vessel data to a local browser UI.

The current milestone is Phase 9: schema v6 ephemeris telemetry with time-sampled patch placement, time-keyed solar route overlays, approximate solar-frame conic segments, active-patch vessel path samples, and a read-only ephemeris UT scrubber on top of the Phase 8 interactive solar map. KSP loads the plugin, captures read-only flight data on the main thread, and serves a self-contained browser UI from the local mod folder. WebGL remains Phase 10+.

## Project Layout

```text
artifacts/              Generated build and staged package output
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
