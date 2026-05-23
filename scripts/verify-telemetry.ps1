# Validates body-orbit alignment fields from KSP Web Map telemetry.
param(
    [string]$BaseUrl = "http://127.0.0.1:8750",
    [double]$SampleTrueFailMeters = 1e6,
    [double]$AnalyticFailMeters = 1e6
)

$ErrorActionPreference = "Stop"

try {
    $telemetry = Invoke-RestMethod -Uri "$BaseUrl/api/telemetry" -Method Get
}
catch {
    Write-Error "Failed to GET $BaseUrl/api/telemetry. Is KSP running with the mod loaded? $_"
    exit 2
}

if (-not $telemetry.valid) {
    Write-Warning "Telemetry snapshot is not marked valid."
}

$schema = $telemetry.schemaVersion
Write-Host "Schema v$schema | resolver: $($telemetry.frameDiagnostics.orbitOffsetMode) | paths: $($telemetry.bodyOrbitPaths.Count)"

$failures = @()
$rows = @()

foreach ($path in $telemetry.bodyOrbitPaths) {
    $v = $path.validation
    if ($null -eq $v) { continue }

    $mode = $v.trailRenderMode
    $liveAnalytic = [double]$v.liveToAnalyticMeters
    $liveSample0 = [double]$v.liveToSample0Meters
    $sampleTrail = if ($null -ne $v.maxSampleToTrailFrameMeters) {
        [double]$v.maxSampleToTrailFrameMeters
    } else {
        [double]$v.maxSampleToTrueMeters
    }

    $rows += [pscustomobject]@{
        Body = $path.bodyName
        Mode = $mode
        LiveToSample0 = $liveSample0
        SampleToTrail = $sampleTrail
        LiveToAnalytic = $liveAnalytic
        PeriodClosure = [double]$v.periodClosureMeters
        Warning = $v.trailWarning
    }

    if ($mode -eq "analytic" -and $liveAnalytic -gt $AnalyticFailMeters) {
        $failures += "$($path.bodyName): liveToAnalytic=$liveAnalytic mode=analytic"
    }

    if ($mode -ne "hidden" -and $sampleTrail -gt $SampleTrueFailMeters) {
        $failures += "$($path.bodyName): maxSampleToTrailFrame=$sampleTrail mode=$mode"
    }

    if ($liveSample0 -gt 1) {
        $failures += "$($path.bodyName): liveToSample0=$liveSample0 m (expected <= 1)"
    }
}

Write-Host ""
Write-Host "Worst sample-to-trail-frame (top 5):"
$rows | Sort-Object SampleToTrail -Descending | Select-Object -First 5 | Format-Table -AutoSize

if ($telemetry.ephemerisValidationResidualMeters -gt 1e6) {
    $failures += "ephemerisValidationResidual=$($telemetry.ephemerisValidationResidualMeters) (same-UT max 1 Mm)"
}

$rootBody = $telemetry.rootBody
$textureRows = @()

foreach ($body in $telemetry.bodies) {
    if ($null -eq $body.name) { continue }
    if ($body.name -eq $rootBody) { continue }
    if ($body.parentBody -ne $rootBody) { continue }

    $textureRows += [pscustomobject]@{
        Body = $body.name
        Status = $body.bodyTextureStatus
        Revision = $body.bodyTextureRevision
        Url = $body.bodyTextureUrl
    }

    if ([string]::IsNullOrWhiteSpace($body.bodyTextureStatus)) {
        $failures += "$($body.name): missing bodyTextureStatus (heliocentric planet)"
    }
    elseif ($body.bodyTextureStatus -eq "ready") {
        if ([string]::IsNullOrWhiteSpace($body.bodyTextureUrl)) {
            $failures += "$($body.name): bodyTextureStatus=ready but bodyTextureUrl missing"
        }
        if ([string]::IsNullOrWhiteSpace($body.bodyTextureRevision)) {
            $failures += "$($body.name): bodyTextureStatus=ready but bodyTextureRevision missing"
        }
    }
}

if ($textureRows.Count -gt 0) {
    Write-Host ""
    Write-Host "Planet body textures:"
    $textureRows | Format-Table -AutoSize
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Error "VERIFY FAILED ($($failures.Count) issue(s)):"
    $failures | ForEach-Object { Write-Host "  - $_" }
    exit 1
}

Write-Host ""
Write-Host "VERIFY PASS: all visible trails within thresholds."
exit 0
