# Captures Kerbin orientation fields from live telemetry into web/fixtures.
param(
    [string]$BaseUrl = "http://127.0.0.1:8750",
    [string]$OutFile = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutFile)) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $OutFile = Join-Path $repoRoot "web\fixtures\telemetry-kerbin-orientation-v10-captured.json"
}

try {
    $telemetry = Invoke-RestMethod -Uri "$BaseUrl/api/telemetry" -Method Get
}
catch {
    Write-Error "Failed to GET $BaseUrl/api/telemetry. Start KSP with the mod loaded. $_"
    exit 2
}

$kerbin = $telemetry.bodies | Where-Object { $_.name -eq "Kerbin" } | Select-Object -First 1
if ($null -eq $kerbin) {
    Write-Error "Kerbin not found in telemetry bodies[]"
    exit 1
}

if ($telemetry.schemaVersion -lt 10) {
    Write-Warning "schemaVersion=$($telemetry.schemaVersion); expected >= 10 for orientation fields"
}

$payload = @{
    schemaVersion = $telemetry.schemaVersion
    valid = $telemetry.valid
    rootBody = $telemetry.rootBody
    gameUniversalTimeSeconds = $telemetry.gameUniversalTimeSeconds
    frameDiagnostics = $telemetry.frameDiagnostics
    bodies = @(
        ($telemetry.bodies | Where-Object { $_.name -eq $telemetry.rootBody } | Select-Object -First 1),
        $kerbin
    )
    bodyOrbitPaths = @()
    capturedUtc = (Get-Date).ToUniversalTime().ToString("o")
}

$payload | ConvertTo-Json -Depth 12 | Set-Content -Path $OutFile -Encoding UTF8
Write-Host "Wrote Kerbin orientation snapshot: $OutFile"
Write-Host "  rotationAngleRadians=$($kerbin.rotationAngleRadians)"
Write-Host "  rotationPeriodSeconds=$($kerbin.rotationPeriodSeconds)"
Write-Host "  inverseRotation=$($kerbin.inverseRotation)"
$av = $kerbin.angularVelocityRootRelativeRadPerSec
if ($null -ne $av) {
    Write-Host "  angularVelocity=($($av.x), $($av.y), $($av.z))"
}
