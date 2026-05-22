param(
    [string] $KspRoot = $env:KSP_ROOT,
    [string] $Configuration = "Release",
    [switch] $Clean
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($KspRoot)) {
    Write-Error "KSP root was not provided. Set KSP_ROOT or pass -KspRoot `"C:\Path\To\KSP`"."
}

$managedPath = Join-Path $KspRoot "KSP_x64_Data\Managed"
$assemblyCSharp = Join-Path $managedPath "Assembly-CSharp.dll"
$unityCore = Join-Path $managedPath "UnityEngine.CoreModule.dll"
$unityImgui = Join-Path $managedPath "UnityEngine.IMGUIModule.dll"

function Assert-FileExists {
    param(
        [string] $Path,
        [string] $Description
    )

    if (-not (Test-Path $Path)) {
        Write-Error "Missing $Description at: $Path"
    }
}

Assert-FileExists -Path (Join-Path $KspRoot "KSP_x64.exe") -Description "KSP executable"
Assert-FileExists -Path $assemblyCSharp -Description "KSP Assembly-CSharp.dll"
Assert-FileExists -Path $unityCore -Description "UnityEngine.CoreModule.dll"
Assert-FileExists -Path $unityImgui -Description "UnityEngine.IMGUIModule.dll"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\KspWebMap\KspWebMap.csproj"
$sourceRoot = Join-Path $repoRoot "src\KspWebMap"
$packageSource = Join-Path $repoRoot "GameData\KspWebMap"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$outputDir = Join-Path $artifactsRoot "bin\$Configuration"
$outputDll = Join-Path $outputDir "KspWebMap.dll"
$packageRoot = Join-Path $artifactsRoot "package"
$packageModRoot = Join-Path $packageRoot "GameData\KspWebMap"
$packagePlugins = Join-Path $packageModRoot "Plugins"

if ($Clean -and (Test-Path $artifactsRoot)) {
    Remove-Item -Path $artifactsRoot -Recurse -Force
}

function Test-DotNetSdkAvailable {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        return $false
    }

    $sdks = & dotnet --list-sdks 2>$null
    return $LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace(($sdks -join ""))
}

function Invoke-DotNetBuild {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

    dotnet build $projectPath `
        --configuration $Configuration `
        /p:OutputPath="$outputDir\" `
        /p:KspRoot="$KspRoot"

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Invoke-FrameworkCscBuild {
    $cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

    if (-not (Test-Path $cscPath)) {
        $cscPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    }

    if (-not (Test-Path $cscPath)) {
        Write-Error "Could not find dotnet SDK or .NET Framework csc.exe. Install the .NET SDK or Visual Studio Build Tools."
    }

    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

    $sources = Get-ChildItem -Path $sourceRoot -Recurse -Filter "*.cs" |
        Where-Object { $_.FullName -notmatch "\\obj\\" } |
        ForEach-Object { $_.FullName }

    & $cscPath `
        /nologo `
        /target:library `
        /optimize+ `
        /out:$outputDll `
        /reference:$assemblyCSharp `
        /reference:$unityCore `
        /reference:$unityImgui `
        $sources

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Invoke-PackageStage {
    if (-not (Test-Path $outputDll)) {
        Write-Error "Build did not produce expected DLL: $outputDll"
    }

    if (-not (Test-Path $packageSource)) {
        Write-Error "Missing source package folder: $packageSource"
    }

    if (Test-Path $packageRoot) {
        Remove-Item -Path $packageRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $packagePlugins | Out-Null

    Get-ChildItem -Force -Path $packageSource |
        Where-Object { $_.Name -ne "Plugins" } |
        ForEach-Object {
            Copy-Item -Path $_.FullName -Destination $packageModRoot -Recurse -Force
        }

    Copy-Item -Path $outputDll -Destination (Join-Path $packagePlugins "KspWebMap.dll") -Force
}

function Invoke-WebBuild {
    $webRoot = Join-Path $repoRoot "web"
    $webPackageJson = Join-Path $webRoot "package.json"

    if (-not (Test-Path $webPackageJson)) {
        Write-Host "No web/package.json found; skipping frontend build."
        return
    }

    $npm = Get-Command npm -ErrorAction SilentlyContinue

    if (-not $npm) {
        Write-Error "npm is required to build the Phase 10 web UI. Install Node.js or run from web/: npm ci && npm run build"
    }

    $viteBin = Join-Path $webRoot "node_modules\vite\bin\vite.js"
    $needsInstall = -not (Test-Path $viteBin)

    Push-Location $webRoot
    try {
        Write-Host "Building web UI (Vite)..."
        if ($needsInstall) {
            Write-Host "Installing web dependencies (node_modules missing or incomplete)..."
            if (Test-Path (Join-Path $webRoot "package-lock.json")) {
                & npm ci
            }
            else {
                & npm install
            }
            if ($LASTEXITCODE -ne 0) {
                exit $LASTEXITCODE
            }
        }
        else {
            Write-Host "Reusing existing web/node_modules (skip npm ci)."
        }

        & npm run build
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
    finally {
        Pop-Location
    }

    $distRoot = Join-Path $webRoot "dist"
    $distAssets = Join-Path $distRoot "assets"
    $bundleJs = Join-Path $distAssets "ksp-solar-map.js"

    if (-not (Test-Path $bundleJs)) {
        Write-Error "Web build did not produce expected bundle: $bundleJs"
    }

    $webDest = Join-Path $packageModRoot "Web"
  $assetsDest = Join-Path $webDest "assets"
    New-Item -ItemType Directory -Force -Path $assetsDest | Out-Null

    if (Test-Path $distAssets) {
        Copy-Item -Path (Join-Path $distAssets "*") -Destination $assetsDest -Recurse -Force
    }

    Write-Host "Copied web bundle to $assetsDest"
}

if (Test-DotNetSdkAvailable) {
    Invoke-DotNetBuild
}
else {
    Write-Host "No .NET SDK found. Falling back to .NET Framework csc.exe."
    Invoke-FrameworkCscBuild
}

Invoke-PackageStage
Invoke-WebBuild

Write-Host ""
Write-Host "Build complete."
Write-Host "DLL output: artifacts\bin\$Configuration\KspWebMap.dll"
Write-Host "Package output: artifacts\package\GameData\KspWebMap"
