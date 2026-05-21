# Phase 11: body icon position parity vs trail and KSP true API at now.
param(
    [string]$BaseUrl = "http://127.0.0.1:8750"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$mjs = Join-Path $scriptDir "verify-body-positions.mjs"

if (-not (Test-Path $mjs)) {
    Write-Error "Missing $mjs"
    exit 2
}

$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) {
    Write-Error "Node.js required for verify-body-positions.mjs"
    exit 2
}

& node $mjs $BaseUrl
exit $LASTEXITCODE
