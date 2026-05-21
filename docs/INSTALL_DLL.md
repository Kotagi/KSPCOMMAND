# Install the KspWebMap DLL (and package)

Use this guide when you have **new C# telemetry code** (for example Phase 11 frame truth) and need the game to load an updated `KspWebMap.dll`. Web-only changes do not require quitting KSP; see [Web-only update](#web-only-update-no-quit) below.

Full build troubleshooting: [BUILD_AND_INSTALL.md](BUILD_AND_INSTALL.md).

## Before you start

| Requirement | Why |
|-------------|-----|
| **KSP fully quit** | Windows locks `GameData\KspWebMap\Plugins\KspWebMap.dll` while the game runs. |
| Valid **KSP root** | Folder that contains `KSP_x64.exe` and `GameData\` (not `GameData` itself). |
| Repo built once | `scripts\build.ps1` produces `artifacts\package\GameData\KspWebMap\`. |

**Example KSP root:**

```text
C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program
```

## Quick install (recommended)

From PowerShell, repository root (`MyMods`):

```powershell
# 1) Point at your KSP install
$env:KSP_ROOT = "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"

# 2) Confirm KSP is not running (no KSP_x64.exe in Task Manager)

# 3) Build plugin + stage package (DLL + web assets)
cd "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\MyMods"
.\scripts\build.ps1

# 4) Copy staged mod into KSP
.\scripts\install.ps1
```

**Success:**

```text
Build complete.
DLL output: artifacts\bin\Release\KspWebMap.dll
Installed KspWebMap to: ...\Kerbal Space Program\GameData\KspWebMap
```

## DLL-only copy (manual)

If you already ran `build.ps1` and only need to replace the plugin:

```powershell
$ksp = "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
$dll = "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\MyMods\artifacts\bin\Release\KspWebMap.dll"

# KSP must be quit
Copy-Item $dll "$ksp\GameData\KspWebMap\Plugins\KspWebMap.dll" -Force
```

Prefer `.\scripts\install.ps1` when `Web\assets` or other mod files also changed (Phase 11 updates both DLL and HUD).

## Verify the new DLL

```powershell
$dll = "$env:KSP_ROOT\GameData\KspWebMap\Plugins\KspWebMap.dll"
Test-Path $dll
(Get-Item $dll).LastWriteTime
```

After starting KSP and loading **flight**:

1. `http://127.0.0.1:8750/api/health` → `"status":"ok"`
2. `http://127.0.0.1:8750/api/telemetry` → `schemaVersion` 8, fields such as `iconTrailSample0ResidualMeters`, `positionValidation`
3. Hard refresh map: `http://127.0.0.1:8750/?v=22` (`Ctrl+F5`)

In-flight checks (Phase 11):

```powershell
.\scripts\verify-telemetry.ps1
.\scripts\verify-body-positions.ps1
```

See [BODY_ORBIT_VNV.md](BODY_ORBIT_VNV.md).

## Web-only update (no quit)

If **only** `web/` changed and the DLL on disk is already current:

```powershell
cd web
npm run build
Copy-Item "dist\assets\*" "$env:KSP_ROOT\GameData\KspWebMap\Web\assets" -Force
```

Hard refresh the browser (`?v=22`). KSP may stay open.

## Troubleshooting

| Problem | Action |
|---------|--------|
| `KSP root was not provided` | Set `$env:KSP_ROOT` or `.\scripts\build.ps1 -KspRoot "..."` |
| Access denied copying DLL | Quit KSP; end stray `KSP_x64.exe` in Task Manager |
| `Could not find staged package` | Run `.\scripts\build.ps1` first |
| Map unchanged after DLL install | Hard refresh; confirm DLL `LastWriteTime` is today |
| Old ~136 Gm HUD alarm | New DLL required; primary banner should show same-UT residuals only |

## Related

- [BUILD_AND_INSTALL.md](BUILD_AND_INSTALL.md) — `KSP_ROOT`, clean rebuild, uninstall
- [OPERATIONS.md](OPERATIONS.md) — smoke test and operations
