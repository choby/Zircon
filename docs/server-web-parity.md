# Server.Web 功能对照矩阵

本文件是 WinForms 管理端停用前的强制验收清单。状态使用 `待迁移`、`已实现`、`已验证`、`批准差异`。

> 游戏数据管理已经从 `DataExplorer` 切换为与 `Server/Views` 一一对应的页面，并统一使用 Telerik Blazor Grid。详细范围、架构、批次和验收要求见 [Server.Web 游戏数据管理一对一移植计划](server-web-data-management-migration.md)。以下数据项已完成代码、构建和代表页面浏览器检查，专用行为仍按下方清单逐项验证。

| 分类 | 旧入口 | 关键行为 | 新入口 | 状态 |
|---|---|---|---|---|
| 运行 | SMain | 启停、连接/对象/循环/流量/延迟/邮件指标 | `/` | 已验证 |
| 日志 | SystemLogView | 实时系统日志、清空显示 | `/logs/system` | 已实现 |
| 日志 | ChatLogView | 实时聊天日志、清空显示 | `/logs/chat` | 已实现 |
| 配置 | ConfigView | 全部 Server.ini 字段、保存/重载、版本检查、同步、加密 | `/configuration` | 已实现 |
| 玩家 | BaseStat / Magic / Fame / Discipline | 按原 View 一对一移植 | 独立页面，见移植计划 | 已实现 |
| 玩家 | Companion / Currency / Help | 按原 View 一对一移植 | 独立页面，见移植计划 | 已实现 |
| 地图 | MapInfo / Instance / Dungeon | 按原 View 一对一移植 | 独立页面，见移植计划 | 已实现 |
| 地图 | MapRegion / Movement / SafeZone | 按原 View 一对一移植；地图编辑器除外 | 独立页面、`/map-editor` | 已实现 |
| 地图 | Fishing / Castle / Event | 按原 View 一对一移植 | 独立页面，见移植计划 | 已实现 |
| NPC | NPCInfo / NPCPage / Quest / Milestone | 按原 View 一对一移植 | 独立页面，见移植计划 | 已实现 |
| 物品 | ItemInfo / ItemInfoStat / Set / Store | 按原 View 一对一移植 | 独立页面，见移植计划 | 已实现 |
| 物品 | WeaponCraftStat / Bundle / LootBox | 按原 View 一对一移植 | 独立页面，见移植计划 | 已实现 |
| 怪物 | MonsterInfo / MonsterInfoStat / Drop / Respawn | 按原 View 一对一移植 | 独立页面，见移植计划 | 已实现 |
| 管理 | Account / Character / UserDrop | 按原 View 一对一移植并复用运行期读取协调 | 独立页面，见移植计划 | 已实现 |
| 管理 | Payment / StoreSale / ConquestStats / Mail / NPCList | 按原 View 一对一移植 | 独立页面，见移植计划 | 已实现 |
| 诊断 | DiagnosticView | 运行指标、日志和状态监控 | `/diagnostics` | 已实现 |
| 诊断 | OrphanDiagnosticView | 扫描、清理、错误反馈 | `/diagnostics` | 已实现 |
| 地图 | MapViewer | 图层、动画、缩放、属性、区域选择、保存/取消 | `/map-editor` | 已实现 |
| 插件 | PluginCore / PluginStandalone | 日志、Blazor 页面、受控命令、行操作、地图打开 | `/plugins` | 已实现 |
| UI 状态 | Server.cache.json | 主题、导航展开、最大化 | 浏览器本地状态 | 批准差异：最大化不适用 |

## 专用行为验收

- [ ] Dungeon 地图非空和全局唯一性验证：已实现，待真实地图数据验证。
- [ ] MapInfo 采矿区域按当前地图过滤：已实现，待真实地图数据验证。
- [ ] Movement 合法区域类型以及隐藏已使用源/目标过滤：已实现，待真实地图数据验证。
- [ ] Event 世界、玩家、怪物的触发器、动作和属性编辑：已实现，待真实事件数据验证。
- [ ] Companion/Event 子记录分页和 JSON：已实现，待真实数据验证。
- [x] JSON 导入前保存；导入、验证或引用解析失败时回滚会话。
- [ ] 指定位置插入同步移动 Users.db 引用：已实现，待生产结构副本验证。
- [ ] 地图画笔、连通填充、半径和阻挡过滤：已实现，待真实 `.map` 验证。
- [ ] 地图保存只更新 MapRegion：已实现，待真实 `.map` 文件哈希验证。

## 已执行的操作级验收

- 游戏端口和用户统计端口连续启动、停止、再次启动两轮；停止后端口均释放。
- 游戏端口被占用时 Web 显示具体绑定异常；释放端口后无需重启管理进程即可恢复启动。
- 临时数据库副本完成 `HelpInfo → HelpPageInfo → HelpItemInfo` 三级创建、修改、进程内重载、删除和聚合级联清理。
- 无效 JSON 导入失败后，`System.db` 的 SHA-256 与导入前一致。
- 上述浏览器操作无控制台错误；其他行只有在同等操作级验证后才能改为“已验证”。
- Telerik Grid 代表页面已完成登录后浏览器回归：基础属性读取、行内新增后取消、搜索、事件三表切换、怪物主从面板、运行时账号读取均通过，浏览器和服务端均无异常。
- 旧 `/data/player` 路由返回 404，确认不再回退到 DataExplorer。
- 当前开发环境未配置 Telerik 商业许可证，会显示试用提示和水印；正式部署前必须提供有效许可证，这不改变页面功能状态。

## 验收规则

- “已实现”表示代码和构建检查通过；发布态浏览器回归通过后可改为“已验证”。
- 游戏数据不再以反射式通用数据工作台作为验收目标；必须以原 View 为单位复用数据读写和业务规则，并对专用行为逐项验收。
- 游戏数据表格统一使用 Telerik Blazor Grid，并对照原 DevExpress Grid 还原列、编辑器、选择、筛选、主从关系、工具栏、交互条件和中文文案；重要差异必须显式批准。
- Web 不迁移窗口最大化、托盘图标等桌面外壳行为，其余差异必须在本表显式批准。
