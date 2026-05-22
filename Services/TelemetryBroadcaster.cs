using System.Diagnostics;
using FH6TelemetryApp.Hubs;
using FH6TelemetryApp.Models;
using Microsoft.AspNetCore.SignalR;

namespace FH6TelemetryApp.Services;

/// <summary>
/// Receives parsed packets from UdpListenerService (at ~60 fps) and forwards
/// to all SignalR clients throttled to 30 fps.
/// </summary>
public sealed class TelemetryBroadcaster(IHubContext<TelemetryHub> hub)
{
    // 33.33 ms in Stopwatch ticks
    private static readonly long s_intervalTicks = Stopwatch.Frequency / 30;

    private long _lastTicks;

    /// <summary>Latest received packet, regardless of broadcast throttle.</summary>
    public TelemetryPacket? Latest { get; private set; }

    /// <summary>
    /// Fired at ~30 fps on the thread-pool for any Blazor Server component that
    /// wants live updates without going through a WebSocket round-trip.
    /// Handlers must call InvokeAsync(StateHasChanged) to marshal back to the
    /// Blazor synchronization context.
    /// </summary>
    public event Action<TelemetryPacket>? PacketBroadcast;

    public async Task OnPacketReceivedAsync(TelemetryPacket packet)
    {
        Latest = packet;

        var now = Stopwatch.GetTimestamp();
        if (now - _lastTicks < s_intervalTicks)
            return;

        _lastTicks = now;
        PacketBroadcast?.Invoke(packet);
        await hub.Clients.All.SendAsync("Telemetry", packet);
    }
}
