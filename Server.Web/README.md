# Server.Web

`Server.Web` 是 Zircon《传奇 3》服务端的中文 Web 管理后台。它基于 ASP.NET Core Blazor Server 构建，将旧版 `Server` 管理界面的常用能力迁移到浏览器中，并直接复用 `ServerLibrary` 的服务器运行环境、MirDB 数据模型和配置体系。

该项目既是管理界面，也是游戏服的宿主进程：管理员可以从网页启动或停止游戏服，并在同一进程内管理配置、游戏资料和运行时数据。请勿将它与旧版 `Server` 同时连接到同一套数据库。

## 主要功能

- 查看游戏服状态、在线人数、运行时间、内存、线程和封包统计。
- 启动、停止游戏服，查看系统日志与聊天日志。
- 按分类编辑 `Server.ini`，并区分立即生效、重启游戏服生效和重启管理后台生效的配置。
- 管理 `Database/System.db` 中的地图、区域、怪物、物品、技能、任务、NPC、商店、掉落等游戏资料。
- 管理运行中 `Database/Users.db` 的账号、角色、邮件、支付、商店销售和攻城统计等数据。
- 使用 WebGL 地图编辑器查看地图贴图并编辑地图区域。
- 导入、导出游戏资料 JSON，检查和清理失效的数据引用。
- 查看管理员审计记录，加载扩展管理页面和表格操作的插件。

界面文案、字段标题和常用枚举值已中文化；底层配置键、枚举值、对象索引和数据库引用仍保持原值，避免影响协议、脚本和数据依赖。

## 技术组成

| 项目 | 说明 |
| --- | --- |
| 运行时 | .NET 10 / ASP.NET Core |
| UI | Blazor Interactive Server、Radzen.Blazor |
| 地图贴图 | SkiaSharp、BCnEncoder.Net |
| 游戏逻辑 | `ServerLibrary` |
| 数据与通用模型 | `LibraryCore` / MirDB |
| 插件契约 | `Plugin.Abstractions` |

## 运行前准备

1. 安装 .NET 10 SDK。
2. 准备与 Zircon 服务端一致的运行目录，其中至少应包含有效的 `Server.ini`、`Database` 和 `Map`；如需在地图编辑器中显示贴图，还需准备客户端资源库。
3. 备份 `Database/System.db` 和 `Database/Users.db`。
4. 停止正在使用同一运行目录的旧版 `Server` 或其他数据库写入程序。

配置和数据路径均以程序输出目录为基准，而不是以仓库根目录为基准。默认布局如下：

```text
Server.Web/
├── Server.ini
├── Database/
│   ├── System.db
│   └── Users.db
├── Backup/
├── Map/
├── Plugins/
├── Audit/
│   └── admin-audit.jsonl
└── DataProtectionKeys/
```

## 管理后台配置

`Server.Web` 复用服务端的 `Server.ini`。首次启动前，应至少检查以下配置：

```ini
[System]
MapPath=.\Map\
ClientPath=客户端资源目录
MasterPassword=请设置高强度管理密码

[AdminWeb]
AdminWebEnabled=True
AdminWebHost=127.0.0.1
AdminWebPort=8080
AdminUserName=admin
AdminAutoStartGameServer=False
```

- 登录账号来自 `AdminUserName`，登录密码复用 `[System]` 下的 `MasterPassword`。
- `AdminWebHost` 仅接受 `localhost` 或明确的 IP 地址。
- `AdminWebPort` 不能与游戏端口、在线人数端口以及已启用的 Web、购买或 IPN 监听端口冲突。
- 建议默认只监听 `127.0.0.1`。需要远程访问时，应在前方部署 HTTPS 反向代理，并限制来源地址；程序只信任来自本机回环地址的一层转发头。
- `AdminAutoStartGameServer=True` 会在管理后台启动后自动启动游戏服。

## 构建与运行

在解决方案根目录执行：

```bash
dotnet restore Server.Web/Server.Web.csproj
dotnet build Server.Web/Server.Web.csproj
dotnet run --project Server.Web/Server.Web.csproj
```

Debug 和 Release 的默认输出目录分别为解决方案上级目录中的 `Debug/Server.Web` 和 `Release/Server.Web`。请将现有服务端运行文件复制或部署到对应输出目录，再根据 `Server.ini` 中的监听地址访问登录页，例如：

```text
http://127.0.0.1:8080/login
```

发布独立目录：

```bash
dotnet publish Server.Web/Server.Web.csproj -c Release -o ./publish/server-web
```

如果某个插件导致后台无法启动，可使用安全模式跳过全部插件：

```bash
dotnet Server.Web.dll --safe-mode
```

## 数据安全与一致性

- `System.db` 由后台持有单一可写会话；新增、修改、删除和关联编辑都会通过 MirDB 保存并写入审计记录。
- 指定位置插入记录会移动对象索引及 `Users.db` 引用，因此只有游戏服停止时才允许执行。
- `Users.db` 的运行时修改会被调度到游戏线程，并使用 ETag 检查并发变化；发现数据已更新时会拒绝覆盖。
- 密码、密钥和安全问题答案等敏感字段不会发送到浏览器，也不能通过运行时数据表修改。
- 地图区域编辑只更新 `MapRegion` 数据，不会改写原始 `.map` 文件。
- 批量导入、清理失效引用、修改数据库加密设置或执行大范围编辑前，请额外保存一份可恢复备份。
- 不要同时运行旧版 `Server`、另一个 `Server.Web` 实例或其他直接写库工具。

## 安全说明

- 管理接口使用 Cookie 登录、请求防伪校验和基于远端 IP 的登录限流；同一地址每 15 分钟最多尝试登录 5 次。
- 修改管理员账号或主密码后，现有登录状态会失效。
- 数据保护密钥保存在 `DataProtectionKeys`，管理员操作日志保存在 `Audit/admin-audit.jsonl`；部署和备份时应保护这两个目录的访问权限。
- `/health/live` 是无需登录的存活检查，只返回服务状态和时间；其他地图、区域和插件资源接口均要求管理员登录。
- 直接暴露到非受信网络前，必须配置 HTTPS、防火墙或访问控制，不要依赖默认 HTTP 监听承担传输加密。

## 插件

插件放在输出目录的 `Plugins` 子目录中，每个插件目录需要包含 `plugin.json` 和入口程序集。清单格式如下：

```json
{
  "schemaVersion": 1,
  "id": "sample-plugin",
  "name": "示例插件",
  "version": "1.0.0",
  "entryAssembly": "Sample.Plugin.dll",
  "enabled": true
}
```

入口程序集中应有且仅有一个实现 `IServerPlugin` 的非抽象类型。插件可注册导航页面、游戏资料表格操作，并通过受限命令启动、停止或查询游戏服状态。后台中切换插件启用状态后，需要重启 `Server.Web` 才会按新状态加载。

## 常见问题

### 启动时提示管理后台已禁用

将 `[AdminWeb]` 下的 `AdminWebEnabled` 设置为 `True`，并确认 `AdminUserName` 和 `[System] MasterPassword` 不为空。

### 启动时提示端口冲突

为 `AdminWebPort` 选择未被游戏端口、在线人数端口、Web 命令、购买或 IPN 服务占用的端口。

### 看不到运行时数据

运行时数据来自当前游戏服内存，需先从概览页启动游戏服。游戏服停止时仍可管理 `System.db` 游戏资料，但没有可供浏览的运行时集合。

### 地图存在但没有贴图

检查 `MapPath`、`ClientPath` 以及客户端资源库是否完整。地图区域仍可读取，但缺失的资源库会使对应贴图无法解码。

### 修改配置后没有立即生效

配置页会标明生效方式。部分配置需要重启游戏服，管理监听地址、端口和登录凭据等配置需要重启 `Server.Web`。
