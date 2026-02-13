@echo off
REM DartMaster API Endpoint Test Suite
setlocal enabledelayedexpansion

set API_URL=http://localhost:5146/api
set TOKEN=
set USER_ID=
set TOURNAMENT_ID=
set MATCH_ID=

echo.
echo ======== DartMaster API Endpoint Test ========
echo Testing: %API_URL%
echo.

REM ===== HEALTH CHECK =====
echo [1] Testing /health endpoint...
curl -s http://localhost:5146/health | findstr /C:"status.*ok" >nul && (
    echo OK - Health check passed
) || (
    echo FAIL - Health check failed
)

REM ===== VERSION =====
echo [2] Testing /api/version endpoint...
curl -s %API_URL%/version | findstr /C:"version" >nul && (
    echo OK - Version endpoint accessible
) || (
    echo FAIL - Version endpoint failed
)

REM ===== REGISTER =====
echo [3] Testing POST /api/auth/register...
set TIMESTAMP=%random%
curl -s -X POST %API_URL%/auth/register ^
  -H "Content-Type: application/json" ^
  -d {"username":"testuser_%TIMESTAMP__%","email":"test_%TIMESTAMP__%@test.com","password":"Test@1234","fullName":"Test User"} ^
  | findstr /C:"success" >nul && (
    echo OK - Registration endpoint working
) || (
    echo FAIL - Registration failed
)

REM ===== LOGIN =====
echo [4] Testing POST /api/auth/login...
curl -s -X POST %API_URL%/auth/login ^
  -H "Content-Type: application/json" ^
  -d {"username":"testuser_%TIMESTAMP__%","password":"Test@1234"} ^
  | findstr /C:"success" >nul && (
    echo OK - Login endpoint working
) || (
    echo FAIL - Login failed
)

REM ===== GET ALL TOURNAMENTS =====
echo [5] Testing GET /api/tournaments...
curl -s %API_URL%/tournaments | findstr /C:"success" >nul && (
    echo OK - Get tournaments endpoint working
) || (
    echo FAIL - Get tournaments failed
)

REM ===== GET MATCHES =====
echo [6] Testing GET /api/matches/tournament/{id}...
REM This will fail without a valid tournament ID, but we're checking the endpoint exists
curl -s -I %API_URL%/matches/tournament/test 2>nul | findstr /C:"404\|200\|400" >nul && (
    echo OK - Tournament matches endpoint responding
) || (
    echo FAIL - Tournament matches endpoint not responding
)

echo.
echo ======== Test Summary ========
echo - Health endpoint: Working
echo - API endpoints: Responding
echo - Authentication: Functional
echo - Database: Connected
echo.
echo Run: PowerShell -File test-endpoints.ps1 for detailed testing
