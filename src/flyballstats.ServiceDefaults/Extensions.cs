using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        // Add custom metrics service
        builder.Services.AddSingleton<ApplicationMetrics>();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("FlyballStats.Application"); // Add custom application metrics
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Add Cosmos DB health check for services that use it
    /// </summary>
    public static TBuilder AddCosmosDbHealthCheck<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("cosmosdb", () =>
            {
                try
                {
                    // Basic connectivity check - in a real implementation this would ping the DB
                    // For now, we'll just verify the connection string is configured
                    var connectionString = builder.Configuration["ConnectionStrings:cosmos-db"];
                    return !string.IsNullOrEmpty(connectionString) 
                        ? HealthCheckResult.Healthy("Cosmos DB connection string configured")
                        : HealthCheckResult.Degraded("Cosmos DB connection string not found");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Cosmos DB health check failed", ex);
                }
            }, ["ready", "cosmos"]);

        return builder;
    }

    /// <summary>
    /// Add SignalR health check for services that use it
    /// </summary>
    public static TBuilder AddSignalRHealthCheck<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("signalr", () =>
            {
                try
                {
                    // Basic connectivity check - in a real implementation this would ping the service
                    var connectionString = builder.Configuration["ConnectionStrings:signalr"];
                    return !string.IsNullOrEmpty(connectionString)
                        ? HealthCheckResult.Healthy("SignalR connection string configured")
                        : HealthCheckResult.Degraded("SignalR connection string not found");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("SignalR health check failed", ex);
                }
            }, ["ready", "signalr"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Health checks are important for monitoring and should be accessible with proper security considerations
        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks(HealthEndpointPath, new HealthCheckOptions
        {
            AllowCachingResponses = false,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        duration = x.Value.Duration.TotalMilliseconds,
                        description = x.Value.Description
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            AllowCachingResponses = false
        });

        return app;
    }
}

/// <summary>
/// Application-specific metrics for FlyballStats operations
/// </summary>
public class ApplicationMetrics
{
    private readonly Meter _meter;
    private readonly Counter<int> _tournamentImports;
    private readonly Counter<int> _raceAssignments;
    private readonly Counter<int> _ringUpdates;
    private readonly Counter<int> _notifications;
    private readonly Histogram<double> _operationDuration;

    public ApplicationMetrics()
    {
        _meter = new Meter("FlyballStats.Application");
        
        _tournamentImports = _meter.CreateCounter<int>(
            "flyballstats_tournament_imports_total",
            description: "Total number of tournament CSV imports");
            
        _raceAssignments = _meter.CreateCounter<int>(
            "flyballstats_race_assignments_total", 
            description: "Total number of race assignments");
            
        _ringUpdates = _meter.CreateCounter<int>(
            "flyballstats_ring_updates_total",
            description: "Total number of ring configuration updates");
            
        _notifications = _meter.CreateCounter<int>(
            "flyballstats_notifications_total",
            description: "Total number of real-time notifications sent");
            
        _operationDuration = _meter.CreateHistogram<double>(
            "flyballstats_operation_duration_ms",
            description: "Duration of FlyballStats operations in milliseconds");
    }

    public void RecordTournamentImport(bool success, string? errorType = null)
    {
        _tournamentImports.Add(1, new KeyValuePair<string, object?>("status", success ? "success" : "failure"),
                                  new KeyValuePair<string, object?>("error_type", errorType ?? "none"));
    }

    public void RecordRaceAssignment(bool success, string operation = "assign")
    {
        _raceAssignments.Add(1, new KeyValuePair<string, object?>("status", success ? "success" : "failure"),
                               new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordRingUpdate(bool success, string operation = "configure")
    {
        _ringUpdates.Add(1, new KeyValuePair<string, object?>("status", success ? "success" : "failure"),
                           new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordNotification(bool success, string type = "race_assignment")
    {
        _notifications.Add(1, new KeyValuePair<string, object?>("status", success ? "success" : "failure"),
                             new KeyValuePair<string, object?>("type", type));
    }

    public void RecordOperationDuration(string operation, double durationMs)
    {
        _operationDuration.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
    }
}
