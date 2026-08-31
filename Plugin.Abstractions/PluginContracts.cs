using Microsoft.AspNetCore.Components;

namespace Plugin.Abstractions;

public interface IServerPlugin
{
    ValueTask InitializeAsync(IPluginContext context, CancellationToken cancellationToken);
    ValueTask StopAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

public interface IPluginContext
{
    string PluginId { get; }
    void Log(string message);
    void AddNavigation(PluginNavigationItem item);
    void AddGridAction(PluginGridAction action);
    Task<object?> DispatchAsync(string command, object? payload, CancellationToken cancellationToken = default);
    void OpenMap(string mapFileName);
}

public sealed record PluginNavigationItem(
    string Key,
    string Title,
    string Route,
    Type ComponentType,
    string? Icon = null)
{
    public bool IsValidComponent => typeof(IComponent).IsAssignableFrom(ComponentType);
}

public sealed record PluginGridAction(
    string Key,
    string Title,
    string EntityType,
    Func<object, CancellationToken, ValueTask> ExecuteAsync);
