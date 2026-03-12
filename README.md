# AgroCDDotnet

This repository deploys a .NET Todo API with ArgoCD, Postgres, and a separate database migrator image.

## Environment layout

* `api-dev` runs the API, a Postgres instance backed by `emptyDir`, and the database migrator job.
* `api-test` runs the API, a Postgres instance backed by a persistent volume, and the database migrator job.

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

If validation fails after `api-dev` is updated, the workflow automatically commits a rollback of the dev overlay back to the previous API image and previous database migrator image.

What rolls back automatically:

* the API image reference in `gitops/overlays/dev/kustomization.yaml`
* the database migrator image reference in `gitops/overlays/dev/kustomization.yaml`

What does not roll back automatically:

* already-applied database schema changes
* Postgres data in `api-test`

The practical rule is that database migrations must be forward-only and backward-compatible with the previous API version. If a bad migration ships, recovery is a new corrective migration rather than an automatic schema rollback.

More setup detail is in [INSTALL.md](/Users/nickpeachey/Developer/projects/AgroCDDotnet/INSTALL.md).
