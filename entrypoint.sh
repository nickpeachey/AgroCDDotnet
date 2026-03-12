#!/bin/bash
set -e

# Configuration
export HOME=/home/runner
GITHUB_URL="${GITHUB_URL}"
GITHUB_TOKEN="${GITHUB_TOKEN}"
RANDOM_ID=$(head /dev/urandom | tr -dc a-z0-9 | head -c 4 ; echo '')
RUNNER_NAME="${RUNNER_NAME:-local-runner}-$RANDOM_ID"
RUNNER_LABELS="${RUNNER_LABELS:-self-hosted,docker}"

# --- Fix Docker Permissions ---
DOCKER_GROUP="docker"
if [ -e /var/run/docker.sock ]; then
    DOCKER_GID=$(stat -c '%g' /var/run/docker.sock)
    EXISTING_GROUP=$(getent group $DOCKER_GID | cut -d: -f1)
    if [ -n "$EXISTING_GROUP" ]; then
        DOCKER_GROUP="$EXISTING_GROUP"
        sudo usermod -aG "$DOCKER_GROUP" runner
    else
        DOCKER_GROUP="host_docker"
        sudo groupadd -g "$DOCKER_GID" "$DOCKER_GROUP"
        sudo usermod -aG "$DOCKER_GROUP" runner
    fi
fi

# Fix Kubeconfig for container-to-container communication
if [ -f /home/runner/.kube/config ]; then
    KIND_IP=$(sudo docker inspect agrocd-control-plane -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' || echo "")
    if [ -n "$KIND_IP" ]; then
        cp /home/runner/.kube/config /home/runner/kube_config_internal
        sed -i "s|server: https://127.0.0.1:.*|server: https://$KIND_IP:6443|g" /home/runner/kube_config_internal
        # No need for sudo chown as we will run as 'runner'
        chmod 644 /home/runner/kube_config_internal
        export KUBECONFIG=/home/runner/kube_config_internal
    fi
fi

cd actions-runner

# Remove existing .runner file to ensure a clean registration with correct labels
rm -f .runner

if [ ! -f .runner ]; then
    echo "Configuring runner..."
    ./config.sh --url "${GITHUB_URL}" --token "${GITHUB_TOKEN}" --name "${RUNNER_NAME}" --labels "${RUNNER_LABELS}" --unattended --replace
fi

echo "Starting runner..."
# Start the runner using 'sg' to ensure it runs with the newly assigned group privileges.
# We use 'exec' so that signals (like SIGTERM) are passed correctly to the runner.
export KUBECONFIG=$KUBECONFIG
exec sg "$DOCKER_GROUP" "cd /home/runner/actions-runner && ./run.sh"
