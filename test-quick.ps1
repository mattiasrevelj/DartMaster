# DartMaster API Quick Test
param([string]$action = "all")

$API = "http://localhost:5146/api"
$token = $null

Write-Host "========== DartMaster API Test ==========" -ForegroundColor Cyan

# Test Health
Write-Host "`nTesting health endpoint..." -ForegroundColor Yellow
try {
    $h = Invoke-WebRequest -Uri "http://localhost:5146/health" -UseBasicParsing -ErrorAction Stop
    Write-Host "✓ API is healthy (Status: $($h.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "✗ API not responding" -ForegroundColor Red
    exit 1
}

# Register User
Write-Host "`nRegistering user..." -ForegroundColor Yellow
try {
    $body = @{
        fullName = "Test User"
        username = "testuser123"
        email = "test123@example.com"
        password = "Test@1234"
    } | ConvertTo-Json
    
    $reg = Invoke-WebRequest -Uri "$API/users/register" -Method POST -ContentType "application/json" -Body $body -UseBasicParsing -ErrorAction Stop
    Write-Host "✓ User registered" -ForegroundColor Green
} catch {
    Write-Host "⚠ Registration: $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
}

# Login
Write-Host "`nLogging in..." -ForegroundColor Yellow
try {
    $body = @{
        username = "testuser123"
        password = "Test@1234"
    } | ConvertTo-Json
    
    $login = Invoke-WebRequest -Uri "$API/users/login" -Method POST -ContentType "application/json" -Body $body -UseBasicParsing -ErrorAction Stop
    $loginData = $login.Content | ConvertFrom-Json
    $token = $loginData.token
    Write-Host "✓ Login successful" -ForegroundColor Green
    Write-Host "  Token: $($token.Substring(0,50))..." -ForegroundColor Gray
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Get Tournaments
Write-Host "`nFetching tournaments..." -ForegroundColor Yellow
try {
    $tours = Invoke-WebRequest -Uri "$API/tournaments" -UseBasicParsing -ErrorAction Stop
    $data = $tours.Content | ConvertFrom-Json
    Write-Host "✓ Tournaments fetched (Count: $($data.Count))" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Create Tournament (needs auth)
if ($token) {
    Write-Host "`nCreating tournament..." -ForegroundColor Yellow
    try {
        $body = @{
            name = "Test Tournament $(Get-Random)"
            description = "API Test"
            startDate = (Get-Date).ToUniversalTime().ToString("o")
            maxPlayers = 16
        } | ConvertTo-Json
        
        $headers = @{ "Authorization" = "Bearer $token" }
        $create = Invoke-WebRequest -Uri "$API/tournaments" -Method POST -ContentType "application/json" -Body $body -Headers $headers -UseBasicParsing -ErrorAction Stop
        $crData = $create.Content | ConvertFrom-Json
        Write-Host "✓ Tournament created (ID: $($crData.id))" -ForegroundColor Green
    } catch {
        Write-Host "✗ Creation failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n✓ Test complete!" -ForegroundColor Cyan
Write-Host ""
