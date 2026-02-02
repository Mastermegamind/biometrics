# Build script for Biometric Fingerprints Attendance System
param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net10.0-windows",
    [switch]$IncludeFingerprintSdks = $true
)

$ErrorActionPreference = "Stop"

Write-Host "Building MatcherService..." -ForegroundColor Cyan
dotnet build MatcherService/MatcherService.csproj -c $Configuration -f $Framework -p:IncludeFingerprintSdks=$IncludeFingerprintSdks
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`nBuilding BiometricFingerprintsAttendanceSystem..." -ForegroundColor Cyan
dotnet build AvaloniaApp/BiometricFingerprintsAttendanceSystem/BiometricFingerprintsAttendanceSystem.csproj -c $Configuration -f $Framework -p:IncludeFingerprintSdks=$IncludeFingerprintSdks
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
