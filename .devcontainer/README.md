# FlyballStats.com Dev Container

This directory contains the dev container configuration for FlyballStats.com, enabling you to run the entire development environment in GitHub Codespaces or any compatible dev container environment.

## What's Included

- **.NET 9.0 SDK** - Required for building and running the Aspire application
- **Aspire CLI** - For running the distributed application orchestrator
- **VS Code Extensions** - Pre-configured C# development tools
- **Node.js LTS** - For any frontend tooling if needed
- **GitHub CLI** - For Git operations

## Getting Started

### Using GitHub Codespaces

1. Navigate to the repository on GitHub
2. Click the **Code** button
3. Select **Codespaces** tab
4. Click **Create codespace on main**
5. Wait for the container to build and the post-create script to run

### Using VS Code with Dev Containers Extension

1. Clone the repository locally
2. Open in VS Code
3. Install the **Dev Containers** extension if not already installed
4. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
5. Select **Dev Containers: Reopen in Container**
6. Wait for the container to build

## Running the Application

Once the dev container is ready:

1. Open a terminal in VS Code
2. Navigate to the `src` directory:
   ```bash
   cd src
   ```
3. Run the application using Aspire:
   ```bash
   aspire run
   ```
   
   Or alternatively, run directly with dotnet:
   ```bash
   dotnet run --project flyballstats.AppHost
   ```

## Accessing the Application

- **Aspire Dashboard**: Available on ports 15000-15004 (automatically forwarded)
- **Web Frontend**: Available on port 5000 (HTTP) or 5001 (HTTPS)
- **API Service**: Available internally via service discovery

VS Code will automatically detect and forward these ports, making them accessible from your browser.

## Troubleshooting

### Build Issues
If you encounter build issues:
```bash
cd src
dotnet restore
dotnet build
```

### Port Conflicts
If ports are not accessible, check the **Ports** tab in VS Code and ensure the ports are forwarded.

### Aspire CLI Issues
Verify Aspire is properly installed:
```bash
dotnet workload list
```

## Development Tips

- The container automatically sets `ASPNETCORE_ENVIRONMENT=Development`
- .NET CLI telemetry is disabled for faster operations
- All necessary VS Code extensions are pre-installed
- The workspace is mounted at `/workspaces/FlyballStats.com`

## Container Specifications

- **Base Image**: `mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm`
- **User**: `vscode` (non-root)
- **Additional Features**: GitHub CLI, Node.js LTS
- **Forwarded Ports**: 5000, 5001, 15000-15004