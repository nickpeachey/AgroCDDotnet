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

Validation: Run automated integration tests against the api-dev endpoint.

Promotion: If tests pass, the workflow updates the gitops/test patch, triggering ArgoCD to sync the test environment.

5. Infrastructure Requirements

Local Runner: A machine with Docker, kubectl, and kind installed.

Ingress: NGINX Ingress Controller inside kind to route traffic to dev.local and test.local.