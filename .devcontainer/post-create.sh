#!/bin/bash

echo "Setting up FlyballStats.com development environment..."

# Update package list
sudo apt-get update

# Install Aspire CLI
echo "Installing .NET Aspire CLI..."
dotnet workload update
dotnet workload install aspire

# Install Aspire CLI as a global tool
echo "Installing Aspire CLI as global tool..."
dotnet tool install -g Microsoft.Dotnet.Aspire.Cli --prerelease

# Verify installations
echo "Verifying installations..."
dotnet --version
echo "Aspire workload installed:"
dotnet workload list | grep aspire || echo "Aspire workload not found, but should be available via SDK"
echo "Aspire CLI tool:"
aspire --version || echo "Aspire CLI tool not found, but workload should be sufficient"

# Restore NuGet packages
echo "Restoring NuGet packages..."
cd /workspaces/FlyballStats.com/src
dotnet restore

# Build the solution to verify everything works
echo "Building the solution..."
dotnet build

echo "Development environment setup complete!"
echo ""
echo "To start the application:"
echo "  1. Navigate to the src directory: cd src"
echo "  2. Run the Aspire application: aspire run"
echo "  3. Or run directly with: dotnet run --project flyballstats.AppHost"
echo ""
echo "The Aspire dashboard will be available on one of the forwarded ports (15000-15004)"
echo "The web application will be available on ports 5000 (HTTP) or 5001 (HTTPS)"