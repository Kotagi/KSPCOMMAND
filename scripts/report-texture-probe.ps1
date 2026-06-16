param(
    [string] $KspRoot = "C:\Users\brand\OneDrive\Desktop\Kerbal\Kerbal Space Program Sol RSS 1.12.4"
)

$ErrorActionPreference = "Stop"

$logPath = Join-Path $KspRoot "KSP.log"
$probeDir = Join-Path $KspRoot "GameData\KspWebMap\Web\assets\bodies\_probe"

Write-Host "KspWebMap texture probe report"
Write-Host "Log: $logPath"
Write-Host "Previews: $probeDir"
Write-Host ""

if (Test-Path $logPath) {
    Write-Host "=== Recent probe log lines ==="
    Select-String -Path $logPath -Pattern "\[KspWebMap\]\[TextureProbe\]" |
        Select-Object -Last 40 |
        ForEach-Object { $_.Line }
}
else {
    Write-Host "KSP.log not found."
}

Write-Host ""
if (Test-Path $probeDir) {
    Write-Host "=== Preview JPEGs ==="
    Get-ChildItem -Path $probeDir -Filter "*.jpg" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 20 Name, Length, LastWriteTime |
        Format-Table -AutoSize

    $summary = Join-Path $probeDir "summary.txt"
    if (Test-Path $summary) {
        Write-Host "=== summary.txt ==="
        Get-Content $summary
    }
}
else {
    Write-Host "Probe directory not created yet. Enter flight and wait ~3 minutes."
}
