# Run script for Biometric Fingerprints Attendance System
param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net10.0-windows"
)

$ErrorActionPreference = "Stop"

$matcherExe = "MatcherService/bin/$Configuration/$Framework/MatcherService.exe"
$appExe = "AvaloniaApp/BiometricFingerprintsAttendanceSystem/bin/$Configuration/$Framework/BiometricFingerprintsAttendanceSystem.exe"

# Check if builds exist
if (-not (Test-Path $matcherExe)) {
    Write-Host "MatcherService not found. Run build.ps1 first." -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $appExe)) {
    Write-Host "BiometricFingerprintsAttendanceSystem not found. Run build.ps1 first." -ForegroundColor Red
    exit 1
}

# Start MatcherService in background
Write-Host "Starting MatcherService..." -ForegroundColor Cyan
$matcherProcess = Start-Process -FilePath $matcherExe -PassThru -WindowStyle Normal

# Give it a moment to start
Start-Sleep -Seconds 2

# Start main application
Write-Host "Starting BiometricFingerprintsAttendanceSystem..." -ForegroundColor Cyan
& $appExe

# When main app exits, stop the matcher service
Write-Host "`nStopping MatcherService..." -ForegroundColor Yellow
Stop-Process -Id $matcherProcess.Id -Force -ErrorAction SilentlyContinue

Write-Host "Done." -ForegroundColor Green
