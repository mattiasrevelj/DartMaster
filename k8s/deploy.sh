#!/bin/bash
# DartMaster Kubernetes Deployment Script

set -e

echo "================================"
echo "DartMaster K8s Deployment Script"
echo "================================"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
print_status() {
  echo -e "${GREEN}✓${NC} $1"
}

print_error() {
  echo -e "${RED}✗${NC} $1"
}

print_info() {
  echo -e "${YELLOW}ℹ${NC} $1"
}

# Check prerequisites
check_prerequisites() {
  print_info "Checking prerequisites..."
  
  if ! command -v kubectl &> /dev/null; then
    print_error "kubectl not found. Please install kubectl first."
    exit 1
  fi
  print_status "kubectl found"
  
  if ! command -v docker &> /dev/null; then
    print_error "docker not found. Please install docker first."
    exit 1
  fi
  print_status "docker found"
  
  echo ""
}

# Build Docker images
build_images() {
  print_info "Building Docker images..."
  
  echo "Building backend image..."
  docker build -t dartmaster-backend:latest -f Dockerfile.backend .
  print_status "Backend image built"
  
  echo "Building frontend image..."
  docker build -t dartmaster-frontend:latest -f Dockerfile.frontend .
  print_status "Frontend image built"
  
  echo ""
}

# Load images into Kubernetes
load_to_k8s() {
  print_info "Loading images into Kubernetes..."
  
  # For Docker Desktop Kubernetes, images are automatically available
  # Just verify they exist locally
  if docker image inspect dartmaster-backend:latest &> /dev/null; then
    print_status "Backend image available"
  else
    print_error "Backend image not found - build first with 'build' command"
    exit 1
  fi
  
  if docker image inspect dartmaster-frontend:latest &> /dev/null; then
    print_status "Frontend image available"
  else
    print_error "Frontend image not found - build first with 'build' command"
    exit 1
  fi
  
  echo ""
}

# Deploy to Kubernetes
deploy() {
  print_info "Deploying to Kubernetes..."
  
  # Apply namespace first
  echo "Creating namespace..."
  kubectl apply -f k8s/namespace.yaml
  print_status "Namespace created"
  
  # Apply ConfigMap and Secret
  echo "Creating ConfigMap and Secret..."
  kubectl apply -f k8s/configmap.yaml
  kubectl apply -f k8s/secret.yaml
  print_status "ConfigMap and Secret created"
  
  # Apply MySQL and PVC
  echo "Deploying MySQL..."
  kubectl apply -f k8s/mysql-pvc.yaml
  kubectl apply -f k8s/mysql-deployment.yaml
  print_status "MySQL deployed"
  
  # Wait for MySQL to be ready
  echo "Waiting for MySQL to be ready..."
  kubectl wait --for=condition=ready pod -l app=mysql -n dartmaster --timeout=300s
  print_status "MySQL is ready"
  
  # Apply backend
  echo "Deploying backend API..."
  kubectl apply -f k8s/backend-deployment.yaml
  print_status "Backend API deployed"
  
  # Wait for backend to be ready
  echo "Waiting for backend to be ready..."
  kubectl wait --for=condition=ready pod -l app=dartmaster-api -n dartmaster --timeout=300s
  print_status "Backend API is ready"
  
  # Apply frontend
  echo "Deploying frontend..."
  kubectl apply -f k8s/frontend-deployment.yaml
  print_status "Frontend deployed"
  
  # Apply HPA
  echo "Setting up auto-scaling..."
  kubectl apply -f k8s/hpa.yaml
  print_status "Auto-scaling configured"
  
  echo ""
}

# Print service info
print_info() {
  echo -e "${YELLOW}ℹ${NC} $1"
}

print_service_info() {
  echo ""
  print_info "Services deployed to Docker Desktop Kubernetes!"
  echo ""
  echo "Available services:"
  kubectl get svc -n dartmaster
  echo ""
  
  echo "Access services:"
  echo "  Frontend: kubectl port-forward svc/frontend-service 5173:5173 -n dartmaster"
  echo "  Backend:  kubectl port-forward svc/api-service 5146:5146 -n dartmaster"
  echo "  MySQL:    kubectl port-forward svc/mysql-service 3306:3306 -n dartmaster"
  echo ""
  
  echo "View logs:"
  echo "  kubectl logs -f deployment/dartmaster-api -n dartmaster"
  echo "  kubectl logs -f deployment/dartmaster-web -n dartmaster"
  echo "  kubectl logs -f deployment/mysql -n dartmaster"
  echo ""
  
  echo "Monitor pods:"
  echo "  kubectl get pods -n dartmaster -w"
  echo ""
}

# Main execution
case "${1:-deploy}" in
  build)
    check_prerequisites
    build_images
    ;;
  load)
    check_prerequisites
    load_to_k8s
    ;;
  deploy)
    check_prerequisites
    build_images
    load_to_k8s
    deploy
    print_service_info
    ;;
  delete)
    print_info "Deleting Kubernetes resources..."
    kubectl delete namespace dartmaster --ignore-not-found=true
    print_status "Resources deleted"
    ;;
  *)
    echo "Usage: $0 {build|load|deploy|delete}"
    echo ""
    echo "Commands:"
    echo "  build   - Build Docker images only"
    echo "  load    - Verify images are available"
    echo "  deploy  - Build, verify images, and deploy to K8s (default)"
    echo "  delete  - Delete all Kubernetes resources"
    exit 1
    ;;
esac

echo -e "${GREEN}Done!${NC}"
