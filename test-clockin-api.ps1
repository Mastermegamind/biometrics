# Test clock-in API
$apiKey = "f211a063b7f88d1e2e303628b101754c3499c4982e6df7d8f1da007632004cce"
$baseUrl = "https://portal.mydreamsacademy.com.ng/api/attendance/clockin"

$headers = @{
    "Content-Type" = "application/json"
    "X-API-Key" = $apiKey
}

# Use a real template from a previous enrollment (from logs)
# This is a sample - replace with actual enrolled template if needed
$payload = @'
{
  "templateBase64": "AOg5Acgp43NcwEE381mKK8RcZ2YBWrtwwQ326O1G0kQ3DmBbk199wx54raj9+EemPkCWk/QdJvvsV0BZGBzzmwJZ+SIuDNLIRUrXH4m1ll3RNjGlNJgmHCxCATDVpflGIOpZEMIjaYd8tlAmeM5OZ4vl9P8TlSECM512EoW8jTfKky+uOIwUBo+ftCThuse2Qu6igke723PCGAhBUo6fL4L56v8YbTnPwMalFAvv+CgLsmgLvCLSKTI/WHA3QQRRyiy26uPRkKiyqk3ZqEQxBa+YJyhe2ZiY3Ju0AS+0wQAEzJzUfLqxPzpZZA3Lv0cjaJcVVlAL5WsGiIG/OD",
  "timestamp": "2026-02-02T07:00:00.000Z",
  "deviceId": "TEST-001"
}
'@

Write-Host "=== Testing Clock-In API ===" -ForegroundColor Cyan
Write-Host "URL: $baseUrl" -ForegroundColor Gray
Write-Host "Payload:" -ForegroundColor Gray
Write-Host $payload -ForegroundColor DarkGray
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Headers $headers -Body $payload
    Write-Host "Success: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Green
} catch {
    Write-Host "Error: $($_.Exception.Response.StatusCode) - $($_.ErrorDetails.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Yellow
    }
}
