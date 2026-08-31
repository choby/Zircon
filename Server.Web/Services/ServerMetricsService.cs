using Server.Envir;
using Server.Web.Models;

namespace Server.Web.Services;

public sealed class ServerMetricsService(IGameServerController controller) : BackgroundService
{
    private volatile ServerMetricsSnapshot _current = Empty(GameServerState.Stopped, null);

    public ServerMetricsSnapshot Current => _current;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
        do
        {
            try
            {
                _current = new ServerMetricsSnapshot(
                    DateTimeOffset.Now,
                    controller.State,
                    SEnvir.Connections?.Count ?? 0,
                    SEnvir.ActiveObjects?.Count ?? 0,
                    SEnvir.Objects?.Count ?? 0,
                    SEnvir.ProcessObjectCount,
                    SEnvir.LoopCount,
                    SEnvir.TotalBytesReceived,
                    SEnvir.TotalBytesSent,
                    SEnvir.DownloadSpeed,
                    SEnvir.UploadSpeed,
                    SEnvir.ConDelay,
                    SEnvir.SaveDelay,
                    EmailService.EMailsSent,
                    controller.LastError);
            }
            catch
            {
                _current = Empty(controller.State, controller.LastError);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private static ServerMetricsSnapshot Empty(GameServerState state, string? error) =>
        new(DateTimeOffset.Now, state, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, error);
}
