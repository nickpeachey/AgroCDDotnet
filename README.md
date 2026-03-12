# AgroCDDotnet

This repository deploys a .NET Todo API with ArgoCD, Postgres, and a separate database migrator image.

## Environment layout

* `api-dev` runs the API, a Postgres instance backed by `emptyDir`, and the database migrator job.
* `api-test` runs the API, a Postgres instance backed by a persistent volume, and the database migrator job.
* `api-dev` tracks `release/dev`, while `api-test` tracks `release/test`.
* `main` is the source branch. The workflow publishes validated deployment state to the release branches.
* CI logs now print the exact `release/dev` and `release/test` commit revisions pushed for deployment so Argo sync issues can be traced to branch state vs cluster state.
* ArgoCD is installed with server-side apply because the `ApplicationSet` CRD can exceed the client-side apply annotation limit.

## Database credentials

Each environment has its own Kubernetes Secret named `postgres-secrets`.

Retrieve the dev credentials:

```bash
kubectl get secret postgres-secrets -n api-dev -o jsonpath='{.data.POSTGRES_USER}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-dev -o jsonpath='{.data.POSTGRES_PASSWORD}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-dev -o jsonpath='{.data.POSTGRES_DB}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-dev -o jsonpath='{.data.TODOS_DATABASE_CONNECTION_STRING}' | base64 -d && echo
```

Retrieve the test credentials:

```bash
kubectl get secret postgres-secrets -n api-test -o jsonpath='{.data.POSTGRES_USER}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-test -o jsonpath='{.data.POSTGRES_PASSWORD}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-test -o jsonpath='{.data.POSTGRES_DB}' | base64 -d && echo
kubectl get secret postgres-secrets -n api-test -o jsonpath='{.data.TODOS_DATABASE_CONNECTION_STRING}' | base64 -d && echo
```

## GHCR pull secret

The workflow creates or updates a Kubernetes secret named `ghcr-pull-secret` in `api-dev` and `api-test` from these GitHub Actions repository secrets:

```text
GHCR_PULL_USERNAME
GHCR_PULL_PASSWORD
```

Verify the in-cluster secrets exist:

```bash
kubectl get secret ghcr-pull-secret -n api-dev
kubectl get secret ghcr-pull-secret -n api-test
```

## Release branch bootstrap

`api-dev` renders from `release/dev` and `api-test` renders from `release/test`, not from `main`.

If `release/test` has never been created yet, bootstrap it once from `release/dev`:

```bash
git fetch origin
git checkout -B release/test origin/release/dev
git push -u origin release/test
```

## Rollback behavior

If dev validation fails, promotion to `release/test` does not happen.

What advances automatically:

* `release/dev` is updated from `main` during the dev deployment step
* `release/test` is updated from `release/dev` only after validation succeeds

What does not roll back automatically:

* `release/dev`
* already-applied database schema changes
* Postgres data in `api-test`

The practical rule is that database migrations must be forward-only and backward-compatible with the previous API version. Recovery is a new corrective commit or migration, not an automatic schema rollback.

More setup detail is in [INSTALL.md](/Users/nickpeachey/Developer/projects/AgroCDDotnet/INSTALL.md).
