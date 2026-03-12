# AgroCDDotnet Setup & Usage Guide

This project establishes a local Kubernetes-based GitOps environment using **Kind**, **ArgoCD**, **GHCR**, and a **self-hosted GitHub Actions runner**.

## 1. Prerequisites

*   **Docker Desktop** (with enough resources allocated).
*   **kubectl** (v1.30+).
*   **Kind** (v0.25+).
*   **Personal Access Token (PAT)** from GitHub with `write:packages`, `read:packages`, and `repo` scopes.
*   **Node.js** (If running integration tests manually).

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

---

## 3. GitHub Configuration

### 1. Repository Permissions
Go to your GitHub Repository -> **Settings** -> **Actions** -> **General**.
1.  Under **Workflow permissions**, select **"Read and write permissions"**.
2.  Ensure **"Allow GitHub Actions to create and approve pull requests"** is checked.
3.  Click **Save**.

### 2. Update ArgoCD URL
1.  Open `argocd-apps.yaml` and update `repoURL` to point to your actual GitHub repository.
2.  Apply the change: `kubectl apply -f argocd-apps.yaml`.

---

## 4. Local GitHub Runner Setup

The runner allows GitHub to build images and update your local cluster.

### 1. Registration Token
Go to **Settings** -> **Actions** -> **Runners** -> **New self-hosted runner**.
*   Look for the `--token` value in the configuration script provided by GitHub.

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

**Verification:** Check the logs to ensure the runner is "Listening for Jobs":
```bash
docker logs -f github-runner
```

---

## 5. Usage & GitOps Workflow

To trigger the pipeline, simply push a change to the `main` branch.

### 1. The Build Phase
*   The runner builds the .NET 10 API image.
*   The image is pushed to **GitHub Container Registry (GHCR)**.

### 2. The Dev Deployment
*   The workflow updates the `gitops/overlays/dev` manifest with the new image tag.
*   The manifest is committed and pushed back to your repository.
*   **ArgoCD** detects the change and syncs the `api-dev` namespace.

### 3. Validation
*   The workflow waits for the `api-dev` rollout to complete.
*   A **Postman Collection** (`integration-tests.postman_collection.json`) is executed using **Newman**.
*   The tests verify:
    *   HTTP 200 Status Code.
    *   Response is an array of 5 items.
    *   Correct JSON structure for each item (date, temperatureC, summary, etc.).

### 4. Promotion
*   If validation passes, the workflow updates the `gitops/overlays/test` manifest.
*   The commit is pushed back to the repository.
*   **ArgoCD** syncs the `api-test` namespace.

---

### Running Tests Manually
If you have Newman installed locally, you can run the integration tests manually against the cluster:
```bash
newman run integration-tests.postman_collection.json \
  --env-var "base_url=localhost" \
  --env-var "host_header=dev.local"
```

---

## 6. Local Endpoints

*   **Dev Environment**: `http://dev.local/weatherforecast`
*   **Test Environment**: `http://test.local/weatherforecast`

*Note: You may need to add these to your `/etc/hosts` file:*
```text
127.0.0.1 dev.local test.local
```

---

## 7. ArgoCD Management

**Access UI:**
1.  Port-forward: `kubectl port-forward svc/argocd-server -n argocd 8081:443`
2.  Open: `https://localhost:8081` (User: `admin`)
3.  Password: (Retrieved during `install.sh`)
