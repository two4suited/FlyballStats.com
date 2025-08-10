# Copilot Instructions for FlyballStats.com (.NET Aspire)

## Big picture (Aspire architecture)
- This is a .NET Aspire distributed app (net9) orchestrated by an AppHost.
  - `flyballstats.AppHost`: Aspire orchestrator; wires projects, dependencies, health, and exposure.
  - `flyballstats.Web`: ASP.NET Core with Razor Components (interactive server) UI.
  - `flyballstats.ApiService`: ASP.NET Core Web API (weather sample + OpenAPI in dev).
  - `flyballstats.ServiceDefaults`: Shared Aspire defaults (service discovery, resilience, health, OpenTelemetry exporters).
- AppHost composition (`src/flyballstats.AppHost/AppHost.cs`):
  - Adds `apiservice` and `webfrontend`; `webfrontend` references and waits for `apiservice`.
  - `WithExternalHttpEndpoints()` only on `webfrontend`. API is internal (service-discovery only).
  - Health checks at `/health` on both services (dev-only mapping in ServiceDefaults).

## How services talk (service discovery & resilience)
- Default HttpClient uses service discovery and standard resilience from `ServiceDefaults`.
- Example: `Web/WeatherApiClient.cs` BaseAddress is `https+http://apiservice` — HTTPS preferred if available.
- Don’t hardcode host:ports; use the service name (e.g., `apiservice`) and let Aspire route.

## Run, build, debug
- From repo root:
  - Build: `dotnet build src/flyballstats.sln`
  - Run all (recommended): `aspire run` (auto-detects the AppHost in this repo)
  - Run individual services (only for isolated debugging):
    - `dotnet run --project src/flyballstats.ApiService`
    - `dotnet run --project src/flyballstats.Web` (expects `apiservice` via service discovery)
- Debug: set startup project to `flyballstats.AppHost` so endpoints/discovery are configured.
- HTTP tests: `src/flyballstats.ApiService/flyballstats.ApiService.http` (prefer running via AppHost; API isn’t externally exposed by default).

### Run with Aspire CLI (preferred)
- Prerequisite: .NET SDK 9.0.302 or newer installed (see https://get.dot.net). If missing, `aspire run` will fail.
- Run everything from the repo root: `aspire run`
- Open/start dashboard (if it doesn’t auto-open): `aspire dashboard --open`
- Optional: if auto-detection fails, specify the AppHost: `aspire run --project src/flyballstats.AppHost`

## Conventions in this repo
- Every service calls `builder.AddServiceDefaults();` and maps `app.MapDefaultEndpoints();` (dev-only `/health` and `/alive`).
- OpenAPI is dev-only in API (`app.MapOpenApi()` inside `IsDevelopment()`).
- UI uses Razor Components with interactive server render mode and OutputCache/Antiforgery.
- Telemetry: If `OTEL_EXPORTER_OTLP_ENDPOINT` is set, OpenTelemetry exporters are enabled (see `ServiceDefaults/Extensions.cs`).

## Key files to learn patterns fast
- App orchestration: `src/flyballstats.AppHost/AppHost.cs` (AddProject, WithExternalHttpEndpoints, WithReference, WaitFor).
- Shared defaults: `src/flyballstats.ServiceDefaults/Extensions.cs` (service discovery, resilience, OTel, health mapping rules).
- Web entry: `src/flyballstats.Web/Program.cs` (Razor Components server, typed HttpClient to `apiservice`).
- API entry: `src/flyballstats.ApiService/Program.cs` (sample `/weatherforecast`, OpenAPI dev gate, default endpoints).
- API client sample: `src/flyballstats.Web/WeatherApiClient.cs` (discovery + resilience by default).

## Adding a new service (quick recipe)
1) Create a new project under `src/`, ensure it calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`.
2) Register it in `AppHost.cs` with `builder.AddProject<Projects.YourProj>("servicename")`; expose externally only if needed.
3) For callers, inject a typed HttpClient and set BaseAddress to `https+http://servicename`.

Notes
- Only `webfrontend` is externally reachable by default; the API is internal to the Aspire network. Expose deliberately.
- Health endpoints are mapped only in Development (see comments in `ServiceDefaults`).

If any section is unclear or missing details you expect, tell me what to refine and I’ll tighten it up.
