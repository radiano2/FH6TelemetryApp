using Microsoft.AspNetCore.SignalR;

namespace FH6TelemetryApp.Hubs;

/// <summary>
/// Clients connect here and receive "Telemetry" push messages from TelemetryBroadcaster.
/// No client-to-server methods are needed — this is a pure push hub.
/// </summary>
public sealed class TelemetryHub : Hub;
