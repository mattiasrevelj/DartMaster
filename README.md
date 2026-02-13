# 🎯 DartMaster - Tournament Management System

A full-stack dart tournament management platform built with .NET 8 backend and React TypeScript frontend.

## 📋 Project Status: MVP Ready

**Completed:**
- ✅ C# ASP.NET Core Minimal API backend (15 endpoints)
- ✅ Entity Framework Core 8 with MySQL database (12 tables)
- ✅ JWT authentication with BCrypt password hashing
- ✅ 15 comprehensive unit tests (TournamentService, MatchService, DartScoreService)
- ✅ React 18 TypeScript frontend with Vite
- ✅ Authentication system (Login/Register)
- ✅ Tournament management dashboard
- ✅ Match viewing and participation
- ✅ Docker containerization (multi-stage builds)
- ✅ Kubernetes deployment (local minikube + production ready)

**Planned:**
- 🔜 Real-time score updates (SignalR/WebSocket)
- 🔜 Live scoreboard with spectator mode
- 🔜 Advanced tournament formats (knockout, group stage)
- 🔜 Player statistics and rankings
- 🔜 CI/CD pipeline (GitHub Actions)

## 🏗️ Architecture

### Backend (.NET 8)
```
backend/
├── Models/           # Entity definitions (Users, Tournaments, Matches, etc.)
├── Data/             # Database context and migrations
├── Services/         # Business logic (Authentication, Tournament, Match, DartScore)
├── Program.cs        # ASP.NET Core configuration and endpoints
└── appsettings.json  # Configuration
```

**Technology Stack:**
- ASP.NET Core 8.0 (Minimal API)
- Entity Framework Core 8.0.11 with Pomelo MySQL
- JWT Bearer Authentication
- BCrypt password hashing
- Serilog logging
- xUnit testing framework with Moq

**API Endpoints (15 total):**

Tournament Management (5):
- `GET /api/tournaments` - List all tournaments
- `GET /api/tournaments/{id}` - Get tournament details
- `POST /api/tournaments` - Create tournament (admin)
- `PUT /api/tournaments/{id}` - Update tournament (admin)
- `DELETE /api/tournaments/{id}` - Delete tournament (admin)

Match Management (6):
- `GET /api/matches?tournamentId={id}` - Get tournament matches
- `GET /api/matches/{id}` - Get match details
- `POST /api/matches` - Create match (admin)
- `PUT /api/matches/{id}/status` - Update match status (admin)
- `POST /api/matches/{id}/participants` - Add participant
- `DELETE /api/matches/{id}` - Delete match (admin)

Dart Score Recording (4):
- `POST /api/matches/{id}/darts` - Record dart throw
- `GET /api/matches/{id}/darts` - Get all darts in match
- `GET /api/matches/{id}/score` - Get current match score
- `POST /api/matches/{id}/darts/undo` - Undo last dart

Authentication (2):
- `POST /api/users/register` - Register new user
- `POST /api/users/login` - Login user

### Frontend (React TypeScript)
```
frontend/
├── src/
│   ├── components/    # Reusable UI components (ProtectedRoute)
│   ├── pages/        # Page components (Login, Register, Dashboard)
│   ├── services/     # API client with axios
│   ├── styles/       # CSS files (responsive design)
│   ├── App.tsx       # Main router
│   └── main.tsx      # Entry point
├── package.json      # Dependencies
├── tsconfig.json     # TypeScript configuration
├── vite.config.ts    # Vite build configuration
└── index.html        # HTML template
```

**Technology Stack:**
- React 18
- TypeScript 5
- Vite
- React Router DOM 6
- Axios
- CSS3 with responsive design

### Database Schema
- **Users** - User accounts with password hashing
- **RefreshTokens** - JWT refresh token management
- **Tournaments** - Tournament instances with status tracking
- **TournamentParticipants** - User tournament registrations
- **TournamentGroups** - Group stage organization
- **Matches** - Individual matches within tournaments
- **MatchParticipants** - Player participation in matches
- **DartThrows** - Recording individual dart throws
- **MatchConfirmations** - Match result confirmations
- **PlayerStatistics** - Player performance metrics
- **Notifications** - System notifications
- **NotificationSubscriptions** - User notification preferences

## 🚀 Getting Started

### Prerequisites
- .NET 8.0+
- Node.js 16+
- MySQL 8.0+
- Docker (for containerization)

### Backend Setup

```bash
cd backend
# Restore dependencies
dotnet restore

# Run migrations and start server
dotnet run --project DartMaster.Api.csproj
```

Server runs on: `http://localhost:5146`

**Database Configuration (appsettings.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;User=dartmaster;Password=dartmaster;Database=DartMaster"
  }
}
```

### Frontend Setup

```bash
cd frontend
# Install dependencies
npm install

# Start development server
npm run dev
```

Frontend runs on: `http://localhost:5173`

### Running Tests

```bash
cd backend.tests
dotnet test
```

All 17 tests should pass ✅

## 📊 Database

**Connection Details:**
- Host: localhost:3306
- Database: DartMaster
- User: dartmaster
- Password: dartmaster

**Docker Container:**
```bash
docker run -d --name dartmaster-db \
  -p 3306:3306 \
  -e MYSQL_ROOT_PASSWORD=root \
  -e MYSQL_DATABASE=DartMaster \
  -e MYSQL_USER=dartmaster \
  -e MYSQL_PASSWORD=dartmaster \
  mariadb:10.5
```

## 🔑 Authentication Flow

1. **Registration**: User creates account with username, email, password, full name
2. **Login**: User provides credentials, receives JWT token + refresh token
3. **Protected Routes**: Frontend stores token in localStorage, includes in API requests
4. **Server**: Validates JWT token on protected endpoints, returns 401 if invalid

## 🧪 Testing

### Unit Tests (15 tests, 100% passing)

**TournamentServiceTests** (5 tests):
- ✅ Create tournament with valid data
- ✅ Reject tournament with past start date
- ✅ Get all tournaments list
- ✅ Update tournament (admin only)
- ✅ Delete tournament (admin only)

**MatchServiceTests** (5 tests):
- ✅ Create match with valid data
- ✅ Reject match creation (non-admin)
- ✅ Add participant to match
- ✅ Reject duplicate participant
- ✅ Get tournament matches

**DartScoreServiceTests** (5 tests):
- ✅ Record dart throw with validation
- ✅ Reject dart without participant
- ✅ Reject invalid points (0-180)
- ✅ Record dart without double at finish
- ✅ Get match score and undo dart

### Manual Testing
- Health endpoint: `GET http://localhost:5146/health`
- User registration tested with valid credentials
- Login returns JWT token
- Protected endpoints require valid token

## 📝 API Examples

### Register User
```bash
curl -X POST http://localhost:5146/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test@1234",
    "fullName": "Test User"
  }'
```

### Login
```bash
curl -X POST http://localhost:5146/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "Test@1234"
  }'
```

### Create Tournament
```bash
curl -X POST http://localhost:5146/api/tournaments \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Friday Night Darts",
    "description": "Weekly tournament",
    "startDate": "2026-02-20T19:00:00Z",
    "maxPlayers": 16
  }'
```

### Get Tournaments
```bash
curl http://localhost:5146/api/tournaments
```

## 🐳 Docker & Kubernetes

### Docker Compose (Local Development)

Quick start with everything:
```bash
docker-compose up --build
```

Services will be available at:
- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5146/api
- **MySQL**: localhost:3306

Stop services:
```bash
docker-compose down
```

**Services in compose:**
- `db` - MySQL 8.0 with persistent storage
- `backend` - .NET 8 API (5146)
- `frontend` - React with Vite (5173)

### Kubernetes Deployment

#### Local Development (Docker Desktop Kubernetes)

Kubernetes är redan inbyggt i Docker Desktop - ingen minikube behövs!

1. **Enable Kubernetes in Docker Desktop**
   - Settings → Kubernetes → Enable Kubernetes
   - Restart Docker Desktop

2. **Deploy using script**
   ```bash
   cd k8s
   bash deploy.sh deploy
   ```

3. **Access services**
   ```bash
   # Frontend (port-forward i terminal)
   kubectl port-forward svc/frontend-service 5173:5173 -n dartmaster
   # Browser: http://localhost:5173
   
   # Backend API
   kubectl port-forward svc/api-service 5146:5146 -n dartmaster
   # API: http://localhost:5146/api
   
   # Database
   kubectl port-forward svc/mysql-service 3306:3306 -n dartmaster
   # Connect: localhost:3306
   ```

4. **Monitor pods**
   ```bash
   kubectl get pods -n dartmaster -w
   kubectl logs -f deployment/dartmaster-api -n dartmaster
   ```

5. **Cleanup**
   ```bash
   bash deploy.sh delete
   ```

#### Production (AKS, GKE, EKS)

See [Kubernetes Deployment Guide](./k8s/DEPLOYMENT.md) for:
- Azure Kubernetes Service (AKS) setup
- Google Cloud GKE deployment
- AWS EKS integration
- Production best practices
- Monitoring and troubleshooting
- Ingress and DNS setup

**Key K8s components:**
- Deployments: Backend (3 replicas), Frontend (2 replicas), MySQL (1 replica)
- Services: Backend (ClusterIP), Frontend (LoadBalancer), MySQL (Headless)
- ConfigMap: Application settings
- Secret: Database credentials
- HPA: Auto-scaling based on CPU/Memory
- PVC: MySQL persistent storage (10Gi default)

**Auto-scaling:**
- Backend: 3-10 replicas (70% CPU / 80% Memory threshold)
- Frontend: 2-5 replicas (75% CPU threshold)

## 🔄 Development Workflow

1. **Backend Changes**: Push code, run tests, Docker builds automatically
2. **Frontend Changes**: Push code, npm build creates static files
3. **Database**: Migrations run automatically on app startup
4. **CI/CD**: GitHub Actions (planned) for automated testing and deployment

## 🤝 Git Commits

**Recent commits:**
- `3b73ecc` - React TypeScript frontend with auth and dashboard
- `89de829` - Comprehensive unit tests for all services
- `a4d4aa7` - Dart Score API endpoints
- `7c0d837` - Match API endpoints
- `d8b56c0` - Tournament API endpoints

## 📚 Documentation

- [Backend README](./backend/README.md)
- [Frontend README](./frontend/README.md)
- [Database Schema](./backend/Models/Entities.cs)

## 🎯 Next Steps

1. **Install Node.js** - Required for frontend development
2. **Set up Docker** - For containerization
3. **Deploy to Test Environment** - Validate full stack
4. **WebSocket Integration** - Real-time score updates
5. **Mobile App** - React Native client
6. **CI/CD Pipeline** - GitHub Actions automation

## 📞 Support

For issues or questions, please create an issue on GitHub:
https://github.com/mattiasrevelj/DartMaster/issues

## 📄 License

MIT License - See LICENSE file for details

---

**Built with ❤️ for dart tournament management**
