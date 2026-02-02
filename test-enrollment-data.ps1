# View enrollment data from recent logs
$logPath = "AvaloniaApp\BiometricFingerprintsAttendanceSystem\bin\Debug\net10.0-windows\logs\app.log"

if (Test-Path $logPath) {
    Write-Host "=== Recent Enrollment Payloads ===" -ForegroundColor Cyan

    # Find enrollment JSON payloads
    Get-Content $logPath | ForEach-Object {
        $line = $_
        if ($line -match "Enrollment actual JSON payload") {
            $json = $_ | ConvertFrom-Json
            Write-Host "`n--- Enrollment at $($json.timestamp) ---" -ForegroundColor Yellow
            Write-Host $json.message -ForegroundColor White
        }
        if ($line -match "Clock-in actual JSON payload") {
            $json = $_ | ConvertFrom-Json
            Write-Host "`n--- Clock-in at $($json.timestamp) ---" -ForegroundColor Green
            Write-Host $json.message -ForegroundColor White
        }
    } | Select-Object -Last 20
} else {
    Write-Host "Log file not found at: $logPath" -ForegroundColor Red
}

# Also check the test enrollment API response to see what templates look like
Write-Host "`n`n=== Fetching Stored Enrollment Templates from API ===" -ForegroundColor Cyan
$apiKey = "f211a063b7f88d1e2e303628b101754c3499c4982e6df7d8f1da007632004cce"
$regNo = "MDA/2025/2026/0010"
$url = "https://portal.mydreamsacademy.com.ng/api/enrollments/templates?regNo=$([uri]::EscapeDataString($regNo))"

$headers = @{
    "X-API-Key" = $apiKey
}

try {
    $response = Invoke-RestMethod -Uri $url -Method GET -Headers $headers
    Write-Host "Stored templates for $regNo :" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Error: $($_.ErrorDetails.Message)" -ForegroundColor Red
}
