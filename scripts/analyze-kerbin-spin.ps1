# Prints Kerbin spin diagnostic summary from live telemetry (KSP must be running).
param(
    [string]$BaseUrl = "http://127.0.0.1:8750"
)

$ErrorActionPreference = "Stop"

$telemetry = Invoke-RestMethod -Uri "$BaseUrl/api/telemetry" -Method Get
$k = $telemetry.bodies | Where-Object { $_.name -eq "Kerbin" } | Select-Object -First 1
if ($null -eq $k) {
    Write-Error "Kerbin not in telemetry"
    exit 1
}

$period = [double]$k.rotationPeriodSeconds
$stockRate = 2 * [Math]::PI / $period
if ($k.inverseRotation -eq $true) { $stockRate = -$stockRate }

$av = $k.angularVelocityRootRelativeRadPerSec
$n = $k.spinAxisRootRelative
$omegaDot = [double]::NaN
if ($null -ne $av -and $null -ne $n) {
    $avLen = [Math]::Sqrt($av.x * $av.x + $av.y * $av.y + $av.z * $av.z)
    $nLen = [Math]::Sqrt($n.x * $n.x + $n.y * $n.y + $n.z * $n.z)
    if ($avLen -gt 0 -and $nLen -gt 0) {
        $omegaDot = ($av.x * $n.x + $av.y * $n.y + $av.z * $n.z) / ($avLen * $nLen)
    }
}

$webRate = if ([double]::IsNaN($omegaDot)) { $stockRate } else { [Math]::Sign($omegaDot) * [Math]::Abs($stockRate) }

Write-Host "=== Kerbin spin analysis (live telemetry) ==="
Write-Host "schemaVersion: $($telemetry.schemaVersion)"
Write-Host "gameUT: $($telemetry.gameUniversalTimeSeconds)"
Write-Host "sampleUT: $($k.bodyOrientationSampleUniversalTimeSeconds)"
Write-Host ""
Write-Host "rotationAngleRad: $($k.rotationAngleRadians)"
Write-Host "rotationPeriodSec: $period"
Write-Host "inverseRotation: $($k.inverseRotation)"
Write-Host "stockRate (period sign): $stockRate rad/s"
Write-Host ""
Write-Host "spinAxis (n̂): ($($n.x), $($n.y), $($n.z))"
Write-Host "angularVelocity: ($($av.x), $($av.y), $($av.z))"
Write-Host "omegaDotN (ω·n̂): $omegaDot"
Write-Host "omegaAgreesWithPeriod: $(if ($stockRate -ge 0) { $omegaDot -ge 0 } else { $omegaDot -lt 0 })"
Write-Host ""
Write-Host "web resolved rate (ω·n̂ sign, period |mag|): $webRate rad/s"
if (-not [double]::IsNaN($omegaDot) -and (($stockRate -ge 0) -ne ($omegaDot -ge 0))) {
    Write-Host ""
    Write-Warning "omega-dot-n disagrees with rotationPeriod sign; v133+ web uses omega-dot-n (not period-only)."
}
