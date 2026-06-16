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
if ($schema -lt 10) {
    Write-Warning "Schema v10 expected for planet body orientation (tilt/spin); got v$schema — rebuild and install DLL."
}

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
$orientationRows = @()

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

    $q = $body.bodyOrientationRootRelative
    if ($null -ne $q) {
        $norm = [Math]::Sqrt([double]$q.x * $q.x + [double]$q.y * $q.y + [double]$q.z * $q.z + [double]$q.w * $q.w)
        $spin = $body.spinAxisRootRelative
        $period = [double]$body.rotationPeriodSeconds
        $stockRate = if ($period -gt 0) { 2 * [Math]::PI / $period } else { [double]::NaN }
        if ($body.inverseRotation -eq $true) { $stockRate = -$stockRate }
        $av = $body.angularVelocityRootRelativeRadPerSec
        $omegaDot = [double]::NaN
        if ($null -ne $av -and $null -ne $spin) {
            $avLen = [Math]::Sqrt([double]$av.x * $av.x + [double]$av.y * $av.y + [double]$av.z * $av.z)
            if ($avLen -gt 1e-12) {
                $axisLen = [Math]::Sqrt([double]$spin.x * $spin.x + [double]$spin.y * $spin.y + [double]$spin.z * $spin.z)
                if ($axisLen -gt 1e-12) {
                    $omegaDot = ([double]$av.x * $spin.x + [double]$av.y * $spin.y + [double]$av.z * $spin.z) / ($avLen * $axisLen)
                }
            }
        }
        $omegaOk = if ([double]::IsNaN($omegaDot) -or [double]::IsNaN($stockRate)) {
            ""
        } elseif (($stockRate -ge 0) -eq ($omegaDot -ge 0)) {
            "ok"
        } else {
            "MISMATCH"
        }

        $orientationRows += [pscustomobject]@{
            Body = $body.name
            Rotates = $body.rotates
            QuatNorm = $norm
            SpinAxis = if ($null -ne $spin) { "($($spin.x),$($spin.y),$($spin.z))" } else { "" }
            SampleUt = $body.bodyOrientationSampleUniversalTimeSeconds
            RotAngle = $body.rotationAngleRadians
            Period = $body.rotationPeriodSeconds
            StockRate = $stockRate
            OmegaDot = $omegaDot
            SpinSign = $omegaOk
        }
        if ($omegaOk -eq "MISMATCH") {
            Write-Warning "$($body.name): angularVelocity opposes stock rotationPeriod sign (web uses period sign)"
        }
        if ($norm -lt 0.99 -or $norm -gt 1.01) {
            $failures += "$($body.name): bodyOrientationRootRelative norm=$norm (expected ~1)"
        }
        if ($body.rotates -eq $true) {
            if ($null -eq $spin) {
                $failures += "$($body.name): rotates=true but spinAxisRootRelative missing"
            }
            else {
                $axisLen = [Math]::Sqrt([double]$spin.x * $spin.x + [double]$spin.y * $spin.y + [double]$spin.z * $spin.z)
                if ($axisLen -lt 0.99 -or $axisLen -gt 1.01) {
                    $failures += "$($body.name): spinAxisRootRelative not unit (len=$axisLen)"
                }
            }
        }
    }
    else {
        $failures += "$($body.name): missing bodyOrientationRootRelative (heliocentric planet, schema v10)"
    }
}

if ($textureRows.Count -gt 0) {
    Write-Host ""
    Write-Host "Planet body textures:"
    $textureRows | Format-Table -AutoSize
}

if ($orientationRows.Count -gt 0) {
    Write-Host ""
    Write-Host "Planet body orientation (schema v10):"
    $orientationRows | Format-Table -AutoSize
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
