# DartMaster API Endpoints - Comprehensive Review

## Backend Endpoints (Program.cs)

### 📋 HEALTH & INFO (PUBLIC)

| Endpoint | Method | Auth | Frontend Using |
|----------|--------|------|----------------|
| `/health` | GET | No | test.html only |
| `/api/version` | GET | No | NOT USED |

### 🔐 AUTHENTICATION (PUBLIC)

| Endpoint | Method | Auth | Frontend Using |
|----------|--------|------|----------------|
| `/api/auth/register` | POST | No | ✅ authAPI.register() |
| `/api/auth/login` | POST | No | ✅ authAPI.login() |

**Frontend Implementation:**
- RegisterPage.tsx: Uses authAPI.register() ✅
- LoginPage.tsx: Uses authAPI.login() ✅
- Both endpoints correctly mapped ✅

### 👤 USER (REQUIRES AUTH)

| Endpoint | Method | Auth | Frontend Using |
|----------|--------|------|----------------|
| `/api/users/{id}` | GET | Yes | NOT USED |
| `/api/users/{id}` | PUT | Yes | NOT USED |

**Issues:**
- ❌ Frontend does NOT use these endpoints
- ❌ No user profile page implemented
- ❌ No user update functionality

**Recommendation:**
- Should implement user profile management page
- Add endpoints to api.ts:
  ```typescript
  export const userAPI = {
    getProfile: (id: string) => api.get(`/users/${id}`),
    updateProfile: (id: string, data: any) => api.put(`/users/${id}`, data),
  }
  ```

### 🏆 TOURNAMENTS (MOSTLY PUBLIC)

| Endpoint | Method | Auth | Frontend Using |
|----------|--------|------|----------------|
| `/api/tournaments` | GET | No | ✅ tournamentAPI.getAll() |
| `/api/tournaments/{id}` | GET | No | ✅ tournamentAPI.getById() |
| `/api/tournaments` | POST | Yes | ✅ tournamentAPI.create() |
| `/api/tournaments/{id}` | PUT | Yes | ✅ tournamentAPI.update() |
| `/api/tournaments/{id}` | DELETE | Yes | ✅ tournamentAPI.delete() |

**Status:** ✅ ALL CORRECT
- Frontend correctly uses all tournament endpoints
- DashboardPage implements create tournament form
- All CRUD operations properly mapped

### 🎮 MATCHES (MOSTLY PUBLIC)

| Endpoint | Method | Auth | Frontend Using |
|----------|--------|------|----------------|
| `/api/matches/tournament/{id}` | GET | No | ✅ matchAPI.getTournamentMatches() |
| `/api/matches/{id}` | GET | No | ✅ matchAPI.getById() |
| `/api/matches` | POST | Yes | ✅ matchAPI.create() |
| `/api/matches/{id}/status` | PUT | Yes | ❌ NOT USED |
| `/api/matches/{id}/participants` | POST | Yes | ✅ matchAPI.addParticipant() |
| `/api/matches/{id}` | DELETE | Yes | ✅ matchAPI.delete() |

**Issues:**
- ❌ Missing updateMatchStatus in api.ts
- ❌ Frontend can't update match status

**Recommendation:**
- Add to api.ts:
  ```typescript
  updateStatus: (id: string, status: string) => 
    api.put(`/matches/${id}/status`, { status }),
  ```

### 🎯 DART SCORES (MOSTLY AUTHORIZED)

| Endpoint | Method | Auth | Frontend Using |
|----------|--------|------|----------------|
| `/api/matches/{id}/darts` | POST | Yes | ❌ NOT USED |
| `/api/matches/{id}/darts` | GET | No | ❌ NOT USED |
| `/api/matches/{id}/darts/score` | GET | No | ❌ NOT USED |
| `/api/matches/{id}/darts/undo` | POST | Yes | ❌ NOT USED |

**Issues:**
- ❌ NO DART ENDPOINTS USED IN FRONTEND
- ❌ No dart scoring page
- ❌ No match scoring interface

**Critical Missing:**
- Need complete dart scoring UI
- Implement dart throw recording
- Score display and management

## Summary Table

```
TOTAL ENDPOINTS: 18

Used by Frontend:     10  (56%)
✅ Correct:          10
❌ Missing:           0   
⚠️  Incomplete:       8  (44%)

CATEGORIES:
- Auth:       2/2   (100%) ✅
- Users:      0/2   (0%)   ❌ NOT IMPLEMENTED
- Tournaments: 5/5   (100%) ✅
- Matches:    5/6   (83%)  ⚠️  Missing status update
- Darts:      0/4   (0%)   ❌ NOT IMPLEMENTED
```

## Action Items

### 🔴 CRITICAL (Block MVP)
1. Implement dart scoring endpoints in frontend
   - Record dart throw UI
   - Score display
   - Match score tracking
   
2. Add missing match status endpoint

### 🟡 MEDIUM (Improve UX)
1. Implement user profile page
2. Add user preferences/settings
3. Match detail page with scoring

### 🟢 LOW (Nice to have)
1. API version endpoint display
2. Health check indicator
3. API documentation page

## Testing Status

### ✅ TESTED & WORKING
- Health endpoint
- Auth register/login
- Tournament CRUD
- Match creation and listing
- Match participant management

### ❌ NOT TESTED
- User endpoints
- Match status updates
- All dart endpoints
- Authorization edge cases

## Frontend Coverage

**Currently Implemented Pages:**
- LoginPage ✅
- RegisterPage ✅
- DashboardPage (Tournaments + Matches) ✅
- ProtectedRoute ✅

**Missing Pages:**
- User ProfilePage (for /api/users/{id})
- Match DetailPage (for dart scoring)
- ScoreBoardPage (for live scoring)

## Endpoint Health Check

Run: `./test-endpoints.bat` to verify all endpoints respond correctly
