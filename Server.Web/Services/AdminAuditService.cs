using System.Collections.Concurrent;
using System.Text.Json;

namespace Server.Web.Services;

public sealed record AdminAuditEntry(DateTimeOffset Timestamp, string User, string Action, string Detail, string? Address);

public sealed class AdminAuditService
{
    private readonly ConcurrentQueue<AdminAuditEntry> _entries = new();
    private readonly object _fileGate = new();
    private readonly string _path = Path.Combine(AppContext.BaseDirectory, "Audit", "admin-audit.jsonl");

    public AdminAuditService()
    {
        try
        {
            if (!File.Exists(_path)) return;
            foreach (string line in File.ReadLines(_path).TakeLast(2_000))
            {
                AdminAuditEntry? entry = JsonSerializer.Deserialize<AdminAuditEntry>(line);
                if (entry is not null) _entries.Enqueue(entry);
            }
        }
        catch
        {
            // A damaged historical audit file must not prevent the administration plane from starting.
        }
    }

    public void Record(string user, string action, string detail, string? address = null)
    {
        AdminAuditEntry entry = new(DateTimeOffset.Now, user, action, detail, address);
        _entries.Enqueue(entry);
        while (_entries.Count > 2_000) _entries.TryDequeue(out _);

        lock (_fileGate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, JsonSerializer.Serialize(entry) + Environment.NewLine);
        }
    }

    public IReadOnlyList<AdminAuditEntry> GetEntries() => _entries.Reverse().ToArray();
}
