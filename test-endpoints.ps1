#!/usr/bin/env pwsh
<#
.SYNOPSIS
Comprehensive test script for DartMaster API endpoints

.DESCRIPTION
Tests all backend endpoints with proper error handling and response validation
#>

param(
    [string]$ApiUrl = "http://localhost:5146/api",
    [switch]$Verbose
)

$ErrorActionPreference = 'Continue'
$token = $null
$userId = $null
$tournamentId = $null
$matchId = $null

# Color output
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Error { Write-Host "❌ $_" -ForegroundColor Red }
function Write-Info { Write-Host "ℹ️  $_" -ForegroundColor Cyan }
function Write-Test { Write-Host "`n┌─ TEST: $_" -ForegroundColor Yellow }
function Write-Result { Write-Host "└─ RESULT: $_" -ForegroundColor Yellow }

# Helper function to make API calls
function Invoke-ApiCall {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [bool]$RequireAuth = $false,
        [string]$ContentType = "application/json"
    )
    
    $url = "$ApiUrl$Endpoint"
    $headers = @{ "Content-Type" = $ContentType }
    
    if ($RequireAuth -and $token) {
        $headers["Authorization"] = "Bearer $token"
    }
    
    Write-Info "→ $Method $Endpoint"
    
    try {
        $params = @{
            Uri = $url
            Method = $Method
            Headers = $headers
            UseBasicParsing = $true
        }
        
        if ($Body) {
            if ($Body -is [hashtable]) {
                $params["Body"] = ($Body | ConvertTo-Json)
            } else {
                $params["Body"] = $Body
            }
        }
        
        $response = Invoke-WebRequest @params
        
        if ($Verbose) {
            Write-Host "Status: $($response.StatusCode)"
        }
        
        return $response | ConvertFrom-Json
    } catch {
        Write-Error "API Call failed: $_"
        return $null
    }
}

Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║        DartMaster API Endpoint Test Suite                     ║" -ForegroundColor Magenta
Write-Host "║        Testing: $ApiUrl " -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta

# ==================== HEALTH & INFO ====================
Write-Host "`n🏥 HEALTH & INFO ENDPOINTS" -ForegroundColor Cyan

Write-Test "/health"
$health = Invoke-WebRequest -Uri "http://localhost:5146/health" -UseBasicParsing | ConvertFrom-Json
if ($health.status -eq "ok") {
    Write-Success "✓ Health check passed"
    Write-Result "Health is OK"
} else {
    Write-Error "Health check failed"
}

Write-Test "/api/version"
$version = Invoke-ApiCall -Method "GET" -Endpoint "/version"
if ($version) {
    Write-Success "✓ Version endpoint working"
    Write-Result "API Version: $($version.version), Environment: $($version.environment)"
} else {
    Write-Error "Version endpoint failed"
}

# ==================== AUTH ENDPOINTS ====================
Write-Host "`n🔐 AUTH ENDPOINTS" -ForegroundColor Cyan

Write-Test "POST /api/auth/register"
$registerData = @{
    username = "testuser_$(Get-Random)"
    email = "test_$(Get-Random)@example.com"
    password = "TestPass123!"
    fullName = "Test User"
}
$register = Invoke-ApiCall -Method "POST" -Endpoint "/auth/register" -Body $registerData
if ($register.success) {
    Write-Success "✓ Registration successful"
    $token = $register.data.token
    $userId = $register.data.userId
    Write-Result "User ID: $userId, Token received: $($token.Length) chars"
} else {
    Write-Error "Registration failed: $($register.message)"
}

Write-Test "POST /api/auth/login"
$loginData = @{
    username = $registerData.username
    password = $registerData.password
}
$login = Invoke-ApiCall -Method "POST" -Endpoint "/auth/login" -Body $loginData
if ($login.success) {
    Write-Success "✓ Login successful"
    if (-not $token) { $token = $login.data.token }
    if (-not $userId) { $userId = $login.data.userId }
    Write-Result "Token: $($token.Substring(0, 20))..."
} else {
    Write-Error "Login failed: $($login.message)"
}

# ==================== USER ENDPOINTS ====================
Write-Host "`n👤 USER ENDPOINTS" -ForegroundColor Cyan

Write-Test "GET /api/users/{id}"
if ($userId) {
    $getUser = Invoke-ApiCall -Method "GET" -Endpoint "/users/$userId" -RequireAuth $true
    if ($getUser) {
        Write-Success "✓ Get user successful"
        Write-Result "User: $($getUser.username) ($($getUser.email))"
    } else {
        Write-Error "Get user failed"
    }
} else {
    Write-Error "Skipped - No user ID"
}

Write-Test "PUT /api/users/{id}"
if ($userId) {
    $updateUser = @{ fullName = "Updated Test User" }
    $updated = Invoke-ApiCall -Method "PUT" -Endpoint "/users/$userId" -Body $updateUser -RequireAuth $true
    if ($updated) {
        Write-Success "✓ Update user successful"
        Write-Result "Full Name updated"
    } else {
        Write-Error "Update user failed"
    }
} else {
    Write-Error "Skipped - No user ID"
}

# ==================== TOURNAMENT ENDPOINTS ====================
Write-Host "`n🏆 TOURNAMENT ENDPOINTS" -ForegroundColor Cyan

Write-Test "GET /api/tournaments (Anonymous)"
$allTournaments = Invoke-ApiCall -Method "GET" -Endpoint "/tournaments"
if ($allTournaments.success) {
    Write-Success "✓ Get tournaments successful"
    Write-Result "Found $($allTournaments.data.Count) tournaments"
} else {
    Write-Error "Get tournaments failed"
}

Write-Test "POST /api/tournaments (Create - Requires Auth)"
if ($token) {
    $createTournament = @{
        name = "Test Tournament $(Get-Random)"
        description = "Automated test tournament"
        startDate = (Get-Date).AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss")
        maxPlayers = 8
    }
    $newTournament = Invoke-ApiCall -Method "POST" -Endpoint "/tournaments" -Body $createTournament -RequireAuth $true
    if ($newTournament.success) {
        Write-Success "✓ Create tournament successful"
        $tournamentId = $newTournament.data.id
        Write-Result "Tournament ID: $tournamentId"
    } else {
        Write-Error "Create tournament failed: $($newTournament.message)"
    }
} else {
    Write-Error "Skipped - No authentication token"
}

if ($tournamentId) {
    Write-Test "GET /api/tournaments/{id} (Anonymous)"
    $getTournament = Invoke-ApiCall -Method "GET" -Endpoint "/tournaments/$tournamentId"
    if ($getTournament.success) {
        Write-Success "✓ Get tournament by ID successful"
        Write-Result "Tournament: $($getTournament.data.name)"
    } else {
        Write-Error "Get tournament failed"
    }

    Write-Test "PUT /api/tournaments/{id} (Update - Requires Auth)"
    if ($token) {
        $updateTournament = @{
            name = "Updated Tournament $(Get-Random)"
            description = "Updated test tournament"
        }
        $updated = Invoke-ApiCall -Method "PUT" -Endpoint "/tournaments/$tournamentId" -Body $updateTournament -RequireAuth $true
        if ($updated.success) {
            Write-Success "✓ Update tournament successful"
            Write-Result "Tournament name updated"
        } else {
            Write-Error "Update tournament failed"
        }
    }
}

# ==================== MATCH ENDPOINTS ====================
Write-Host "`n🎮 MATCH ENDPOINTS" -ForegroundColor Cyan

if ($tournamentId) {
    Write-Test "GET /api/matches/tournament/{tournamentId} (Anonymous)"
    $getMatches = Invoke-ApiCall -Method "GET" -Endpoint "/matches/tournament/$tournamentId"
    if ($getMatches.success) {
        Write-Success "✓ Get tournament matches successful"
        Write-Result "Found $($getMatches.data.Count) matches"
    } else {
        Write-Error "Get matches failed"
    }

    Write-Test "POST /api/matches (Create - Requires Auth)"
    if ($token) {
        $createMatch = @{
            tournamentId = $tournamentId
            matchFormat = "501"
            participants = @()
        }
        $newMatch = Invoke-ApiCall -Method "POST" -Endpoint "/matches" -Body $createMatch -RequireAuth $true
        if ($newMatch.success) {
            Write-Success "✓ Create match successful"
            $matchId = $newMatch.data.id
            Write-Result "Match ID: $matchId"
        } else {
            Write-Error "Create match failed: $($newMatch.message)"
        }
    }
} else {
    Write-Error "Skipped - No tournament ID"
}

if ($matchId) {
    Write-Test "GET /api/matches/{id} (Anonymous)"
    $getMatch = Invoke-ApiCall -Method "GET" -Endpoint "/matches/$matchId"
    if ($getMatch.success) {
        Write-Success "✓ Get match by ID successful"
        Write-Result "Match Status: $($getMatch.data.status)"
    } else {
        Write-Error "Get match failed"
    }

    Write-Test "POST /api/matches/{id}/participants (Add Participant - Requires Auth)"
    if ($token) {
        $addParticipant = @{ userId = $userId }
        $participant = Invoke-ApiCall -Method "POST" -Endpoint "/matches/$matchId/participants" -Body $addParticipant -RequireAuth $true
        if ($participant) {
            Write-Success "✓ Add participant successful"
            Write-Result "Participant added to match"
        } else {
            Write-Error "Add participant failed"
        }
    }

    Write-Test "PUT /api/matches/{id}/status (Update Status - Requires Auth)"
    if ($token) {
        $updateStatus = @{ status = "IN_PROGRESS" }
        $statusUpdate = Invoke-ApiCall -Method "PUT" -Endpoint "/matches/$matchId/status" -Body $updateStatus -RequireAuth $true
        if ($statusUpdate) {
            Write-Success "✓ Update match status successful"
            Write-Result "Match status updated to IN_PROGRESS"
        } else {
            Write-Error "Update match status failed"
        }
    }
}

# ==================== DART SCORE ENDPOINTS ====================
Write-Host "`n🎯 DART SCORE ENDPOINTS" -ForegroundColor Cyan

if ($matchId) {
    Write-Test "POST /api/matches/{matchId}/darts (Record Dart - Requires Auth)"
    if ($token) {
        $recordDart = @{ score = 20; multiplier = 1 }
        $dart = Invoke-ApiCall -Method "POST" -Endpoint "/matches/$matchId/darts" -Body $recordDart -RequireAuth $true
        if ($dart.success) {
            Write-Success "✓ Record dart throw successful"
            Write-Result "Dart recorded: Score=$($dart.data.score), Multiplier=$($dart.data.multiplier)"
        } else {
            Write-Error "Record dart failed: $($dart.message)"
        }
    }

    Write-Test "GET /api/matches/{matchId}/darts (Anonymous)"
    $getDarts = Invoke-ApiCall -Method "GET" -Endpoint "/matches/$matchId/darts"
    if ($getDarts.success) {
        Write-Success "✓ Get match darts successful"
        Write-Result "Found $($getDarts.data.Count) darts"
    } else {
        Write-Error "Get darts failed"
    }

    Write-Test "GET /api/matches/{matchId}/darts/score (Anonymous)"
    $getScore = Invoke-ApiCall -Method "GET" -Endpoint "/matches/$matchId/darts/score"
    if ($getScore.success) {
        Write-Success "✓ Get match score successful"
        Write-Result "Match score retrieved"
    } else {
        Write-Error "Get score failed"
    }

    Write-Test "POST /api/matches/{matchId}/darts/undo (Undo - Requires Auth)"
    if ($token) {
        $undo = Invoke-ApiCall -Method "POST" -Endpoint "/matches/$matchId/darts/undo" -Body @{} -RequireAuth $true
        if ($undo) {
            Write-Success "✓ Undo dart successful"
            Write-Result "Last dart undone"
        } else {
            Write-Error "Undo dart failed"
        }
    }
}

# ==================== CLEANUP ====================
Write-Host "`n🗑️  CLEANUP" -ForegroundColor Cyan

if ($matchId -and $token) {
    Write-Test "DELETE /api/matches/{id}"
    $deleteMatch = Invoke-ApiCall -Method "DELETE" -Endpoint "/matches/$matchId" -RequireAuth $true
    if ($deleteMatch) {
        Write-Success "✓ Delete match successful"
        Write-Result "Match deleted"
    }
}

if ($tournamentId -and $token) {
    Write-Test "DELETE /api/tournaments/{id}"
    $deleteTournament = Invoke-ApiCall -Method "DELETE" -Endpoint "/tournaments/$tournamentId" -RequireAuth $true
    if ($deleteTournament) {
        Write-Success "✓ Delete tournament successful"
        Write-Result "Tournament deleted"
    }
}

# ==================== SUMMARY ====================
Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║                     TEST SUITE COMPLETE                        ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""
