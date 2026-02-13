# DartMaster Docker & Kubernetes Quick Reference

## Docker Compose Commands

### Start Services
```bash
# Start all services, rebuild if needed
docker-compose up --build

# Start in background
docker-compose up -d

# Start specific service
docker-compose up backend frontend

# Rebuild without starting
docker-compose build
```

### Stop Services
```bash
# Stop all services (keep data)
docker-compose stop

# Stop and remove containers
docker-compose down

# Stop and remove everything including data
docker-compose down -v
```

### View Status
```bash
# List all containers
docker-compose ps

# Show logs for all services
docker-compose logs

# Follow logs (real-time)
docker-compose logs -f

# Logs for specific service
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f db
```

### Access Containers
```bash
# Open shell in backend
docker-compose exec backend /bin/bash

# Open shell in frontend
docker-compose exec frontend /bin/sh

# Open MySQL shell
docker-compose exec db mysql -u root -proot

# Execute single command
docker-compose exec backend dotnet --info
```

### Manage Data
```bash
# List all volumes
docker volume ls

# Inspect MySQL volume
docker volume inspect dartmaster_mysql_data

# Remove volume (delete database data)
docker-compose down -v
```

---

## Kubernetes Commands

### Namespace Management
```bash
# Create namespace
kubectl create namespace dartmaster

# List namespaces
kubectl get namespaces

# Switch to dartmaster namespace
kubectl config set-context --current --namespace=dartmaster

# Delete namespace (removes all resources)
kubectl delete namespace dartmaster
```

### Pod Management
```bash
# List pods
kubectl get pods -n dartmaster

# List pods with details
kubectl get pods -n dartmaster -o wide

# Watch pod status (live)
kubectl get pods -n dartmaster -w

# Describe pod
kubectl describe pod <pod-name> -n dartmaster

# View pod logs
kubectl logs -f <pod-name> -n dartmaster

# Execute command in pod
kubectl exec -it <pod-name> -n dartmaster -- /bin/bash

# View previous logs (crash logs)
kubectl logs <pod-name> -n dartmaster --previous
```

### Deployment Management
```bash
# List deployments
kubectl get deployments -n dartmaster

# Describe deployment
kubectl describe deployment dartmaster-api -n dartmaster

# Scale deployment
kubectl scale deployment dartmaster-api --replicas=5 -n dartmaster

# Update deployment image
kubectl set image deployment/dartmaster-api \
  api=dartmaster-backend:v2 -n dartmaster

# Rollout status
kubectl rollout status deployment/dartmaster-api -n dartmaster

# Rollback to previous version
kubectl rollout undo deployment/dartmaster-api -n dartmaster

# View rollout history
kubectl rollout history deployment/dartmaster-api -n dartmaster
```

### Service Management
```bash
# List services
kubectl get svc -n dartmaster

# List services with endpoints
kubectl get svc -n dartmaster -o wide

# Describe service
kubectl describe svc api-service -n dartmaster

# Port forward to local machine
kubectl port-forward svc/api-service 5146:5146 -n dartmaster

# Get external IP (for LoadBalancer)
kubectl get svc frontend-service -n dartmaster
```

### Configuration Management
```bash
# List ConfigMaps
kubectl get configmap -n dartmaster

# View ConfigMap
kubectl describe cm dartmaster-config -n dartmaster

# Edit ConfigMap
kubectl edit configmap dartmaster-config -n dartmaster

# List Secrets
kubectl get secrets -n dartmaster

# View Secret (base64 encoded)
kubectl get secret dartmaster-secret -o yaml -n dartmaster
```

### Auto-Scaling (HPA)
```bash
# List HPA
kubectl get hpa -n dartmaster

# Describe HPA
kubectl describe hpa dartmaster-api-hpa -n dartmaster

# Watch HPA status
kubectl get hpa -n dartmaster -w
```

### Events & Troubleshooting
```bash
# Get recent events
kubectl get events -n dartmaster --sort-by='.lastTimestamp'

# Watch events (live)
kubectl get events -n dartmaster -w

# Top nodes
kubectl top nodes

# Top pods
kubectl top pods -n dartmaster

# Get cluster info
kubectl cluster-info

# Describe node
kubectl describe node <node-name>
```

### Resource Management
```bash
# List all resources
kubectl get all -n dartmaster

# Delete resource by name
kubectl delete pod <pod-name> -n dartmaster
kubectl delete deployment dartmaster-api -n dartmaster

# Delete by manifest file
kubectl delete -f k8s/backend-deployment.yaml

# Delete all in namespace
kubectl delete all -n dartmaster
```

### Apply & Update
```bash
# Apply single file
kubectl apply -f k8s/namespace.yaml

# Apply all files in directory
kubectl apply -f k8s/

# Apply with force replace
kubectl replace --force -f k8s/backend-deployment.yaml

# Check what would be applied (dry-run)
kubectl apply -f k8s/ --dry-run=client
```

---

## Deployment Workflow

### Local Development (Docker Desktop Kubernetes)

Docker Desktop inkluderar redan Kubernetes - verwende det istället för minikube!

```bash
# 1. Enable Kubernetes
# Docker Desktop Settings → Kubernetes → Enable Kubernetes → Restart

# 2. Build images
docker build -t dartmaster-backend:latest -f Dockerfile.backend .
docker build -t dartmaster-frontend:latest -f Dockerfile.frontend .

# 3. Deploy using script
cd k8s
bash deploy.sh deploy

# 4. Port-forward services (i separata terminals)
kubectl port-forward svc/frontend-service 5173:5173 -n dartmaster &
kubectl port-forward svc/api-service 5146:5146 -n dartmaster &
kubectl port-forward svc/mysql-service 3306:3306 -n dartmaster &

# 5. Access
# Frontend: http://localhost:5173
# Backend: http://localhost:5146/api
# Database: localhost:3306

# 6. Monitor
kubectl get pods -n dartmaster -w

# 7. Cleanup
bash deploy.sh delete
```

### Production Deployment (AKS/GKE/EKS)

```bash
# 1. Configure kubectl to cluster
# AKS: az aks get-credentials ...
# GKE: gcloud container clusters get-credentials ...

# 2. Push images to registry
docker tag dartmaster-backend:latest <registry>/dartmaster-backend:latest
docker push <registry>/dartmaster-backend:latest
docker push <registry>/dartmaster-frontend:latest

# 3. Update image references in k8s files
kubectl set image deployment/dartmaster-api \
  api=<registry>/dartmaster-backend:latest -n dartmaster

# 4. Apply manifests
kubectl apply -f k8s/

# 5. Monitor deployment
kubectl rollout status deployment/dartmaster-api -n dartmaster

# 6. Get external endpoints
kubectl get svc -n dartmaster
```

---

## Common Patterns

### Port Forwarding

```bash
# Forward backend
kubectl port-forward svc/api-service 5146:5146 -n dartmaster

# Forward database
kubectl port-forward svc/mysql-service 3306:3306 -n dartmaster

# Forward multiple (separate terminals)
kubectl port-forward svc/frontend-service 80:80 -n dartmaster &
kubectl port-forward svc/api-service 5146:5146 -n dartmaster &
```

### Debugging Pod

```bash
# 1. Get pod name
kubectl get pods -n dartmaster

# 2. View logs
kubectl logs -f <pod-name> -n dartmaster

# 3. Access shell
kubectl exec -it <pod-name> -n dartmaster -- /bin/bash

# 4. Check events
kubectl describe pod <pod-name> -n dartmaster
```

### Updating Image

```bash
# Method 1: Edit deployment
kubectl edit deployment dartmaster-api -n dartmaster
# (Change "image:" field)

# Method 2: Set image command
kubectl set image deployment/dartmaster-api \
  api=dartmaster-backend:v2.0 -n dartmaster

# Method 3: Edit manifest and apply
# Edit backend-deployment.yaml, then:
kubectl apply -f k8s/backend-deployment.yaml
```

### Scaling Application

```bash
# Set exact number of replicas
kubectl scale deployment dartmaster-api --replicas=5 -n dartmaster

# HPA will override manual scaling
# View current HPA status
kubectl get hpa -n dartmaster
```

### Database Operations

```bash
# Connect to MySQL
kubectl exec -it deployment/mysql -n dartmaster -- \
  mysql -u root -proot DartMaster

# Backup database
kubectl exec deployment/mysql -n dartmaster -- \
  mysqldump -u root -proot DartMaster > backup.sql

# Restore database
kubectl exec -i deployment/mysql -n dartmaster -- \
  mysql -u root -proot DartMaster < backup.sql
```

---

## Container Sizes

```bash
# List image sizes
docker images dartmaster-*

# View image layers and size
docker history dartmaster-backend:latest
docker history dartmaster-frontend:latest

# Typical sizes:
# - Backend: ~300MB (optimized from ~1.2GB)
# - Frontend: ~50MB (optimized from ~900MB)
# - MySQL: ~400MB
```

---

## Environment Variables Quick List

### Backend
- `ConnectionStrings__DefaultConnection` - Database connection string
- `ASPNETCORE_ENVIRONMENT` - Production/Development
- `ASPNETCORE_URLS` - Listening URL (http://+:5146)

### Frontend
- `REACT_APP_API_URL` - Backend API URL

### Database
- `MYSQL_ROOT_PASSWORD` - Root password
- `MYSQL_DATABASE` - Database name
- `MYSQL_USER` - Database user
- `MYSQL_PASSWORD` - Database user password

---

## Useful Aliases

Add to `~/.bashrc` or `~/.zshrc`:

```bash
# Kubernetes
alias k='kubectl'
alias kg='kubectl get'
alias kgp='kubectl get pods -n dartmaster'
alias kgs='kubectl get svc -n dartmaster'
alias kgd='kubectl get deployment -n dartmaster'
alias kl='kubectl logs -f'
alias ke='kubectl exec -it'
alias kdel='kubectl delete'

# Docker
alias dc='docker-compose'
alias dcu='docker-compose up'
alias dcd='docker-compose down'
alias dcl='docker-compose logs -f'

# Useful combinations
alias k8s-watch='watch "kubectl get all -n dartmaster -o wide"'
alias minikube-dashboard='minikube dashboard'
```

Usage:
```bash
kg pod -n dartmaster     # kubectl get pods -n dartmaster
kl dartmaster-api        # kubectl logs -f dartmaster-api -n dartmaster
dc up --build            # docker-compose up --build
```

---

## Resources

- [Docker Documentation](https://docs.docker.com/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [kubectl Cheatsheet](https://kubernetes.io/docs/reference/kubectl/cheatsheet/)
- [minikube Documentation](https://minikube.sigs.k8s.io/)

---

## Support Files

- [Docker Guide](./DOCKER.md) - Comprehensive Docker documentation
- [K8s Deployment Guide](./k8s/DEPLOYMENT.md) - Production deployment instructions
- [README](./README.md) - Project overview and quick start
