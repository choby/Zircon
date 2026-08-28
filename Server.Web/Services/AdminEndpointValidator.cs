using System.Net;
using Server.Envir;

namespace Server.Web.Services;

public static class AdminEndpointValidator
{
    public static IPAddress ValidateAndResolve()
    {
        if (!Config.AdminWebEnabled)
            throw new InvalidOperationException("Server.Web 已禁用。请在 Server.ini 的 [AdminWeb] 中设置 AdminWebEnabled=True。");
        if (string.IsNullOrWhiteSpace(Config.AdminUserName))
            throw new InvalidOperationException("[AdminWeb] AdminUserName 不能为空。");
        if (string.IsNullOrEmpty(Config.MasterPassword))
            throw new InvalidOperationException("[System] MasterPassword 不能为空。");
        if (Config.AdminWebPort == 0)
            throw new InvalidOperationException("[AdminWeb] AdminWebPort 必须在 1 到 65535 之间。");

        HashSet<int> reservedPorts = [Config.Port, Config.UserCountPort];
        AddListenerPort(Config.WebPrefix, reservedPorts);
        AddListenerPort(Config.BuyPrefix, reservedPorts);
        AddListenerPort(Config.IPNPrefix, reservedPorts);

        if (reservedPorts.Contains(Config.AdminWebPort))
            throw new InvalidOperationException($"管理端口 {Config.AdminWebPort} 与现有服务器监听端口冲突。");

        if (string.Equals(Config.AdminWebHost, "localhost", StringComparison.OrdinalIgnoreCase))
            return IPAddress.Loopback;
        if (!IPAddress.TryParse(Config.AdminWebHost, out IPAddress? address))
            throw new InvalidOperationException($"AdminWebHost '{Config.AdminWebHost}' 不是有效的 IP 地址。");

        return address;
    }

    private static void AddListenerPort(string? value, ISet<int> ports)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        string normalized = value.Replace("*", "127.0.0.1", StringComparison.Ordinal);
        if (Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
            ports.Add(uri.Port);
    }
}
