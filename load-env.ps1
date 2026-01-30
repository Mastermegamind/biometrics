param(
    [string]$EnvPath = ""
)

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($EnvPath)) {
    $EnvPath = Join-Path $repoRoot "AvaloniaApp\BiometricFingerprintsAttendanceSystem\.env"
}

if (-not (Test-Path $EnvPath)) {
    Write-Error "Env file not found: $EnvPath"
    return
}

Get-Content $EnvPath | ForEach-Object {
    $line = $_.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith("#")) { return }
    $idx = $line.IndexOf("=")
    if ($idx -le 0) { return }
    $key = $line.Substring(0, $idx).Trim()
    $value = $line.Substring($idx + 1).Trim()
    if ($value.StartsWith('"') -and $value.EndsWith('"')) {
        $value = $value.Substring(1, $value.Length - 2)
    }
    if ([string]::IsNullOrWhiteSpace($key)) { return }
    Set-Item -Path "Env:$key" -Value $value
    Write-Host "Set $key"
}

Write-Host "Loaded env from $EnvPath"
