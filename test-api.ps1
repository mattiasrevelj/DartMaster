# DartMaster API Test Script
# Usage: powershell .\test-api.ps1

$API_URL = "http://localhost:5146/api"
$Global:AuthToken = $null

function Write-Section($Title) {
    Write-Host "`n========== $Title ==========" -ForegroundColor Cyan
}

function Test-Health {
    Write-Section "Health Check"
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5146/health" -UseBasicParsing -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ API is healthy" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ API health check failed: $($response.StatusCode)" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ Connection failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Register-User {
    Write-Section "Register User"
    try {
        $body = @{
            fullName = "Test User"
            username = "testuser"
            email = "test@example.com"
            password = "Test@1234"
        } | ConvertTo-Json
        
        $response = Invoke-WebRequest -Uri "$API_URL/users/register" `
            -Method POST `
            -ContentType "application/json" `
            -Body $body `
            -UseBasicParsing
        
        $data = $response.Content | ConvertFrom-Json
        Write-Host "✓ Registration successful" -ForegroundColor Green
        Write-Host ($data | ConvertTo-Json -Depth 2)
        return $data
    } catch {
        Write-Host "✗ Registration failed" -ForegroundColor Red
        Write-Host $_.Exception.Message
        return $null
    }
}

function Login-User {
    Write-Section "Login"
    try {
        $body = @{
            username = "testuser"
            password = "Test@1234"
        } | ConvertTo-Json
        
        $response = Invoke-WebRequest -Uri "$API_URL/users/login" `
            -Method POST `
            -ContentType "application/json" `
            -Body $body `
            -UseBasicParsing
        
        $data = $response.Content | ConvertFrom-Json
        if ($data.token) {
            $Global:AuthToken = $data.token
            Write-Host "✓ Login successful" -ForegroundColor Green
            Write-Host "Token saved:" $data.token.Substring(0, 50) "..."
            return $data.token
        } else {
            Write-Host "✗ Login failed: No token received" -ForegroundColor Red
            return $null
        }
    } catch {
        Write-Host "✗ Login failed" -ForegroundColor Red
        Write-Host $_.Exception.Message
        return $null
    }
}

function Get-Tournaments {
    Write-Section "Get Tournaments"
    try {
        $response = Invoke-WebRequest -Uri "$API_URL/tournaments" `
            -UseBasicParsing
        
        $data = $response.Content | ConvertFrom-Json
        Write-Host "✓ Fetched tournaments" -ForegroundColor Green
        if ($data -is [System.Array]) {
            Write-Host "Count: $($data.Count)"
            $data | ForEach-Object { Write-Host "  - $($_.name) (ID: $($_.id))" }
        } else {
            Write-Host ($data | ConvertTo-Json)
        }
        return $data
    } catch {
        Write-Host "✗ Failed to fetch tournaments" -ForegroundColor Red
        Write-Host $_.Exception.Message
        return $null
    }
}

function Create-Tournament {
    Write-Section "Create Tournament"
    
    if (-not $Global:AuthToken) {
        Write-Host "✗ Not authenticated. Please login first." -ForegroundColor Red
        return $null
    }
    
    try {
        $body = @{
            name = "Friday Night Darts $(Get-Random)"
            description = "Weekly tournament"
            startDate = (Get-Date).ToUniversalTime().ToString("o")
            maxPlayers = 16
        } | ConvertTo-Json
        
        $headers = @{
            "Authorization" = "Bearer $Global:AuthToken"
        }
        
        $response = Invoke-WebRequest -Uri "$API_URL/tournaments" `
            -Method POST `
            -ContentType "application/json" `
            -Body $body `
            -Headers $headers `
            -UseBasicParsing
        
        $data = $response.Content | ConvertFrom-Json
        Write-Host "✓ Tournament created" -ForegroundColor Green
        Write-Host ($data | ConvertTo-Json)
        return $data
    } catch {
        Write-Host "✗ Failed to create tournament" -ForegroundColor Red
        Write-Host $_.Exception.Message
        return $null
    }
}

function Test-AllEndpoints {
    Write-Host "`n`n╔══════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║    DartMaster API Test Suite Started       ║" -ForegroundColor Magenta
    Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Magenta
    
    # Health check
    if (-not (Test-Health)) {
        Write-Host "`n✗ API is not running. Start it with: docker-compose up -d" -ForegroundColor Red
        return
    }
    
    # Register
    Register-User
    
    # Login
    Login-User
    
    # Get tournaments (no auth needed)
    Get-Tournaments
    
    # Create tournament (needs auth)
    Create-Tournament
    
    # Get tournaments again to verify creation
    Write-Host "`n" -NoNewline
    Get-Tournaments
    
    Write-Host "`n`n╔══════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║    Tests Completed                          ║" -ForegroundColor Magenta
    Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Magenta
}

# Run all tests
Test-AllEndpoints

Write-Host "`n"
