#!/bin/bash
set -e

# --- Configuration ---
CLUSTER_NAME="agrocd"
KIND_CONFIG="kind-config.yaml"
ARGOCD_VERSION="stable"

echo "--- 1. Prerequisites Check ---"
command -v docker >/dev/null 2>&1 || { echo "Docker is required but not installed. Aborting." >&2; exit 1; }
command -v kubectl >/dev/null 2>&1 || { echo "kubectl is required but not installed. Aborting." >&2; exit 1; }
command -v kind >/dev/null 2>&1 || { echo "kind is required but not installed. Aborting." >&2; exit 1; }

echo "--- 2. Create Kind Cluster ---"
if kind get clusters | grep -q "^$CLUSTER_NAME$"; then
    echo "Cluster '$CLUSTER_NAME' already exists. Skipping creation."
else
    kind create cluster --name "$CLUSTER_NAME" --config "$KIND_CONFIG"
fi

echo "--- 3. Install NGINX Ingress Controller ---"
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
echo "Waiting for Ingress Controller to be ready..."
kubectl wait --namespace ingress-nginx --for=condition=ready pod --selector=app.kubernetes.io/component=controller --timeout=300s

echo "--- 4. Install ArgoCD ---"
kubectl create namespace argocd --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/$ARGOCD_VERSION/manifests/install.yaml

echo "Waiting for ArgoCD components to be ready..."
kubectl wait --namespace argocd --for=condition=ready pod --selector=app.kubernetes.io/name=argocd-server --timeout=300s

echo "--- 5. Create Application Namespaces ---"
kubectl create namespace api-dev --dry-run=client -o yaml | kubectl apply -f -
kubectl create namespace api-test --dry-run=client -o yaml | kubectl apply -f -

echo "--- 6. Apply ArgoCD Applications ---"
if [ -f "argocd-apps.yaml" ]; then
    kubectl apply -f argocd-apps.yaml
else
    echo "Warning: argocd-apps.yaml not found. Skipping."
fi

echo "--- 7. Setup Complete ---"
echo ""
echo "ArgoCD initial admin password:"
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d && echo ""
echo ""
echo "To access ArgoCD UI locally (after port-forwarding):"
echo "kubectl port-forward svc/argocd-server -n argocd 8081:443"
echo "URL: https://localhost:8081"
echo ""
echo "Next steps:"
echo "1. Configure your .env file with GITHUB_URL and GITHUB_TOKEN."
echo "2. Start the local runner: docker-compose -f docker-compose.runner.yml up --build -d"
echo "3. Follow instructions in INSTALL.md for GitHub Repository Settings."
