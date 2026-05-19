# Development Guide

## Current Goal

Before building the browser map, keep the foundation small, reproducible, and easy to inspect.

The current test plugin runs only in the flight scene. KSP creates `KspWebMapAddon`, the addon starts a `ServiceRegistry`, and the registry starts a temporary development window. Clicking the button posts a KSP screen message and writes to the KSP log.

## Build Inputs

The C# project references KSP and Unity assemblies from your KSP install:

```text
KSP_x64_Data/Managed/Assembly-CSharp.dll
KSP_x64_Data/Managed/UnityEngine.CoreModule.dll
KSP_x64_Data/Managed/UnityEngine.IMGUIModule.dll
```

The build script accepts the KSP install path:

```powershell
.\scripts\build.ps1 -Clean -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
```

You can also set an environment variable for the current PowerShell session:

```powershell
$env:KSP_ROOT = "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
.\scripts\build.ps1
```

If a .NET SDK is not installed, the build script falls back to the Windows .NET Framework compiler at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.

Build output is staged under:

```text
artifacts/bin/Release/KspWebMap.dll
artifacts/package/GameData/KspWebMap
```

## Install

KSP loads mods from `GameData`. The final installed layout should be:

```text
Kerbal Space Program/
  GameData/
    KspWebMap/
      Plugins/
        KspWebMap.dll
      Web/
        index.html
```

If this repository is outside the KSP install, install the staged package into the KSP install after building.

The helper script does that copy for you:

```powershell
.\scripts\install.ps1 -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
```

Preview an install:

```powershell
.\scripts\install.ps1 -DryRun -Verbose -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
```

## Verification Steps

1. Build the project.
2. Install `GameData/KspWebMap` into KSP.
3. Start KSP.
4. Enter any flight scene.
5. Confirm a draggable window titled `KSP Web Map - Dev Test` appears.
6. Click `Say Hello World`.
7. Confirm the on-screen message appears.

Optional log verification:

```text
[KspWebMap] Flight addon loaded.
[KspWebMap] Hello World button clicked. Count: 1
```

KSP writes logs to `KSP.log` in the KSP install folder and to `Player.log` under the Unity player log location.

## Design Notes

- `KspWebMapAddon` is the KSP entry point and lifecycle host.
- `ServiceRegistry` owns service startup and shutdown order.
- `DevWindowService` owns only the temporary in-game verification UI.
- Future web server, telemetry, and map-rendering code should live in separate classes so this test UI can be removed cleanly.
- The browser map should be data-driven instead of depending on KSP's map view being open.
- Unity and KSP APIs must be accessed on the main thread.
