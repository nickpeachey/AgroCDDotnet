# AgroCDDotnet

This repository deploys a .NET Todo API with ArgoCD, Postgres, and a separate database migrator image.

## Environment layout

* `api-dev` runs the API, a Postgres instance backed by `emptyDir`, and the database migrator job.
* `api-test` runs the API, a Postgres instance backed by a persistent volume, and the database migrator job.
* `api-dev` tracks `release/dev`, while `api-test` tracks `release/test`.
* `main` is the source branch. The workflow publishes validated deployment state to the release branches.

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
