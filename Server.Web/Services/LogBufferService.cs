using Server.Envir;

namespace Server.Web.Services;

public sealed class LogBufferService : BackgroundService
{
    private readonly object _sync = new();
    private readonly List<string> _system = [];
    private readonly List<string> _chat = [];
    private const int SystemLimit = 2_000;
    private const int ChatLimit = 5_000;

    public IReadOnlyList<string> GetSystemLogs() => Snapshot(_system);
    public IReadOnlyList<string> GetChatLogs() => Snapshot(_chat);

    public void ClearSystem()
    {
        lock (_sync) _system.Clear();
    }

    public void ClearChat()
    {
        lock (_sync) _chat.Clear();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(250));
        do
        {
            Drain(SEnvir.DisplayLogs, _system, SystemLimit);
            Drain(SEnvir.DisplayChatLogs, _chat, ChatLimit);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private void Drain(System.Collections.Concurrent.ConcurrentQueue<string> queue, List<string> target, int limit)
    {
        lock (_sync)
        {
            while (queue.TryDequeue(out string? line)) target.Add(line);
            if (target.Count > limit) target.RemoveRange(0, target.Count - limit);
        }
    }

    private IReadOnlyList<string> Snapshot(List<string> source)
    {
        lock (_sync) return source.ToArray();
    }
}
