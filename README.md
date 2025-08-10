# FlyballStats.com

A modern, real-time flyball racing schedule and ring management system built with .NET Aspire and designed for Tournament Directors, Race Directors, and tournament participants.

[![.NET Build & Test](https://github.com/two4suited/FlyballStats.com/actions/workflows/ci.yml/badge.svg)](https://github.com/two4suited/FlyballStats.com/actions/workflows/ci.yml)

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)

### Cloud & DevOps

- **Azure Cloud Platform**: Scalable cloud hosting and services
- **Azure Cosmos DB**: Global distribution and automatic scaling
- **.NET Aspire Dashboard**: Built-in observability and monitoring
- **GitHub Actions**: Continuous integration and deployment
- **OpenTelemetry**: Comprehensive telemetry and distributed tracing

## Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 9 SDK** (9.0.302 or newer) - [Download here](https://get.dot.net)
- **Docker Desktop** - For local development containers
- **Azure Account** - For cloud resources (optional for local development)
- **Visual Studio 2024** or **VS Code** with C# extension

#### Verify Prerequisites

```bash
# Check .NET version
dotnet --version

# Verify Docker is running
docker --version

# Install .NET Aspire workload (if not already installed)
dotnet workload install aspire
```

### Installation Steps

1. **Clone the Repository**
   ```bash
   git clone https://github.com/two4suited/FlyballStats.com.git
   cd FlyballStats.com
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore src/flyballstats.sln
   ```

3. **Build the Solution**
   ```bash
   dotnet build src/flyballstats.sln
   ```

### Running the Application

#### Method 1: Using Aspire CLI (Recommended)

The preferred way to run the application is using the .NET Aspire CLI, which automatically orchestrates all services:

```bash
# Run from repository root
aspire run

# Or specify the AppHost project explicitly
aspire run --project src/flyballstats.AppHost
```

This command will:
- Start all required services (Web, API, Service Defaults)
- Configure service discovery and health checks
- Launch the Aspire dashboard for monitoring
- Open the web application in your default browser

#### Method 2: Using Visual Studio

1. Set `flyballstats.AppHost` as the startup project
2. Press F5 or click "Start Debugging"
3. The Aspire dashboard will open automatically

#### Method 3: Individual Service Debugging

For isolated debugging of specific services:

```bash
# API Service only (runs on http://localhost:5000)
dotnet run --project src/flyballstats.ApiService

# Web Frontend (expects API service via service discovery)
dotnet run --project src/flyballstats.Web
```

### Accessing the Application

Once running, you can access:

- **Web Application**: https://localhost:7042 (or URL shown in console)
- **Aspire Dashboard**: https://localhost:15888 (monitoring and diagnostics)
- **API Documentation**: https://localhost:7042/openapi (development only)
- **Health Checks**: Available at `/health` endpoints (development only)

### Environment Configuration

The application uses standard ASP.NET Core configuration. Key settings can be configured via:

- `appsettings.json` - Default configuration
- `appsettings.Development.json` - Development overrides
- Environment variables - Production configuration
- Azure App Configuration - Cloud configuration management

## Development

### Project Structure

The solution follows .NET Aspire patterns with clear separation of concerns:

```
src/
├── flyballstats.AppHost/          # .NET Aspire orchestrator
│   ├── AppHost.cs                 # Service composition and configuration
│   └── appsettings.json           # Orchestrator settings
├── flyballstats.Web/              # Blazor Server web application
│   ├── Components/                # Blazor components and pages
│   ├── Models/                    # View models and DTOs
│   ├── Program.cs                 # Web application startup
│   └── TournamentApiClient.cs     # Typed HTTP client for API
├── flyballstats.ApiService/       # REST API service
│   ├── Models/                    # API models and DTOs
│   ├── Services/                  # Business logic services
│   ├── Program.cs                 # API service startup
│   └── flyballstats.ApiService.http # HTTP test file
├── flyballstats.ServiceDefaults/  # Shared Aspire configuration
│   └── Extensions.cs              # Service discovery, health, telemetry
└── flyballstats.Tests/            # Unit and integration tests
    └── *.Tests.cs                 # Test files
```

### Key Architecture Patterns

#### Service Discovery
Services communicate using Aspire's built-in service discovery. Example from Web to API:

```csharp
builder.Services.AddHttpClient<TournamentApiClient>(client =>
{
    // Service discovery resolves "apiservice" to the actual endpoint
    client.BaseAddress = new("https+http://apiservice");
});
```

#### Health Checks
All services implement health checks via `MapDefaultEndpoints()`:

```csharp
// Available at /health in development
app.MapDefaultEndpoints();
```

#### Observability
Comprehensive telemetry is automatically configured through ServiceDefaults:

- **Metrics**: Performance counters and custom metrics
- **Logging**: Structured logging with correlation IDs
- **Tracing**: Distributed request tracing across services

### Development Workflow

#### Local Development Setup

1. **Start the Application**
   ```bash
   aspire run
   ```

2. **Make Code Changes**
   - Edit files in any project
   - Hot reload is enabled for Blazor components
   - API changes require restart

3. **View Logs and Metrics**
   - Open Aspire dashboard at https://localhost:15888
   - Monitor service health, logs, and performance

#### Running Tests

```bash
# Run all tests
dotnet test src/flyballstats.sln

# Run tests with coverage
dotnet test src/flyballstats.sln --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test src/flyballstats.Tests/
```

#### Debugging Services

- **Aspire Dashboard**: Use for service logs, metrics, and tracing
- **Browser DevTools**: For Blazor client-side debugging
- **Visual Studio Debugger**: Set breakpoints in any service
- **HTTP Files**: Use `flyballstats.ApiService.http` for API testing

### Code Style and Conventions

- **C# Conventions**: Follow standard .NET coding conventions
- **Nullable Reference Types**: Enabled across all projects
- **Implicit Usings**: Enabled for cleaner code
- **File-Scoped Namespaces**: Used throughout the solution

## Deployment

### Azure Deployment

The application is designed for Azure cloud deployment with the following resources:

#### Required Azure Resources

- **Azure App Service**: Host for web application and API
- **Azure Cosmos DB**: NoSQL database for tournament data
- **Azure Key Vault**: Secure storage for connection strings and secrets
- **Azure Application Insights**: Application performance monitoring
- **Azure SignalR Service**: Scalable real-time messaging (optional)

#### Deployment Process

1. **Provision Azure Resources**
   ```bash
   # Using Azure CLI (example)
   az group create --name flyballstats-rg --location eastus
   az cosmosdb create --name flyballstats-cosmos --resource-group flyballstats-rg
   ```

2. **Configure Application Settings**
   - Set connection strings in Azure App Service configuration
   - Configure Microsoft Entra ID application registration
   - Set up custom domains and SSL certificates

3. **Deploy Application**
   - Use GitHub Actions workflow for automated deployment
   - Or deploy directly from Visual Studio/VS Code

### CI/CD Pipeline

The repository includes a GitHub Actions workflow (`.github/workflows/build-and-test.yml`) that:

- **Builds** the solution on every push and pull request
- **Runs Tests** with result reporting
- **Caches Dependencies** for faster builds
- **Supports .NET 9** with appropriate SDK setup

#### Workflow Triggers

- Push to `main` branch
- Pull requests to `main` branch
- Manual workflow dispatch
- Only triggers on changes to `src/` or workflow files

### Environment Configuration

Production deployment requires these environment variables:

```bash
# Database
COSMOS_CONNECTION_STRING=<your-cosmos-connection-string>

# Authentication
AZURE_AD_TENANT_ID=<your-tenant-id>
AZURE_AD_CLIENT_ID=<your-client-id>

# Application Insights
APPLICATIONINSIGHTS_CONNECTION_STRING=<your-insights-connection>

# OpenTelemetry (optional)
OTEL_EXPORTER_OTLP_ENDPOINT=<your-otlp-endpoint>
```

## API Documentation

### OpenAPI/Swagger Documentation

In development mode, the API provides interactive documentation:

- **URL**: https://localhost:7042/openapi
- **Interactive Testing**: Built-in request/response testing
- **Schema Export**: Download OpenAPI specification

### Key API Endpoints

#### Tournament Management
```http
POST /tournaments/upload-csv     # Upload tournament CSV
GET  /tournaments               # List all tournaments
GET  /tournaments/{id}          # Get specific tournament
```

#### Example API Usage

```bash
# Upload a tournament CSV
curl -X POST "https://localhost:7042/tournaments/upload-csv" \
  -H "Content-Type: application/json" \
  -d '{
    "tournamentId": "summer-2024",
    "tournamentName": "Summer Championship 2024",
    "csvContent": "Race,LeftTeam,RightTeam,Division\n1,Team A,Team B,Open"
  }'
```

### HTTP Test Files

Use the included HTTP test file for API exploration:
- **File**: `src/flyballstats.ApiService/flyballstats.ApiService.http`
- **Usage**: Open in Visual Studio, VS Code, or compatible HTTP client

## Contributing

We welcome contributions to FlyballStats.com! Please follow these guidelines:

### Getting Started with Contributions

1. **Fork the Repository**
   ```bash
   git clone https://github.com/your-username/FlyballStats.com.git
   ```

2. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Set Up Development Environment**
   - Follow the [Getting Started](#getting-started) guide
   - Ensure all tests pass: `dotnet test src/flyballstats.sln`

### Development Guidelines

- **Code Quality**: Follow existing code style and conventions
- **Testing**: Add tests for new features and bug fixes
- **Documentation**: Update relevant documentation for changes
- **Commit Messages**: Use clear, descriptive commit messages

### Pull Request Process

1. **Ensure Quality**
   - All tests pass
   - Code builds without warnings
   - Follow established patterns

2. **Create Pull Request**
   - Provide clear description of changes
   - Reference any related issues
   - Include screenshots for UI changes

3. **Code Review**
   - Address reviewer feedback promptly
   - Keep discussions constructive and collaborative

### Project Phases and Roadmap

The project follows a structured development approach outlined in the PRD:

- **Phase 1**: Core CSV import and validation ✅
- **Phase 2**: Ring configuration and management ✅
- **Phase 3**: Real-time operations and notifications 🚧
- **Phase 4**: Advanced features and optimization 📋
- **Phase 5**: Documentation and polish 📋

### Issue Reporting

When reporting issues:

- **Use Issue Templates**: Follow provided templates for bugs and features
- **Provide Context**: Include steps to reproduce, expected vs actual behavior
- **Environment Details**: Specify .NET version, OS, browser (for UI issues)
- **Logs**: Include relevant error messages and stack traces

## Troubleshooting

### Common Setup Issues

#### .NET 9 SDK Not Found
```bash
# Install .NET 9 SDK
winget install Microsoft.DotNet.SDK.9
# or download from https://get.dot.net
```
#### Platform-Specific Installation

**Windows:**
```bash
winget install Microsoft.DotNet.SDK.9
#### Aspire Workload Missing
```bash
dotnet workload install aspire
```

#### Port Conflicts
If you encounter port conflicts, update the configuration in:
- `src/flyballstats.AppHost/appsettings.json`
- Or set environment variables to override ports

#### Service Discovery Issues
Ensure all services are started through the AppHost:
```bash
aspire run  # Not individual service commands
```

### Development Environment Issues

#### Hot Reload Not Working
1. Ensure you're running in Development mode
2. Check that file watchers are not being blocked by antivirus
3. Restart the application if needed

#### Database Connection Errors
For local development, the application uses in-memory storage by default. For production:
1. Verify Cosmos DB connection string
2. Check network connectivity
3. Ensure proper authentication configuration

### Performance Issues

#### Slow Build Times
1. Use dependency caching: Builds should leverage NuGet package cache
2. Consider using self-hosted runners for CI/CD
3. Close unnecessary applications during development

#### Memory Usage
The application includes telemetry monitoring. Check the Aspire dashboard for:
- Memory usage patterns
- Garbage collection statistics
- Service health metrics

### Getting Help

- **GitHub Issues**: Report bugs and request features
- **GitHub Discussions**: Community questions and discussions
- **Documentation**: Check this README and inline code comments
- **Aspire Documentation**: [Official .NET Aspire docs](https://learn.microsoft.com/en-us/dotnet/aspire/)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### Third-Party Acknowledgments

- **.NET and ASP.NET Core**: Microsoft Corporation
- **Bootstrap**: Twitter, Inc. and the Bootstrap Authors
- **Azure Services**: Microsoft Corporation
- **SignalR**: Microsoft Corporation

---

## Project Status

🚧 **Active Development** - This project is actively being developed. Features and APIs may change.

For the latest updates and release information, check:
- [GitHub Releases](https://github.com/two4suited/FlyballStats.com/releases)
- [Project Board](https://github.com/two4suited/FlyballStats.com/projects)
- [Issues](https://github.com/two4suited/FlyballStats.com/issues)

---

**Built with ❤️ for the flyball community**