#!/bin/bash
set -e
exec > >(tee -a /home/runner/entrypoint.log) 2>&1

echo "Entrypoint started as $(whoami)"

# --- Fix Docker Permissions ---
if [ -e /var/run/docker.sock ]; then
    DOCKER_GID=$(stat -c '%g' /var/run/docker.sock)
    echo "Host Docker GID: $DOCKER_GID"
    EXISTING_GROUP=$(getent group $DOCKER_GID | cut -d: -f1)
    if [ -n "$EXISTING_GROUP" ]; then
        echo "Adding runner to existing group $EXISTING_GROUP"
        usermod -aG "$EXISTING_GROUP" runner
    else
        echo "Creating host_docker group with GID $DOCKER_GID"
        groupadd -g "$DOCKER_GID" host_docker
        usermod -aG host_docker runner
    fi
fi

# Fix Kubeconfig for container-to-container communication
if [ -f /home/runner/.kube/config ]; then
    echo "Found kubeconfig at /home/runner/.kube/config"
    KIND_IP=$(docker inspect agrocd-control-plane -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' || echo "")
    if [ -n "$KIND_IP" ]; then
        echo "Found kind IP: $KIND_IP"
        cp /home/runner/.kube/config /home/runner/kube_config_internal
        sed -i "s|server: https://127.0.0.1:.*|server: https://$KIND_IP:6443|g" /home/runner/kube_config_internal
        chown runner:runner /home/runner/kube_config_internal
        chmod 644 /home/runner/kube_config_internal
        export KUBECONFIG=/home/runner/kube_config_internal
        echo "Exported KUBECONFIG=$KUBECONFIG"
    else
        echo "Could not find kind IP"
    fi
else
    echo "No kubeconfig found at /home/runner/.kube/config"
fi

# Configuration
GITHUB_URL="${GITHUB_URL}"
GITHUB_TOKEN="${GITHUB_TOKEN}"
RANDOM_ID=$(head /dev/urandom | tr -dc a-z0-9 | head -c 4 ; echo '')
RUNNER_NAME="${RUNNER_NAME:-local-runner}-$RANDOM_ID"
RUNNER_LABELS="${RUNNER_LABELS:-self-hosted,docker}"

echo "Runner Name: $RUNNER_NAME"

cd actions-runner

# Remove existing .runner file to ensure a clean registration with correct labels
rm -f .runner

if [ ! -f .runner ]; then
    echo "Configuring runner..."
    sudo -E -u runner ./config.sh --url "${GITHUB_URL}" --token "${GITHUB_TOKEN}" --name "${RUNNER_NAME}" --labels "${RUNNER_LABELS}" --unattended --replace
fi

echo "Starting runner..."
# Fix: Explicitly set HOME and ensure the runner user owns its home directory
export HOME=/home/runner
chown -R runner:runner /home/runner

# Use sudo -E -u runner to run the runner as the 'runner' user while preserving environment variables
# and ensuring the new group membership (from usermod) is recognized.
exec sudo -E -u runner HOME=/home/runner KUBECONFIG=$KUBECONFIG ./run.sh
