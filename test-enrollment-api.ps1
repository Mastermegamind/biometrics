# Test enrollment API with different payload formats
$apiKey = "f211a063b7f88d1e2e303628b101754c3499c4982e6df7d8f1da007632004cce"
$baseUrl = "https://portal.mydreamsacademy.com.ng/api/enrollments"

$headers = @{
    "Content-Type" = "application/json"
    "X-API-Key" = $apiKey
}

# Test 1: snake_case (current code format)
Write-Host "`n=== Test 1: snake_case format ===" -ForegroundColor Cyan
$payload1 = @'
{
  "regno": "MDA/2025/2026/0010",
  "records": [
    {
      "regno": "MDA/2025/2026/0010",
      "finger_name": "left-thumb",
      "finger_index": 6,
      "template": "dGVzdA==",
      "template_data": "dGVzdA==",
      "captured_at": "2026-02-02T05:49:22"
    }
  ]
}
'@
try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Headers $headers -Body $payload1
    Write-Host "Success: $($response | ConvertTo-Json -Compress)" -ForegroundColor Green
} catch {
    Write-Host "Error: $($_.Exception.Response.StatusCode) - $($_.ErrorDetails.Message)" -ForegroundColor Red
}

# Test 2: camelCase format
Write-Host "`n=== Test 2: camelCase format ===" -ForegroundColor Cyan
$payload2 = @'
{
  "regNo": "MDA/2025/2026/0010",
  "records": [
    {
      "regNo": "MDA/2025/2026/0010",
      "fingerName": "left-thumb",
      "fingerIndex": 6,
      "template": "dGVzdA==",
      "templateData": "dGVzdA==",
      "capturedAt": "2026-02-02T05:49:22"
    }
  ]
}
'@
try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Headers $headers -Body $payload2
    Write-Host "Success: $($response | ConvertTo-Json -Compress)" -ForegroundColor Green
} catch {
    Write-Host "Error: $($_.Exception.Response.StatusCode) - $($_.ErrorDetails.Message)" -ForegroundColor Red
}

# Test 3: Laravel style (reg_no with underscore)
Write-Host "`n=== Test 3: Laravel style (reg_no) ===" -ForegroundColor Cyan
$payload3 = @'
{
  "reg_no": "MDA/2025/2026/0010",
  "records": [
    {
      "finger_name": "left-thumb",
      "finger_index": 6,
      "template": "dGVzdA==",
      "captured_at": "2026-02-02T05:49:22"
    }
  ]
}
'@
try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Headers $headers -Body $payload3
    Write-Host "Success: $($response | ConvertTo-Json -Compress)" -ForegroundColor Green
} catch {
    Write-Host "Error: $($_.Exception.Response.StatusCode) - $($_.ErrorDetails.Message)" -ForegroundColor Red
}

# Test 4: Check what GET returns
Write-Host "`n=== Test 4: GET enrollments ===" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method GET -Headers $headers
    Write-Host "Success: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Green
} catch {
    Write-Host "Error: $($_.Exception.Response.StatusCode) - $($_.ErrorDetails.Message)" -ForegroundColor Red
}
