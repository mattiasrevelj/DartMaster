# DartMaster Docker Guide

Complete guide for developing, building, and deploying DartMaster with Docker.

## Quick Start

### Prerequisites
- Docker Desktop installed (https://www.docker.com/products/docker-desktop)
- Docker Compose included with Docker Desktop

### Run Full Stack
```bash
cd /path/to/DartMaster
docker-compose up --build
```

Access:
- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5146/api
- **MySQL**: localhost:3306

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f db
```

### Stop Services
```bash
docker-compose down

# Also remove volumes (database data)
docker-compose down -v
```

## Docker Images

### Backend Image (Dockerfile.backend)

**Base Image**: `mcr.microsoft.com/dotnet/aspnet:8.0`

**Multi-stage Build:**
1. **Builder Stage** - Uses .NET SDK 8.0 to compile
2. **Publish Stage** - Creates optimized release build
3. **Runtime Stage** - Minimal aspnet image with only runtime dependencies

**Build locally:**
```bash
docker build -t dartmaster-backend:latest -f Dockerfile.backend .
```

**Run standalone:**
```bash
docker run -p 5146:5146 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Port=3306;Database=DartMaster;User=dartmaster;Password=dartmaster;" \
  dartmaster-backend:latest
```

### Frontend Image (Dockerfile.frontend)

**Base Image**: `node:18-alpine` → `alpine:latest`

**Multi-stage Build:**
1. **Builder Stage** - Uses Node 18 Alpine to build React app
2. **Production Stage** - Minimal Alpine image with just the built app
3. **Server** - Uses `serve` package to serve static files

**Build locally:**
```bash
docker build -t dartmaster-frontend:latest -f Dockerfile.frontend .
```

**Run standalone:**
```bash
docker run -p 5173:5173 \
  -e REACT_APP_API_URL="http://localhost:5146/api" \
  dartmaster-frontend:latest
```

## Development Workflow

### 1. Modify Backend Code

**With docker-compose:**
```bash
# Edit backend code
# Rebuild and restart
docker-compose up --build backend

# Or just rebuild
docker-compose build backend
docker-compose up backend
```

**Troubleshooting:**
- Clear build cache: `docker-compose build --no-cache backend`
- Check logs: `docker-compose logs backend`

### 2. Modify Frontend Code

**With docker-compose:**
```bash
# Edit React code
# Rebuild and restart
docker-compose up --build frontend

# For development with hot reload, consider running locally:
cd frontend
npm install
npm run dev
```

**Local development (faster):**
```bash
# Terminal 1: Start services
docker-compose up db backend

# Terminal 2: Run frontend locally with hot reload
cd frontend
npm install
npm run dev
# Visit http://localhost:5173
```

### 3. Database Changes

**Database is persistent** in `mysql_data` volume:
```bash
# View all volumes
docker volume ls

# Inspect volume
docker volume inspect dartmaster_mysql_data

# Reset database
docker-compose down -v  # Removes database data
docker-compose up       # Creates fresh database
```

## Build Optimization

### Image Size Comparison

**Before optimization:**
- Backend: ~1.2GB (with SDK)
- Frontend: ~900MB (full Node image)

**After multi-stage optimization:**
- Backend: ~300MB (runtime only)
- Frontend: ~50MB (Alpine + static files)

**Layers in optimized images:**
```bash
# View backend image layers
docker history dartmaster-backend:latest

# View frontend image layers
docker history dartmaster-frontend:latest
```

## Docker Compose Configuration

### Services

#### Database (MySQL)
```yaml
db:
  image: mysql:8.0
  ports:
    - "3306:3306"
  environment:
    MYSQL_ROOT_PASSWORD: root
    MYSQL_DATABASE: DartMaster
    MYSQL_USER: dartmaster
    MYSQL_PASSWORD: dartmaster
  volumes:
    - mysql_data:/var/lib/mysql
  healthcheck:
    test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
```

Health checks ensure backend waits for database readiness.

#### Backend API
```yaml
backend:
  build:
    context: .
    dockerfile: Dockerfile.backend
  ports:
    - "5146:5146"
  environment:
    ConnectionStrings__DefaultConnection: "Server=db;..."
    ASPNETCORE_ENVIRONMENT: Development
  depends_on:
    db:
      condition: service_healthy
```

#### Frontend
```yaml
frontend:
  build:
    context: .
    dockerfile: Dockerfile.frontend
  ports:
    - "5173:5173"
  environment:
    REACT_APP_API_URL: "http://localhost:5146/api"
  depends_on:
    - backend
```

### Networks & Volumes

**Network:**
- `dartmaster-network` - bridge network for service communication
- Services communicate via hostnames (e.g., `db`, `backend`, `frontend`)

**Volume:**
- `mysql_data` - Persistent MySQL storage
- Survives `docker-compose down`
- Removed only with `docker-compose down -v`

## Networking for Development

### Service-to-Service Communication

**Within Docker:**
```
Frontend (port 5173) → Backend (called as 'backend' or 'api-service')
Backend (port 5146) → Database (called as 'db')
```

**From Host Machine:**
```
http://localhost:5173 (Frontend)
http://localhost:5146/api (Backend)
localhost:3306 (Database)
```

### Port Mapping

```bash
# View all port mappings
docker-compose ps

# Custom port mapping example:
# Change docker-compose.yml:
services:
  backend:
    ports:
      - "8080:5146"  # Host port 8080 → Container port 5146
```

## Debugging

### Access Container Shell

```bash
# Backend
docker-compose exec backend /bin/bash

# Frontend
docker-compose exec frontend /bin/sh

# Database
docker-compose exec db /bin/bash
```

### Execute Commands in Container

```bash
# Run .NET CLI command
docker-compose exec backend dotnet --info

# Run npm command
docker-compose exec frontend npm list

# Run MySQL command
docker-compose exec db mysql -u root -proot -e "SELECT * FROM Users;"
```

### View Container Processes

```bash
# Show running processes
docker-compose top backend
docker-compose top frontend
docker-compose top db
```

### Inspect Container

```bash
# View container configuration
docker inspect dartmaster-api

# View container network
docker network inspect dartmaster_dartmaster-network
```

## Performance Tips

### 1. Use .dockerignore and .gitignore

Create `.dockerignore`:
```
.git
.gitignore
node_modules
dist
build
bin
obj
.vs
.vscode
README.md
```

### 2. Layer Caching

Place frequently changing files at end of Dockerfile:
```dockerfile
# Good - stable layers first
FROM mcr.microsoft.com/dotnet/sdk:8.0
COPY *.csproj ./
RUN dotnet restore
COPY . .  # Changes here most frequent
RUN dotnet publish -c Release
```

### 3. Minimize Image Size

- Use Alpine base images (50MB vs 900MB)
- Multi-stage builds (separate build and runtime)
- Remove build tools from final image

### 4. Development Tips

- Use volumes for source code (hot reload)
- Keep container logs visible
- Use health checks
- Implement dependency ordering

## Common Issues

### "Cannot connect to database"

**Solution:**
```bash
# Ensure database is healthy
docker-compose ps
docker-compose logs db

# Rebuild with fresh database
docker-compose down -v
docker-compose up
```

### "Port already in use"

**Solution - Change port in docker-compose.yml:**
```yaml
ports:
  - "5147:5146"  # Use 5147 instead of 5146
```

Or kill process using port:
```bash
# Windows
netstat -ano | findstr :5146
taskkill /PID <PID> /F

# macOS/Linux
lsof -i :5146
kill -9 <PID>
```

### "Image build fails"

**Solution:**
```bash
# Clear cache and rebuild
docker-compose build --no-cache

# Check Dockerfile syntax
docker build -f Dockerfile.backend --no-cache .

# View build output
docker buildx build --progress=plain -f Dockerfile.backend .
```

### "Frontend not connecting to API"

**Check API URL:**
- Within Docker: Backend called `backend` service
- From host browser: Use `http://localhost:5146`
- CORS may need to be enabled in backend

**Solution in docker-compose.yml:**
```yaml
frontend:
  environment:
    REACT_APP_API_URL: "http://localhost:5146/api"
```

### "High CPU/Memory usage"

**Check resource usage:**
```bash
docker stats

# Limit resources in docker-compose.yml:
backend:
  deploy:
    resources:
      limits:
        cpus: '1'
        memory: 512M
```

## Production Checklist

Before deploying to production:

- [ ] Remove `DEBUG` environment variables
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Use strong database passwords (not in env, use secrets)
- [ ] Configure HTTPS/TLS
- [ ] Set up health checks
- [ ] Configure logging (not just console)
- [ ] Set resource limits
- [ ] Use non-root user in Dockerfile (add USER directive)
- [ ] Scan images for vulnerabilities
- [ ] Implement registry authentication
- [ ] Set up monitoring and alerting

## Docker Image Scanning

Scan for vulnerabilities:
```bash
# Using Trivy
trivy image dartmaster-backend:latest
trivy image dartmaster-frontend:latest

# Using Docker Scout (Docker Desktop)
docker scout cves dartmaster-backend:latest
```

## Registry Operations

### Push to Docker Hub

```bash
# Tag image
docker tag dartmaster-backend:latest myusername/dartmaster-backend:latest

# Login
docker login

# Push
docker push myusername/dartmaster-backend:latest
```

### Push to Azure Container Registry

```bash
# Login to ACR
az acr login --name myregistry

# Tag image
docker tag dartmaster-backend:latest myregistry.azurecr.io/dartmaster-backend:latest

# Push
docker push myregistry.azurecr.io/dartmaster-backend:latest
```

## References

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Reference](https://docs.docker.com/compose/compose-file/)
- [Best Practices for Python Docker](https://docs.docker.com/language/python/build-images/)
- [Multi-stage Builds](https://docs.docker.com/build/building/multi-stage/)
