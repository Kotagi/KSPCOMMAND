[CmdletBinding()]
param(
    [string] $KspRoot = $env:KSP_ROOT,
    [switch] $DryRun,
    [switch] $NoClean
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($KspRoot)) {
    Write-Error "KSP root was not provided. Set KSP_ROOT or pass -KspRoot `"C:\Path\To\KSP`"."
}

function Assert-SafeModDestination {
    param([string] $Path)

    $leaf = Split-Path $Path -Leaf
    $parent = Split-Path $Path -Parent
    $parentLeaf = Split-Path $parent -Leaf

    if ($leaf -ne "KspWebMap" -or $parentLeaf -ne "GameData") {
        Write-Error "Refusing to install to unexpected destination: $Path"
    }
}

if (-not (Test-Path (Join-Path $KspRoot "KSP_x64.exe"))) {
    Write-Error "KSP root does not look valid. Missing KSP_x64.exe under: $KspRoot"
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$source = Join-Path $repoRoot "artifacts\package\GameData\KspWebMap"
$destination = Join-Path $KspRoot "GameData\KspWebMap"
$destinationParent = Split-Path $destination -Parent

Assert-SafeModDestination -Path $destination

if (-not (Test-Path $source)) {
    Write-Error "Could not find staged package: $source. Run scripts\build.ps1 first."
}

Write-Verbose "Source package: $source"
Write-Verbose "Destination: $destination"

if ($DryRun) {
    Write-Host "Dry run: would install KspWebMap to: $destination"
    if (-not $NoClean -and (Test-Path $destination)) {
        Write-Host "Dry run: would clean existing destination before copying."
    }

    Get-ChildItem -Path $source -Recurse -Force | ForEach-Object {
        Write-Host ("Dry run: would copy " + $_.FullName)
    }

    exit 0
}

New-Item -ItemType Directory -Force -Path $destinationParent | Out-Null

if (-not $NoClean -and (Test-Path $destination)) {
    Write-Verbose "Cleaning existing destination."
    Remove-Item -Path $destination -Recurse -Force
}

Copy-Item -Path $source -Destination $destinationParent -Recurse -Force

Write-Host "Installed KspWebMap to: $destination"
