# DartMaster Kubernetes Deployment Guide

## Prerequisites

Before deploying to Kubernetes, ensure you have:

1. **kubectl** - Kubernetes command-line tool
   ```bash
   # macOS
   brew install kubectl
   
   # Windows (Chocolatey)
   choco install kubernetes-cli
   
   # Or download from: https://kubernetes.io/docs/tasks/tools/
   ```

2. **Docker** - Container runtime
   ```bash
   # macOS: https://docs.docker.com/desktop/install/mac-install/
   # Windows: https://docs.docker.com/desktop/install/windows-install/
   # Linux: https://docs.docker.com/engine/install/
   ```

3. **minikube** (for local testing) or access to a Kubernetes cluster
   ```bash
   # macOS
   brew install minikube
   
   # Windows (Chocolatey)
   choco install minikube
   
   # Or download from: https://minikube.sigs.k8s.io/docs/start/
   ```

## Local Development with Docker Desktop Kubernetes

Docker Desktop inkluderar Kubernetes - inget behov av minikube!

### 1. Enable Kubernetes in Docker Desktop

1. Öppna **Docker Desktop Settings**
2. Gå till **Kubernetes** flik
3. ✅ Checka "Enable Kubernetes"
4. Klicka "Apply & Restart"
5. Vänta tills K8s startar (se status i taskbar)

### 2. Verify Kubernetes is Running

```bash
kubectl cluster-info
kubectl get nodes
```

### 3. Deploy Using the Script

```bash
cd k8s
bash deploy.sh deploy
```

Eller steg för steg:
```bash
bash deploy.sh build      # Build Docker images
bash deploy.sh load       # Verify images exist
bash deploy.sh deploy     # Deploy to K8s
```

### 4. Access the Services

#### Frontend
```bash
kubectl port-forward svc/frontend-service 5173:5173 -n dartmaster
```
Öppna: http://localhost:5173

#### Backend API
```bash
kubectl port-forward svc/api-service 5146:5146 -n dartmaster
```
API: http://localhost:5146/api

#### MySQL Database
```bash
kubectl port-forward svc/mysql-service 3306:3306 -n dartmaster
```
Anslut: localhost:3306

### 5. Monitor Deployments

```bash
# Watch pod status
kubectl get pods -n dartmaster -w

# View events
kubectl get events -n dartmaster --sort-by='.lastTimestamp'

# View logs
kubectl logs -f deployment/dartmaster-api -n dartmaster
kubectl logs -f deployment/dartmaster-web -n dartmaster
kubectl logs -f deployment/mysql -n dartmaster

# Describe resources
kubectl describe deployment dartmaster-api -n dartmaster
```

### 6. Scale Deployments

```bash
# Scale backend API
kubectl scale deployment dartmaster-api --replicas=5 -n dartmaster

# Scale frontend
kubectl scale deployment dartmaster-web --replicas=3 -n dartmaster
```

### 7. Clean Up

```bash
bash deploy.sh delete
# eller manually:
kubectl delete namespace dartmaster
```

## Production Deployment

### Using Azure Kubernetes Service (AKS)

1. **Create AKS cluster**
   ```bash
   az aks create \
     --resource-group myResourceGroup \
     --name dartmaster-cluster \
     --node-count 3 \
     --vm-set-type VirtualMachineScaleSets \
     --load-balancer-sku standard
   ```

2. **Get cluster credentials**
   ```bash
   az aks get-credentials \
     --resource-group myResourceGroup \
     --name dartmaster-cluster
   ```

3. **Push images to container registry**
   ```bash
   # Create Azure Container Registry
   az acr create --resource-group myResourceGroup \
     --name dartmasteracr --sku Basic
   
   # Build and push
   az acr build --registry dartmasteracr \
     --image dartmaster-backend:latest \
     -f Dockerfile.backend .
   
   az acr build --registry dartmasteracr \
     --image dartmaster-frontend:latest \
     -f Dockerfile.frontend .
   ```

4. **Update image references in deployments**
   ```yaml
   # In backend-deployment.yaml
   image: dartmasteracr.azurecr.io/dartmaster-backend:latest
   imagePullPolicy: Always
   
   # In frontend-deployment.yaml
   image: dartmasteracr.azurecr.io/dartmaster-frontend:latest
   imagePullPolicy: Always
   ```

5. **Create imagePullSecret for private registry**
   ```bash
   kubectl create secret docker-registry acr-secret \
     --docker-server=dartmasteracr.azurecr.io \
     --docker-username=<username> \
     --docker-password=<password> \
     -n dartmaster
   ```

6. **Deploy to AKS**
   ```bash
   # Update deploy.sh to use correct image paths
   bash deploy.sh deploy
   ```

### Using Google Cloud GKE

1. **Create GKE cluster**
   ```bash
   gcloud container clusters create dartmaster-cluster \
     --zone us-central1-a \
     --num-nodes 3 \
     --machine-type n1-standard-2
   ```

2. **Get cluster credentials**
   ```bash
   gcloud container clusters get-credentials dartmaster-cluster \
     --zone us-central1-a
   ```

3. **Push images to Google Container Registry**
   ```bash
   docker tag dartmaster-backend:latest \
     gcr.io/my-project/dartmaster-backend:latest
   
   docker tag dartmaster-frontend:latest \
     gcr.io/my-project/dartmaster-frontend:latest
   
   docker push gcr.io/my-project/dartmaster-backend:latest
   docker push gcr.io/my-project/dartmaster-frontend:latest
   ```

4. **Update image references and deploy**
   ```bash
   # Update image paths in deployment files
   bash deploy.sh deploy
   ```

## Monitoring and Troubleshooting

### Check pod status
```bash
kubectl get pods -n dartmaster -o wide
```

### View pod logs
```bash
kubectl logs <pod-name> -n dartmaster
kubectl logs <pod-name> -n dartmaster --previous  # Previous crash logs
```

### Execute commands in pod
```bash
kubectl exec -it <pod-name> -n dartmaster -- /bin/bash
```

### Check resource usage
```bash
kubectl top nodes
kubectl top pods -n dartmaster
```

### HPA status
```bash
kubectl get hpa -n dartmaster
kubectl describe hpa dartmaster-api-hpa -n dartmaster
```

### Troubleshoot image pull issues
```bash
kubectl describe pod <pod-name> -n dartmaster
# Look for ImagePullBackOff or similar errors in Events section
```

## Environment Variables

### ConfigMap variables
- `ASPNETCORE_ENVIRONMENT`: Production
- `DATABASE_HOST`: mysql-service
- `DATABASE_PORT`: 3306
- `DATABASE_NAME`: DartMaster

### Secret variables
- `MYSQL_ROOT_PASSWORD`: root
- `MYSQL_USER`: dartmaster
- `MYSQL_PASSWORD`: dartmaster
- `DATABASE_USER`: dartmaster
- `DATABASE_PASSWORD`: dartmaster

### Frontend environment
- `REACT_APP_API_URL`: http://api-service:5146/api

## Ingress Setup (Optional)

For production, use Ingress for routing:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: dartmaster-ingress
  namespace: dartmaster
spec:
  rules:
  - host: dartmaster.example.com
    http:
      paths:
      - path: /api
        pathType: Prefix
        backend:
          service:
            name: api-service
            port:
              number: 5146
      - path: /
        pathType: Prefix
        backend:
          service:
            name: frontend-service
            port:
              number: 80
```

## Backup and Recovery

### Backup MySQL data
```bash
kubectl exec -it deployment/mysql -n dartmaster -- \
  mysqldump -u root -proot DartMaster > backup.sql
```

### Restore MySQL data
```bash
kubectl exec -i deployment/mysql -n dartmaster -- \
  mysql -u root -proot DartMaster < backup.sql
```

## Updating Deployments

### Rolling update with new image
```bash
kubectl set image deployment/dartmaster-api \
  api=dartmaster-backend:v2 \
  -n dartmaster
```

### Rollback to previous version
```bash
kubectl rollout undo deployment/dartmaster-api -n dartmaster
```

### View rollout history
```bash
kubectl rollout history deployment/dartmaster-api -n dartmaster
```

## Performance Tuning

### Resource limits
Adjust `limits` and `requests` in deployment files based on your needs:
```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### HPA thresholds
Modify CPU and memory thresholds in `hpa.yaml`:
```yaml
metrics:
- type: Resource
  resource:
    name: cpu
    target:
      type: Utilization
      averageUtilization: 70  # Adjust this value
```

## Security Considerations

1. **Use Secrets for sensitive data** (already implemented)
2. **Network Policies** - Restrict traffic between pods
3. **RBAC** - Implement role-based access control
4. **Pod Security Policies** - Define security standards
5. **Resource Quotas** - Limit resource consumption
6. **Regular Updates** - Keep images and dependencies updated

For production, consider:
- Using sealed secrets or external secret management (HashiCorp Vault, Azure Key Vault)
- Implementing network policies
- Setting up Pod Security Standards
- Using private container registries
- Implementing pod security contexts

## Support

For help with Kubernetes:
- Official Documentation:https://kubernetes.io/docs/
- kubectl Cheat Sheet: https://kubernetes.io/docs/reference/kubectl/cheatsheet/
- Local minikube: https://minikube.sigs.k8s.io/docs/
