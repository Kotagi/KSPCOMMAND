[CmdletBinding()]
param(
    [string] $KspRoot = $env:KSP_ROOT,
    [switch] $DryRun
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($KspRoot)) {
    Write-Error "KSP root was not provided. Set KSP_ROOT or pass -KspRoot `"C:\Path\To\KSP`"."
}

if (-not (Test-Path (Join-Path $KspRoot "KSP_x64.exe"))) {
    Write-Error "KSP root does not look valid. Missing KSP_x64.exe under: $KspRoot"
}

$destination = Join-Path $KspRoot "GameData\KspWebMap"
$parent = Split-Path $destination -Parent

if ((Split-Path $destination -Leaf) -ne "KspWebMap" -or (Split-Path $parent -Leaf) -ne "GameData") {
    Write-Error "Refusing to uninstall unexpected path: $destination"
}

if (-not (Test-Path $destination)) {
    Write-Host "KspWebMap is not installed at: $destination"
    exit 0
}

if ($DryRun) {
    Write-Host "Dry run: would remove: $destination"
    exit 0
}

Remove-Item -Path $destination -Recurse -Force
Write-Host "Uninstalled KspWebMap from: $destination"
