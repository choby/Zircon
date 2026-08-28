using System.Reflection;
using System.Text.Json;
using Plugin.Abstractions;
using Server.Web.Models;
using Server.Web.Plugins;

namespace Server.Web.Services;

public sealed class PluginRegistry(
    IGameServerController gameServer,
    AdminAuditService audit,
    ILogger<PluginRegistry> logger) : IHostedService
{
    private readonly List<LoadedPlugin> _loaded = [];
    private readonly List<PluginStatus> _statuses = [];
    private readonly List<PluginGridAction> _gridActions = [];
    private readonly Dictionary<string, string> _manifestPaths = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PluginStatus> Statuses => _statuses.ToArray();
    public IReadOnlyList<PluginGridAction> GridActions => _gridActions.ToArray();
    public event Action<string>? MapRequested;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Environment.GetCommandLineArgs().Any(arg => string.Equals(arg, "--safe-mode", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning("Plugin safe mode is active; no plugins were loaded.");
            return;
        }

        string root = Path.Combine(AppContext.BaseDirectory, "Plugins");
        if (!Directory.Exists(root)) return;

        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        foreach (string manifestPath in Directory.EnumerateFiles(root, "plugin.json", SearchOption.AllDirectories))
        {
            PluginManifest? manifest = null;
            List<PluginNavigationItem> navigation = [];
            try
            {
                manifest = JsonSerializer.Deserialize<PluginManifest>(await File.ReadAllTextAsync(manifestPath, cancellationToken));
                ValidateManifest(manifest, ids);
                _manifestPaths[manifest!.Id] = manifestPath;
                if (!manifest.Enabled)
                {
                    _statuses.Add(new PluginStatus(manifest.Id, manifest.Name, manifest.Version, false, false, null, []));
                    continue;
                }

                string pluginDirectory = Path.GetDirectoryName(manifestPath)!;
                string assemblyPath = Path.GetFullPath(Path.Combine(pluginDirectory, manifest.EntryAssembly));
                if (Path.GetRelativePath(pluginDirectory, assemblyPath).StartsWith("..", StringComparison.Ordinal))
                    throw new InvalidDataException("Plugin entry assembly escapes its plugin directory.");

                PluginLoadContext loadContext = new(assemblyPath);
                Assembly assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
                Type entryType = assembly.GetTypes().Single(type => !type.IsAbstract && typeof(IServerPlugin).IsAssignableFrom(type));
                IServerPlugin plugin = (IServerPlugin)Activator.CreateInstance(entryType)!;
                PluginContext context = new(this, gameServer, manifest.Id, navigation, _gridActions, logger);
                await plugin.InitializeAsync(context, cancellationToken);
                _loaded.Add(new LoadedPlugin(manifest, plugin, loadContext));
                _statuses.Add(new PluginStatus(manifest.Id, manifest.Name, manifest.Version, true, true, null, navigation));
                audit.Record("system", "Plugin.Load", $"{manifest.Id} {manifest.Version}");
            }
            catch (Exception ex)
            {
                string id = manifest?.Id ?? Path.GetFileName(Path.GetDirectoryName(manifestPath)) ?? "unknown";
                _statuses.Add(new PluginStatus(id, manifest?.Name ?? id, manifest?.Version ?? "?", manifest?.Enabled ?? false, false, ex.Message, navigation));
                logger.LogError(ex, "Failed to load plugin manifest {Manifest}", manifestPath);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (LoadedPlugin plugin in _loaded.AsEnumerable().Reverse())
        {
            try { await plugin.Instance.StopAsync(cancellationToken); }
            catch (Exception ex) { logger.LogError(ex, "Failed to stop plugin {Plugin}", plugin.Manifest.Id); }
        }
    }

    public PluginNavigationItem? FindPage(string pluginId, string pageKey) => _statuses
        .FirstOrDefault(status => string.Equals(status.Id, pluginId, StringComparison.OrdinalIgnoreCase))?
        .Navigation.FirstOrDefault(item => string.Equals(item.Key, pageKey, StringComparison.OrdinalIgnoreCase));

    public async Task SetEnabledAsync(string pluginId, bool enabled, string user, CancellationToken cancellationToken = default)
    {
        if (!_manifestPaths.TryGetValue(pluginId, out string? path)) throw new KeyNotFoundException("找不到插件清单。");
        PluginManifest manifest = JsonSerializer.Deserialize<PluginManifest>(await File.ReadAllTextAsync(path, cancellationToken)) ??
                                  throw new InvalidDataException("插件清单为空。");
        manifest = manifest with { Enabled = enabled };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);

        int index = _statuses.FindIndex(status => string.Equals(status.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) _statuses[index] = _statuses[index] with { Enabled = enabled };
        audit.Record(user, "Plugin.SetEnabled", $"{pluginId}: {enabled}");
    }

    public string? ResolveAsset(string pluginId, string relativePath)
    {
        if (!_manifestPaths.TryGetValue(pluginId, out string? manifestPath)) return null;
        string root = Path.Combine(Path.GetDirectoryName(manifestPath)!, "wwwroot");
        string target = Path.GetFullPath(Path.Combine(root, relativePath));
        if (Path.GetRelativePath(root, target).StartsWith("..", StringComparison.Ordinal) || !File.Exists(target)) return null;
        return target;
    }

    private static void ValidateManifest(PluginManifest? manifest, ISet<string> ids)
    {
        if (manifest is null) throw new InvalidDataException("Plugin manifest is empty.");
        if (manifest.SchemaVersion != PluginManifest.CurrentSchemaVersion) throw new InvalidDataException("Unsupported plugin manifest schema.");
        if (string.IsNullOrWhiteSpace(manifest.Id) || !manifest.Id.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_'))
            throw new InvalidDataException("Plugin id contains invalid characters.");
        if (!ids.Add(manifest.Id)) throw new InvalidDataException($"Duplicate plugin id: {manifest.Id}");
        if (string.IsNullOrWhiteSpace(manifest.EntryAssembly)) throw new InvalidDataException("Plugin entryAssembly is required.");
    }

    private sealed record LoadedPlugin(PluginManifest Manifest, IServerPlugin Instance, PluginLoadContext LoadContext);

    private sealed class PluginContext(
        PluginRegistry owner,
        IGameServerController gameServer,
        string pluginId,
        List<PluginNavigationItem> navigation,
        List<PluginGridAction> gridActions,
        ILogger logger) : IPluginContext
    {
        public string PluginId => pluginId;
        public void Log(string message) => logger.LogInformation("Plugin {Plugin}: {Message}", pluginId, message);

        public void AddNavigation(PluginNavigationItem item)
        {
            if (!item.IsValidComponent) throw new InvalidOperationException($"{item.ComponentType} is not a Blazor component.");
            navigation.Add(item);
        }

        public void AddGridAction(PluginGridAction action) => gridActions.Add(action);

        public async Task<object?> DispatchAsync(string command, object? payload, CancellationToken cancellationToken = default)
        {
            switch (command)
            {
                case "server.start": await gameServer.StartAsync(cancellationToken); return null;
                case "server.stop": await gameServer.StopAsync(cancellationToken); return null;
                case "server.state": return gameServer.State.ToString();
                default: throw new InvalidOperationException($"Plugin command is not allowed: {command}");
            }
        }

        public void OpenMap(string mapFileName) => owner.MapRequested?.Invoke(mapFileName);
    }
}
