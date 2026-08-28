using Plugin.Abstractions;

namespace Server.Web.Models;

public sealed record PluginStatus(
    string Id,
    string Name,
    string Version,
    bool Enabled,
    bool Loaded,
    string? Error,
    IReadOnlyList<PluginNavigationItem> Navigation);
