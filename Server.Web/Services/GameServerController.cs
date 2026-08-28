using Server.Envir;
using Server.Web.Models;
using System.Net;
using System.Net.Sockets;

namespace Server.Web.Services;

public interface IGameServerController
{
    GameServerState State { get; }
    string? LastError { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public sealed class GameServerController(ILogger<GameServerController> logger)
    : IGameServerController, IHostedService
{
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    public GameServerState State => SEnvir.LifecycleState switch
    {
        ServerLifecycleState.Starting => GameServerState.Starting,
        ServerLifecycleState.Running => GameServerState.Running,
        ServerLifecycleState.Stopping => GameServerState.Stopping,
        ServerLifecycleState.Faulted => GameServerState.Faulted,
        _ => GameServerState.Stopped
    };
    public string? LastError => SEnvir.LastStartupException?.ToString();

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (State is GameServerState.Starting or GameServerState.Running) return;
            SEnvir.UseLogConsole = false;
            ValidatePreflight();
            await SEnvir.StartServerAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start the game server");
            throw;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (SEnvir.EnvirThread is null)
            {
                return;
            }
            await SEnvir.StopServerAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stop the game server");
            throw;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        if (Config.AdminAutoStartGameServer)
            await StartAsync(cancellationToken);
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        if (SEnvir.EnvirThread is not null)
            await StopAsync(cancellationToken);
    }

    private static void ValidatePreflight()
    {
        if (!IPAddress.TryParse(Config.IPAddress, out IPAddress? address))
            throw new InvalidOperationException($"游戏监听地址 '{Config.IPAddress}' 无效。");
        if (Config.Port == 0 || Config.UserCountPort == 0 || Config.Port == Config.UserCountPort)
            throw new InvalidOperationException("游戏端口和用户统计端口必须是不同的非零端口。");

        ValidatePort(address, Config.Port, "游戏端口");
        ValidatePort(address, Config.UserCountPort, "用户统计端口");

        string database = Path.Combine(MirDB.Session.ExecutionRoot, "System.db");
        if (!File.Exists(database)) throw new FileNotFoundException("找不到 System.db。", database);
        if (!Directory.Exists(Library.PlatformPath.Resolve(Config.MapPath)))
            Directory.CreateDirectory(Library.PlatformPath.Resolve(Config.MapPath));
    }

    private static void ValidatePort(IPAddress address, int port, string name)
    {
        TcpListener listener = new(address, port);
        try { listener.Start(); }
        catch (Exception ex) { throw new InvalidOperationException($"{name} {address}:{port} 无法绑定。", ex); }
        finally { listener.Stop(); }
    }
}
