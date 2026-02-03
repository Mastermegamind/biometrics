param(
    [Parameter(Mandatory = $true)]
    [string]$BinPath,
    [string]$ApiUrl = "https://portal.mydreamsacademy.com.ng/api/enrollments/templates/all",
    [string]$ApiKey = "",
    [string]$RegNo = "",
    [int]$FingerIndex = 0
)

function Get-Sha256Hex([byte[]]$Bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace("-", "")
    } finally {
        $sha.Dispose()
    }
}

function Is-Base64Bytes([byte[]]$Bytes) {
    if ($Bytes.Length -eq 0 -or ($Bytes.Length % 4) -ne 0) { return $false }
    foreach ($b in $Bytes) {
        $c = [char]$b
        $ok = (($c -ge 'A' -and $c -le 'Z') -or
               ($c -ge 'a' -and $c -le 'z') -or
               ($c -ge '0' -and $c -le '9') -or
               $c -eq '+' -or $c -eq '/' -or $c -eq '=')
        if (-not $ok) { return $false }
    }
    return $true
}

function Decode-TemplateBase64([string]$Base64) {
    try {
        $bytes = [System.Convert]::FromBase64String($Base64)
    } catch {
        return $null
    }

    if (Is-Base64Bytes $bytes) {
        try {
            $text = [System.Text.Encoding]::ASCII.GetString($bytes)
            $decoded2 = [System.Convert]::FromBase64String($text)
            if ($decoded2.Length -gt 32) {
                return @{ Bytes = $decoded2; DoubleDecoded = $true }
            }
        } catch {
            # ignore
        }
    }

    return @{ Bytes = $bytes; DoubleDecoded = $false }
}

if (-not (Test-Path -LiteralPath $BinPath)) {
    Write-Error "BinPath not found: $BinPath"
    exit 1
}

$capturedBytes = [System.IO.File]::ReadAllBytes($BinPath)
$capturedHash = Get-Sha256Hex $capturedBytes
Write-Host "Captured: $BinPath"
Write-Host "Captured bytes: $($capturedBytes.Length)"
Write-Host "Captured sha256: $capturedHash"
Write-Host ""

$headers = @{}
if ($ApiKey -ne "") {
    $headers["X-API-Key"] = $ApiKey
}

try {
    $response = Invoke-RestMethod -Uri $ApiUrl -Headers $headers -Method Get
} catch {
    Write-Error "API request failed: $($_.Exception.Message)"
    exit 1
}

$records = $null
if ($response -is [array]) {
    $records = $response
} elseif ($response.records) {
    $records = $response.records
} elseif ($response.data) {
    $records = $response.data
}

if (-not $records) {
    Write-Host "No records returned from API."
    exit 0
}

if ($RegNo -ne "") {
    $records = $records | Where-Object { $_.regno -eq $RegNo -or $_.regNo -eq $RegNo }
}
if ($FingerIndex -gt 0) {
    $records = $records | Where-Object { $_.finger_index -eq $FingerIndex -or $_.fingerIndex -eq $FingerIndex }
}

if (-not $records) {
    Write-Host "No records matched the filter."
    exit 0
}

$matchCount = 0
foreach ($r in $records) {
    $regNo = $r.regno
    if (-not $regNo) { $regNo = $r.regNo }
    $fingerIndex = $r.finger_index
    if (-not $fingerIndex) { $fingerIndex = $r.fingerIndex }
    $template = $r.template_data
    if (-not $template) { $template = $r.templateData }
    if (-not $template) { $template = $r.template }

    if (-not $template) { continue }
    $decoded = Decode-TemplateBase64 $template
    if ($null -eq $decoded) { continue }

    $bytes = $decoded.Bytes
    $hash = Get-Sha256Hex $bytes
    $doubleFlag = $decoded.DoubleDecoded
    $isMatch = $hash -eq $capturedHash

    Write-Host "RegNo=$regNo FingerIndex=$fingerIndex Bytes=$($bytes.Length) DoubleDecoded=$doubleFlag Match=$isMatch Hash=$hash"
    if ($isMatch) { $matchCount++ }
}

Write-Host ""
Write-Host "Total matches: $matchCount"
