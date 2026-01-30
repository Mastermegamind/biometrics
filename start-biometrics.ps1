param(
    [string]$DigitalPersonaSdkPath = "C:\Program Files\DigitalPersona\One Touch SDK\.NET\Bin",
    [string]$MatcherUrl = "http://localhost:5085",
    [switch]$WithSdks
)

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$matcherDir = Join-Path $repoRoot "MatcherService"
$appDir = Join-Path $repoRoot "AvaloniaApp\BiometricFingerprintsAttendanceSystem"
$envPath = Join-Path $appDir ".env"

if (Test-Path $envPath) {
    Get-Content $envPath | ForEach-Object {
        $line = $_.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith("#")) { return }
        $idx = $line.IndexOf("=")
        if ($idx -le 0) { return }
        $key = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim()
        if ($value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        if ($key -eq "BIO_DP_SDK_PATH" -and -not [string]::IsNullOrWhiteSpace($value)) {
            $DigitalPersonaSdkPath = $value
        }
    }
}

$matcherArgs = @("run", "--urls", $MatcherUrl)
$appArgs = @("run", "-c", "Debug", "-f", "net10.0-windows")

if ($WithSdks) {
    $matcherArgs += @("-p:IncludeFingerprintSdks=true", "-p:DigitalPersonaSdkPath=$DigitalPersonaSdkPath")
    $appArgs += @("-p:IncludeFingerprintSdks=true", "-p:DigitalPersonaSdkPath=$DigitalPersonaSdkPath")
}

Start-Process -FilePath "dotnet" -ArgumentList $matcherArgs -WorkingDirectory $matcherDir
Start-Process -FilePath "dotnet" -ArgumentList $appArgs -WorkingDirectory $repoRoot

Write-Host "MatcherService started at $MatcherUrl"
Write-Host "Main app started from $repoRoot"
