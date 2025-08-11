using flyballstats.Web;
using flyballstats.Web.Components;
using flyballstats.Web.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add dependency-specific health checks
builder.AddSignalRHealthCheck();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddOutputCache();

// Add custom services
builder.Services.AddSingleton<ErrorReportService>();
builder.Services.AddSingleton<RealTimeService>();



builder.Services.AddHttpClient<TournamentApiClient>(client =>
    {
        // Use direct localhost URL for development without Aspire
        var baseAddress = builder.Environment.IsDevelopment() 
            ? "http://localhost:5000" 
            : "https+http://apiservice";
        client.BaseAddress = new(baseAddress);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.UseStaticFiles();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
