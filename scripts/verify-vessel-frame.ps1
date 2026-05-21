# Checks vessel path vs patch conic plane alignment (live telemetry).
param(
    [string]$BaseUrl = "http://127.0.0.1:8750",
    [double]$MaxLiveToPath0Meters = 1,
    [double]$MaxVesselToPatchPlaneDegrees = 5,
    [double]$MaxPatchToRefTrailDegrees = 15
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$webRoot = Join-Path $repoRoot "web"

Push-Location $webRoot
try {
    npx --yes tsx "../scripts/verify-vessel-frame.mjs" $BaseUrl $MaxLiveToPath0Meters $MaxVesselToPatchPlaneDegrees $MaxPatchToRefTrailDegrees
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
