using FH6TelemetryApp.Components;
using FH6TelemetryApp.Hubs;
using FH6TelemetryApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

// Telemetry pipeline
builder.Services.AddSingleton<TelemetryBroadcaster>();
builder.Services.AddHostedService<UdpListenerService>();

// MongoDB + repositories
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<LayoutRepository>();
builder.Services.AddSingleton<SessionRepository>();

// Diagnostics — singleton so auto-detection runs independently of browser tabs
builder.Services.AddSingleton<DiagnosticsRecorder>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHub<TelemetryHub>("/telemetryhub");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
