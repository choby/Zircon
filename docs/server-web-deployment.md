# Server.Web 跨平台部署

## 技术结论

地图编辑器和插件均有可行的 Web 替代方案，且不与 Blazor 冲突。因此管理端采用 .NET 10 Blazor Interactive Server；管理组件使用 Telerik UI for Blazor 15.0.0 评估版本，地图画布使用本地打包的 PixiJS 8.20.1。服务端代码不依赖 WinForms、DevExpress 或 Windows API。

## 发布与启动

在仓库根目录执行：

```bash
dotnet publish Server.Web/Server.Web.csproj -c Release -o publish/Server.Web
cd publish/Server.Web
dotnet Server.Web.dll
```

这是 framework-dependent 跨平台发布包，目标 Linux、macOS 或 Windows 主机需要安装 .NET 10 ASP.NET Core Runtime。工作目录不影响静态资源和配置路径；`Server.ini`、`System.db`、`Users.db` 均位于程序执行目录，地图和插件路径相对于该目录解析。数据库备份固定保存在执行目录下的 `Backup/System` 和 `Backup/Users`。

## Server.ini

```ini
[System]
MasterPassword=replace-with-existing-master-password

[AdminWeb]
AdminWebEnabled=True
AdminWebHost=127.0.0.1
AdminWebPort=8080
AdminUserName=admin
AdminAutoStartGameServer=False
```

管理端登录密码直接使用现有 `MasterPassword`。数据库目录和备份目录不再通过 `Server.ini` 配置；部署或升级时应把现有 `System.db`、`Users.db` 移至程序执行目录。`AdminWebPort` 必须为 1–65535，且不能与游戏端口、用户统计端口、商城回调、购买或 IPN 监听端口重复。修改管理员账号、密码、主机或端口后重启进程；修改账号或密码会使旧登录会话失效。

默认只监听回环地址。需要远程管理时，推荐仍监听回环地址并由 HTTPS 反向代理转发；只有可信本机代理的转发头会被接受。数据保护密钥保存在发布目录的 `DataProtectionKeys`，应限制为服务账号可读写。

## 插件

每个插件放在 `Plugins/<plugin-id>/`，包含入口程序集和 `plugin.json`：

```json
{
  "schemaVersion": 1,
  "id": "sample-plugin",
  "name": "Sample Plugin",
  "version": "1.0.0",
  "entryAssembly": "Sample.Plugin.dll",
  "enabled": true
}
```

入口类型实现 `Plugin.Abstractions.IServerPlugin`。插件可以注册 Blazor 页面、数据行操作、日志和地图打开请求，并只能调用白名单中的游戏服启动、停止和状态命令。静态资源放在插件目录的 `wwwroot`。清单启停在进程重启后生效；故障排查时使用 `dotnet Server.Web.dll --safe-mode` 禁止加载全部插件。

## 运维与验收

- `/health/live` 用于进程存活探测，不需要登录且不暴露业务数据。
- 管理操作写入发布目录下的审计日志；System.db 修改和 Users.db 修改均执行并发检查。
- 迁移验收以 `docs/server-web-parity.md` 为唯一逐项对照表。
- Telerik 保持评估版本配置；迁移过程不执行商业授权检查，也不绕过或伪造运行时授权状态。
