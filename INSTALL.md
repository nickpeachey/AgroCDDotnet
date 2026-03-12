# AgroCDDotnet Setup & Usage Guide

This project establishes a local Kubernetes-based GitOps environment using **Kind**, **ArgoCD**, **GHCR**, and a **self-hosted GitHub Actions runner**. The deployed stack includes a **.NET API**, a namespace-local **Postgres** instance, and a dedicated **database migration project** that ArgoCD runs before the API rollout.

## 1. Prerequisites

*   **Docker Desktop** (with enough resources allocated).
*   **kubectl** (v1.30+).
*   **Kind** (v0.25+).
*   **Personal Access Token (PAT)** from GitHub with `write:packages`, `read:packages`, and `repo` scopes.
*   **Node.js** (if running Newman manually).

---

## 2. Infrastructure Setup

Run the provided install script to bootstrap the local environment:

```bash
chmod +x install.sh
./install.sh
```

**The script will:**
1.  Create a local `kind` cluster with Ingress port-mapping (80, 443).
2.  Install the **NGINX Ingress Controller**.
3.  Install **ArgoCD**.
4.  Configure `api-dev` and `api-test` namespaces.
5.  Deploy the ArgoCD Applications defined in `argocd-apps.yaml`.
6.  Prepare ArgoCD to manage Postgres, the database migration job, and the API in both environments.

---

## 3. GitHub Configuration

### 1. Repository Permissions
Go to your GitHub repository under **Settings** -> **Actions** -> **General**.
1.  Under **Workflow permissions**, select **Read and write permissions**.
2.  Ensure **Allow GitHub Actions to create and approve pull requests** is checked.
3.  Save the change.

### 2. Update ArgoCD URL
1.  Open `argocd-apps.yaml` and update `repoURL` to point to your actual GitHub repository.
2.  Apply the change with `kubectl apply -f argocd-apps.yaml`.

---

## 4. Local GitHub Runner Setup

The runner builds both application and database migration images, then updates the GitOps overlays in this repository.

### 1. Registration Token
Go to **Settings** -> **Actions** -> **Runners** -> **New self-hosted runner**.
*   Copy the `--token` value from the GitHub instructions.

### 2. Environment Variables
Create a `.env` file in the project root:

```env
GITHUB_URL=https://github.com/your-username/your-repo
GITHUB_TOKEN=PASTE_COPIED_TOKEN_HERE
```

### 3. Start the Runner
```bash
docker-compose -f docker-compose.runner.yml up --build -d
```

If you change the runner container configuration later, recreate it so Docker host networking aliases are refreshed:

```bash
docker-compose -f docker-compose.runner.yml up --build -d --force-recreate
```

**Verification:** Check the logs until the runner is listening for jobs:
```bash
docker logs -f github-runner
```

---

## 5. Usage & GitOps Workflow

Push a change to `main` to trigger the full path.

### 1. Build
*   The workflow runs the .NET integration suite against a temporary Postgres container.
*   It builds and pushes the API image.
*   It builds and pushes the database migrator image from [`AgroCDDotnet.Database`](./AgroCDDotnet.Database/Program.cs).

### 2. Dev Deployment
*   The workflow creates or updates the `release/dev` branch from the latest `main`.
*   It updates `gitops/overlays/dev/kustomization.yaml` there with the new API and database migrator images.
*   ArgoCD syncs `api-dev` from `release/dev`.
*   During sync, ArgoCD deploys Postgres, runs the database migration job, and then rolls out the API.
*   The dev Postgres instance uses ephemeral storage.

### 3. Validation
*   The workflow waits for the `api-dev` Postgres deployment, migration job, and API deployment to complete.
*   The xUnit deployed integration tests exercise Todo CRUD.
*   Newman runs [`integration-tests.postman_collection.json`](./integration-tests.postman_collection.json) against the same dev environment.

### 4. Promotion
*   If dev validation passes, the workflow builds a `release/test` branch from the validated `release/dev` state and updates `gitops/overlays/test/kustomization.yaml` there.
*   The `api-test` ArgoCD application tracks `release/test`, so test only syncs after promotion.
*   The test Postgres instance uses a persistent volume claim bound to a dedicated persistent volume.

### 5. Validation Failure Behavior
*   If validation fails after `api-dev` is updated, promotion to `release/test` does not occur.
*   `api-test` therefore remains on its last promoted state.
*   Database migrations are not rolled back automatically. Migrations must therefore be forward-only and backward-compatible with the previous API version.

---

## 6. Database Project Workflow

Database changes live in [`AgroCDDotnet.Database/Scripts`](./AgroCDDotnet.Database/Scripts).

To change the schema:
1. Add a new versioned SQL script such as `002_add_due_date.sql`.
2. Commit and push it to `main`.
3. Let the normal GitHub Actions workflow build the updated database migrator image.
4. ArgoCD will run that new migrator in `api-dev`, validation will run against the updated schema, and promotion will carry the same change to `api-test`.

Applied scripts are tracked in the `schema_migrations` table in each environment, so the migration job is safe to rerun.

Credentials are stored per environment in Kubernetes Secrets:
*   `gitops/overlays/dev/postgres-secret.yaml`
*   `gitops/overlays/test/postgres-secret.yaml`

---

## 7. Running Tests Manually

Run the .NET integration tests:

```bash
dotnet test AgroCDDotnet.Api.Tests/AgroCDDotnet.Api.Tests.csproj --filter "TestTarget=InMemory"
```

Run Newman against the dev environment:

```bash
newman run integration-tests.postman_collection.json \
  --env-var "base_url=localhost" \
  --env-var "host_header=dev.local"
```

---

## 8. Local Endpoints

*   **Dev Health**: `http://dev.local/healthz`
*   **Test Health**: `http://test.local/healthz`
*   **Todo API**: `http://dev.local/todos` and `http://test.local/todos`

*Note: You may need to add these to your `/etc/hosts` file:*
```text
127.0.0.1 dev.local test.local
```

---

## 9. Retrieving Database Credentials

Retrieve values from the dev Secret:

```bash
kubectl get secret postgres-secrets -n api-dev -o jsonpath='{.data.POSTGRES_USER}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-dev -o jsonpath='{.data.POSTGRES_PASSWORD}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-dev -o jsonpath='{.data.POSTGRES_DB}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-dev -o jsonpath='{.data.TODOS_DATABASE_CONNECTION_STRING}' | base64 -d && echo
```

Retrieve values from the test Secret:

```bash
kubectl get secret postgres-secrets -n api-test -o jsonpath='{.data.POSTGRES_USER}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-test -o jsonpath='{.data.POSTGRES_PASSWORD}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-test -o jsonpath='{.data.POSTGRES_DB}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-test -o jsonpath='{.data.TODOS_DATABASE_CONNECTION_STRING}' | base64 -d && echo
```

---

## 10. ArgoCD Management

**Access UI:**
1.  Port-forward: `kubectl port-forward svc/argocd-server -n argocd 8081:443`
2.  Open: `https://localhost:8081`
3.  User: `admin`
4.  Password: retrieve it during `install.sh`
