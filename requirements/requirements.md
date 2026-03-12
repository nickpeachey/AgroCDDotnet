1. Project Overview

This project establishes a local Kubernetes-based GitOps environment. It uses a .NET 10 WebAPI as the target application, ArgoCD for continuous delivery, and GitHub Actions (via a local runner) for automated CI and environment promotion.

2. Tech Stack

Runtime: .NET 10 SDK

Orchestration: Kubernetes (Local kind cluster)

GitOps: ArgoCD v2.13+

CI/CD: GitHub Actions (Self-hosted Runner)

Manifest Management: Kustomize

Container Registry: GitHub Container Registry (GHCR) or Local Docker Registry

3. Environment Architecture

Dev Namespace: api-dev - Automatically syncs whenever a new image is built from the main branch.

Test Namespace: api-test - Only updated after integration tests pass against the Dev environment.

4. CI/CD Workflow

Build Phase: Triggered on push to main. Build Docker image and push to registry.

Dev Deployment: Update the gitops/dev Kustomize patch with the new image tag.

Validation: Run automated integration tests using **Postman & Newman** against the api-dev endpoint. Tests include:
  - Verifying HTTP 200 response.
  - Ensuring the response is an array with 5 entries.
  - Validating the schema of the JSON objects (Date, TemperatureC, Summary).

Promotion: If tests pass, the workflow updates the gitops/test patch, triggering ArgoCD to sync the test environment.

5. Infrastructure Requirements

Local Runner: A self-hosted GitHub Actions runner configured on the local machine. It can be run directly or via Docker Compose.

To set up the runner (Docker Compose recommended):
1. Create a `.env` file with `GITHUB_URL` and `GITHUB_TOKEN`.
2. Run `docker-compose -f docker-compose.runner.yml up --build -d`.

Alternatively, to set up the runner manually:
1. Run `./setup-runner.sh` to download the runner software.
2. Follow the instructions in the script to register the runner with your GitHub repository.
3. Start the runner using `./actions-runner/run.sh`.

Ingress: NGINX Ingress Controller inside kind to route traffic to dev.local and test.local.

6. ArgoCD Access

The ArgoCD UI is accessible via the local cluster (if port-forwarded) or through the Ingress if configured. By default, the admin username is `admin`.

### ArgoCD Application Setup
By default, the ArgoCD Applications are defined in `argocd-apps.yaml`. **You must update the `repoURL`** in this file to point to your actual GitHub repository for ArgoCD to sync the manifests correctly.

To apply the applications to the cluster:
```bash
kubectl apply -f argocd-apps.yaml
```

To retrieve the initial admin password, run:
```bash
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d && echo ""
```

Your current initial password is: `XXI4rjwZEmAGxkhN`