
# FlyballStats.com

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/two4suited/FlyballStats.com)

FlyballStats.com is a modern, real-time system for managing flyball racing tournaments. It helps Tournament Directors, Race Directors, and participants organize schedules, track races, and manage ring operations efficiently during events.

With FlyballStats.com, you can:

- Upload and validate tournament schedules
- Configure rings and race order
- Monitor live race progress and results
- Receive real-time notifications and updates
- Access race data and standings instantly

The platform is designed to simplify tournament management, reduce manual errors, and provide a seamless experience for everyone involved in flyball racing events.

**Built for the flyball community to make tournaments run smoother and more enjoyable.**

## 🚀 Quick Start with GitHub Codespaces

The fastest way to get started is using GitHub Codespaces:

1. Click the **"Open in GitHub Codespaces"** badge above
2. Wait for the dev container to build (includes .NET 9.0 and Aspire CLI)
3. Once ready, open a terminal and run:
   ```bash
   cd src
   aspire run
   ```
4. Access the Aspire dashboard and web application through the forwarded ports

## 🛠️ Local Development

### Prerequisites
- .NET 9.0 SDK
- Aspire workload (`dotnet workload install aspire`)

### Running the Application
```bash
cd src
aspire run
```

Or run directly with dotnet:
```bash
cd src
dotnet run --project flyballstats.AppHost
```