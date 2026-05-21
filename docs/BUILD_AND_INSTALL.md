# Build and install KspWebMap

This guide explains how to compile the C# plugin and install it into Kerbal Space Program. It fixes the common **“KSP root was not provided / C# build skipped”** problem by setting `KSP_ROOT` (or passing `-KspRoot`) before running `build.ps1`.

**Short path (quit KSP → new DLL):** [INSTALL_DLL.md](INSTALL_DLL.md).

## What you need

| Item | Purpose |
|------|---------|
| KSP install folder | Must contain `KSP_x64.exe`, `GameData\`, `KSP_x64_Data\` |
| This repo (`MyMods`) | Source and `scripts\` |
| .NET SDK *or* Windows .NET Framework | `build.ps1` uses SDK if present, else `csc.exe` |
| Node.js + npm | Only for rebuilding the web UI (included in full `build.ps1`) |

**Example KSP root (adjust to your PC):**

```text
C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program
```

That path is the folder that **contains** `GameData`, not `GameData` itself.

## Why `KSP_ROOT` matters

`scripts\build.ps1` and `scripts\install.ps1` must read KSP’s game assemblies to compile against:

```text
KSP_x64_Data\Managed\Assembly-CSharp.dll
KSP_x64_Data\Managed\UnityEngine.CoreModule.dll
KSP_x64_Data\Managed\UnityEngine.IMGUIModule.dll
```

Without a valid root, the script stops with:

```text
KSP root was not provided. Set KSP_ROOT or pass -KspRoot "C:\Path\To\KSP".
```

Agents and CI shells often run **without** `KSP_ROOT` set, so the C# step is skipped even though web assets may still build.

## Set `KSP_ROOT` (pick one)

### Option A — One PowerShell session (quick test)

```powershell
$env:KSP_ROOT = "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
Test-Path "$env:KSP_ROOT\KSP_x64.exe"   # should be True
```

### Option B — Every new terminal (user environment variable)

1. Windows **Settings → System → About → Advanced system settings → Environment Variables**.
2. Under **User variables**, **New**:
   - Name: `KSP_ROOT`
   - Value: `C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program`
3. OK, then **open a new PowerShell window** (existing windows keep old env).

Verify:

```powershell
echo $env:KSP_ROOT
Test-Path "$env:KSP_ROOT\KSP_x64.exe"
```

### Option C — Always pass `-KspRoot` (no env var)

```powershell
$ksp = "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
.\scripts\build.ps1 -KspRoot $ksp
.\scripts\install.ps1 -KspRoot $ksp
```

Use the same path in all three places if you mix options.

## Full build (DLL + web package)

From the **repository root** (`MyMods`):

```powershell
cd "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\MyMods"

$env:KSP_ROOT = "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"

.\scripts\build.ps1 -Clean
```

**Success looks like:**

```text
Build complete.
DLL output: artifacts\bin\Release\KspWebMap.dll
Package output: artifacts\package\GameData\KspWebMap
```

**Staged install tree:**

```text
artifacts\package\GameData\KspWebMap\
  Plugins\KspWebMap.dll
  Web\...
  (other mod files from GameData\KspWebMap in repo)
```

### Clean rebuild

`-Clean` deletes `artifacts\` first. Use after C# changes or when the package looks stale.

## Install into KSP

### 1. Quit KSP

Kerbal locks `KspWebMap.dll` while running. Install or overwrite the DLL only when KSP is **fully closed**.

### 2. Install staged package

```powershell
$env:KSP_ROOT = "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
.\scripts\install.ps1
```

Or with explicit path:

```powershell
.\scripts\install.ps1 -KspRoot "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"
```

**Success:**

```text
Installed KspWebMap to: ...\Kerbal Space Program\GameData\KspWebMap
```

The installer only replaces `GameData\KspWebMap` (safe guard).

### 3. Preview without copying (dry run)

```powershell
.\scripts\install.ps1 -DryRun -Verbose -KspRoot $env:KSP_ROOT
```

### 4. Manual copy (if you prefer)

After `build.ps1`:

```powershell
$src = "...\MyMods\artifacts\package\GameData\KspWebMap"
$dst = "...\Kerbal Space Program\GameData\KspWebMap"
# KSP must be quit
Copy-Item $src $dst -Recurse -Force
```

Or copy only the DLL:

```powershell
Copy-Item "...\artifacts\bin\Release\KspWebMap.dll" `
  "...\Kerbal Space Program\GameData\KspWebMap\Plugins\KspWebMap.dll" -Force
```

Use the full `install.ps1` when web assets or cfg files changed too.

## Verify installation

```powershell
$ksp = $env:KSP_ROOT
Test-Path "$ksp\GameData\KspWebMap\Plugins\KspWebMap.dll"
(Get-Item "$ksp\GameData\KspWebMap\Plugins\KspWebMap.dll").LastWriteTime
```

In game:

1. Start KSP, load a **flight** save.
2. Open `http://127.0.0.1:8750/api/health` — expect `"status":"ok"`.
3. Open `http://127.0.0.1:8750/?v=22` and hard refresh (`Ctrl+F5`) after web updates.

## Web-only update (no C# rebuild)

If only `web/` changed and the DLL is already installed:

```powershell
cd web
npm ci
npm run build
Copy-Item "dist\assets\*" "...\Kerbal Space Program\GameData\KspWebMap\Web\assets" -Force
```

KSP can stay running for web-only updates. Hard refresh the browser.

## Uninstall

```powershell
.\scripts\uninstall.ps1 -KspRoot $env:KSP_ROOT
```

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `KSP root was not provided` | Set `$env:KSP_ROOT` or pass `-KspRoot` (see above). |
| `Missing KSP_x64.exe` | `-KspRoot` must point at the **game root**, not `GameData`. |
| `Could not find staged package` | Run `.\scripts\build.ps1` first. |
| `Copy-Item` / DLL access denied | Quit KSP completely, retry install. |
| `No .NET SDK found. Falling back to csc.exe` | Normal on some machines; build can still succeed. |
| Build OK but map unchanged | Hard refresh `?v=22`; confirm DLL timestamp under `Plugins\`. |
| Agent “C# build skipped” | Agent shell has no `KSP_ROOT`; set env or use `-KspRoot` in commands. |

## One-liner checklist (new DLL)

```powershell
# 1) Set root (session or user env)
$env:KSP_ROOT = "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\Kerbal Space Program"

# 2) Quit KSP

# 3) Build + install
cd "C:\Users\brand\OneDrive\Desktop\Kerbal\1.12.3\MyMods"
.\scripts\build.ps1 -Clean
.\scripts\install.ps1

# 4) Start KSP, flight, browser: http://127.0.0.1:8750/?v=22
```

## Related docs

- [OPERATIONS.md](OPERATIONS.md) — smoke test, flight QA, API endpoints
- [BODY_ORBIT_VNV.md](BODY_ORBIT_VNV.md) — in-flight verification scripts
