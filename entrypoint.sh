#!/bin/bash
set -e

# --- Fix Docker Permissions ---
if [ -e /var/run/docker.sock ]; then
    # Get GID of the docker socket on host
    DOCKER_GID=$(stat -c '%g' /var/run/docker.sock)
    
    # Check if a group with that GID already exists
    EXISTING_GROUP=$(getent group $DOCKER_GID | cut -d: -f1)
    
    if [ -n "$EXISTING_GROUP" ]; then
        # If group exists, add runner to it
        sudo usermod -aG "$EXISTING_GROUP" runner
    else
        # If not, create 'docker' group with that GID and add runner
        sudo groupadd -g "$DOCKER_GID" host_docker
        sudo usermod -aG host_docker runner
    fi
fi

# Configuration
GITHUB_URL="${GITHUB_URL}"
GITHUB_TOKEN="${GITHUB_TOKEN}"
RANDOM_ID=$(head /dev/urandom | tr -dc a-z0-9 | head -c 4 ; echo '')
RUNNER_NAME="${RUNNER_NAME:-local-runner}-$RANDOM_ID"
RUNNER_LABELS="${RUNNER_LABELS:-self-hosted,docker}"

# Fix Kubeconfig for container-to-container communication
if [ -f /home/runner/.kube/config ]; then
    echo "Updating kubeconfig..."
    # Change 127.0.0.1 (localhost) to the Docker container IP for kind
    KIND_IP=$(docker inspect agrocd-control-plane -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}')
    if [ -n "$KIND_IP" ]; then
        echo "Found kind IP: $KIND_IP"
        # Create a local copy that the runner can actually use
        cp /home/runner/.kube/config /home/runner/kube_config_internal
        sed -i "s/127.0.0.1/$KIND_IP/g" /home/runner/kube_config_internal
        # Ensure the runner user owns it
        chown runner:runner /home/runner/kube_config_internal
        export KUBECONFIG=/home/runner/kube_config_internal
    fi
fi

cd actions-runner

# Remove existing .runner file to ensure a clean registration with correct labels
rm -f .runner

if [ ! -f .runner ]; then
    echo "Configuring runner..."
    sudo -E -u runner ./config.sh --url "${GITHUB_URL}" --token "${GITHUB_TOKEN}" --name "${RUNNER_NAME}" --labels "${RUNNER_LABELS}" --unattended --replace
fi

echo "Starting runner..."
# Use sudo -E -u runner to run the runner as the 'runner' user while preserving environment variables
# and ensuring the new group membership (from usermod) is recognized.
export KUBECONFIG=$KUBECONFIG
exec sudo -E -u runner KUBECONFIG=$KUBECONFIG ./run.sh
