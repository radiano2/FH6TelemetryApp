using System.Net;
using System.Net.Sockets;
using FH6TelemetryApp.Models;

namespace FH6TelemetryApp.Services;

public sealed class UdpListenerService(
    IConfiguration config,
    TelemetryBroadcaster broadcaster,
    ILogger<UdpListenerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var port    = config.GetValue<int>("Telemetry:UdpPort", 20440);
        var address = config.GetValue<string>("Telemetry:ListenAddress", "0.0.0.0")!;

        using var udp = new UdpClient(new IPEndPoint(IPAddress.Parse(address), port));
        logger.LogInformation("UDP listener bound to {Address}:{Port}", address, port);

        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await udp.ReceiveAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "UDP receive error");
                continue;
            }

            if (!TelemetryParser.TryParse(result.Buffer, out TelemetryPacket packet))
            {
                logger.LogDebug("Dropped short packet ({Len} bytes)", result.Buffer.Length);
                continue;
            }

            // Skip idle packets (IsRaceOn == 0) to avoid flooding with menu data
            if (packet.IsRaceOn == 0)
                continue;

            await broadcaster.OnPacketReceivedAsync(packet);
        }

        logger.LogInformation("UDP listener stopped");
    }
}
