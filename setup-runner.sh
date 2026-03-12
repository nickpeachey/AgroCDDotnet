#!/bin/bash

# Configuration
RUNNER_DIR="actions-runner"
VERSION="2.313.0" # You may want to check for the latest version

echo "--- 1. Prerequisites Check ---"
command -v docker >/dev/null 2>&1 || { echo "Docker is required but not installed. Aborting." >&2; exit 1; }
command -v kubectl >/dev/null 2>&1 || { echo "kubectl is required but not installed. Aborting." >&2; exit 1; }
command -v kind >/dev/null 2>&1 || { echo "kind is required but not installed. Aborting." >&2; exit 1; }
command -v kustomize >/dev/null 2>&1 || { echo "kustomize is required but not installed. Aborting." >&2; exit 1; }

echo "--- 2. Create Runner Directory ---"
mkdir -p $RUNNER_DIR && cd $RUNNER_DIR

echo "--- 3. Download Runner Package ---"
if [[ "$OSTYPE" == "darwin"* ]]; then
    # MacOS
    curl -o actions-runner-osx-x64-$VERSION.tar.gz -L https://github.com/actions/runner/releases/download/v$VERSION/actions-runner-osx-x64-$VERSION.tar.gz
    tar xzf ./actions-runner-osx-x64-$VERSION.tar.gz
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    # Linux
    curl -o actions-runner-linux-x64-$VERSION.tar.gz -L https://github.com/actions/runner/releases/download/v$VERSION/actions-runner-linux-x64-$VERSION.tar.gz
    tar xzf ./actions-runner-linux-x64-$VERSION.tar.gz
else
    echo "Unsupported OS: $OSTYPE"
    exit 1
fi

echo "--- 4. Instructions for Configuration ---"
echo ""
echo "To finish the setup, you need a registration token from your GitHub repository:"
echo "1. Go to your GitHub Repository -> Settings -> Actions -> Runners"
echo "2. Click 'New self-hosted runner'"
echo "3. Copy the token from the 'Configure' section"
echo ""
echo "Run the following command inside the '$RUNNER_DIR' directory:"
echo "./config.sh --url https://github.com/<your-user>/<your-repo> --token <your-token>"
echo ""
echo "After configuration, start the runner with:"
echo "./run.sh"
echo ""
echo "Note: Ensure the runner user has permission to use Docker and kubectl."
