# Server.Web 游戏数据管理一对一移植计划

## 1. 背景与决定

`Server.Web` 当前通过 `DataExplorer.razor`、`SystemCatalogService` 和 `AdminEntityModels` 提供按模型反射生成的通用数据工作台。该实现只覆盖了通用 CRUD，无法可靠表达 `Server/Views` 中每个管理页面已有的筛选、关联、主从表、默认值、插入、导入导出和校验规则；同时它在 Web 项目中重新实现了数据库会话与数据操作，违背了复用原 `Server` 数据读写组件的原则。

因此作出以下决定：

1. 移除 `DataExplorer` 及其专用通用 CRUD 实现，不继续扩展反射式数据管理框架。
2. 以 `Server/Views` 中的游戏数据管理窗口为基准，一个 View 对应一个 Web 管理入口和一个明确的功能验收项。
3. 除 UI 外，数据会话、保存、导入导出、引用解析、默认数据、插入和业务校验尽量从原 `Server` 实现中提取并复用；不得在 Razor 页面或 Web 专用服务中复制一份规则。
4. Web 数据表格统一使用与 DevExpress Grid 定位相近的 Telerik Blazor Grid 组件，尽量还原 Windows 端的数据布局、操作方式和中文文案，不以简化的 HTML 表格代替。
5. 本计划不移植非游戏数据或桌面/运维能力，例如服务器启停、`Server.ini` 管理、地图编辑器、插件管理、日志窗口和桌面外壳行为。它们继续按各自计划维护。

## 2. 范围边界

### 2.1 纳入范围

- `System.db` 中的游戏配置数据管理。
- `Users.db` 和服务器运行时集合中的账号、角色、交易、邮件等游戏数据管理。
- 每个原 View 的列表字段、编辑字段、只读字段、查找源、筛选、排序、主从关系、增删改、保存、JSON 导入导出和专用动作。
- Web 所需的认证、授权、并发冲突提示、审计和错误反馈。

### 2.2 不纳入范围

| 原入口或能力 | 处理方式 |
|---|---|
| `SMain` 服务器启动、停止及运行指标 | 不属于游戏数据移植，保留现有独立实现 |
| `ConfigView`、`DatabaseEncryptionForm`、`SyncForm` | 不属于本计划，保留配置/运维入口 |
| `SystemLogView`、`ChatLogView` | 不属于数据管理，保留日志入口 |
| `MapViewer` | 不移植；地图编辑器继续独立维护 |
| `DiagnosticView`、`OrphanDiagnosticView` | 不并入数据页面，继续作为独立诊断工具 |
| `PluginCore`、`PluginStandalone` 管理 | 不属于本计划，继续使用插件入口 |
| WinForms 窗口布局、皮肤、MDI、托盘、最大化状态 | Web 不复刻桌面外壳行为 |

`MapRegionView` 的表格数据管理仍在范围内，但“打开地图”和可视化区域绘制不在范围内。`NPCListView` 展示的是运行中的游戏 NPC 集合，属于游戏数据只读管理，仍需一对一移植。

## 3. 目标架构与复用原则

`Server` 是 `net10.0-windows` WinForms 项目，不能作为 Web 的跨平台业务依赖。复用应通过提取无 UI 代码完成，而不是让 `Server.Web` 引用 WinForms，也不是复制代码。

建议新增一个不依赖 WinForms、DevExpress、Blazor 或 HTTP 的共享项目，例如 `Server.Management`：

```text
Server/Views/<Name>View             Server.Web/Components/Data/<Name>.razor
              \                       /
               \                     /
                Server.Management/<Name>Manager
                         |
        MirDB Session / System.db / Users.db / DBObject converters
```

共享层按以下边界组织：

- `IServerDataSession`：统一持有和协调原有 `MirDB.Session`，禁止各 Web 页面创建自己的 Session。
- `SystemDataContext` 与 `UserDataContext`：分别暴露系统库和用户库集合，继续使用原模型、索引、引用和保存语义。
- 数据会话所有者还需承接 `SystemCatalogService` 当前承担的 `SystemDataBootstrapper.Apply`、数据库重载及“保存并取得 System.db 路径”能力，供启动初始化、数据库加密和本地/远程同步继续使用。
- `JsonDataImporter`、`JsonDataExporter`、`ImportReferenceResolver`：从 `Server/Helpers` 提取文件对话框和消息框之外的纯逻辑，两个 UI 共用。
- `<Name>Manager`：只承载原 `<Name>View` 中确有的专用筛选、默认值、校验、插入和关联操作。简单 View 可直接使用共享仓储，不为形式统一创建空壳服务。
- UI 适配层：WinForms 只负责控件绑定和文件选择；Web 只负责路由、Telerik Blazor Grid、表单、上传下载和交互状态。

强制约束：

- Razor 组件不得直接调用 `Session.Save`、通过反射写属性或自行维护 DBObject 引用。
- Web API/服务不得重新实现通用的 DBObject 序列化、延迟引用解析、级联删除或插入重排。
- 原 View 中的业务规则应先提取到共享层，再由原 View 和新 Web 页面共同调用；在完成双端回归前不删除原逻辑入口。
- 数据修改必须经过服务端校验；浏览器提交的索引、枚举、引用和只读字段均不可信。
- 同一数据库只允许一个受控写入通道。运行期写入必须沿用游戏线程/会话的协调方式，不得并行打开第二个可写 Session。

## 4. 一对一移植清单

状态统一使用：`待提取`、`共享层完成`、`Web 完成`、`已验证`。只有与原 View 逐项对照通过后才能标记为 `已验证`。

### 4.1 基础、成长与伙伴

| 原 View | Web 入口 | 必须保持的专用行为 | 初始状态 |
|---|---|---|---|
| `BaseStatView` | `/data/base-stats` | 基础属性列表、保存、JSON 导入导出 | 待提取 |
| `MagicInfoView` | `/data/magics` | 技能数据字段与原表格编辑语义 | 待提取 |
| `FameInfoView` | `/data/fame` | 声望、属性和奖励主从数据 | 待提取 |
| `DisciplineInfoView` | `/data/disciplines` | 修炼数据字段与原编辑语义 | 待提取 |
| `CompanionInfoView` | `/data/companions` | 伙伴、对白、等级、技能分页/主从关系及默认货币 | 待提取 |
| `CurrencyInfoView` | `/data/currencies` | 默认货币和图像数据初始化、货币与物品关联 | 待提取 |
| `HelpInfoView` | `/data/help` | 帮助主题及其多级子项关系 | 待提取 |

### 4.2 地图、区域、实例与事件数据

| 原 View | Web 入口 | 必须保持的专用行为 | 初始状态 |
|---|---|---|---|
| `MapInfoView` | `/data/maps` | 地图属性/状态、引用查找及按当前地图过滤的采矿区域 | 待提取 |
| `MapRegionView` | `/data/map-regions` | 区域数据 CRUD、引用查找、指定位置插入；不包含地图绘制 | 待提取 |
| `InstanceInfoView` | `/data/instances` | 实例与地图、入口、区域、物品的关联 | 待提取 |
| `DungeonInfoView` | `/data/dungeons` | 地牢地图非空和全局唯一性、嵌套地图管理 | 待提取 |
| `MovementInfoView` | `/data/movements` | 合法区域类型、隐藏已使用源/目标、传送条件关联 | 待提取 |
| `SafeZoneInfoView` | `/data/safe-zones` | 安全区与区域关联 | 待提取 |
| `RespawnInfoView` | `/data/respawns` | 刷新点与怪物、区域关联 | 待提取 |
| `FishingInfoView` | `/data/fishing` | 钓鱼区域过滤、掉落子项和物品引用 | 待提取 |
| `CastleInfoView` | `/data/castles` | 城堡地图、区域、目标怪物和奖励关联；怪物类型过滤 | 待提取 |
| `EventInfoView` | `/data/events` | 世界、玩家、怪物三类事件及触发器、动作、属性主从编辑 | 待提取 |

### 4.3 NPC、任务与里程碑

| 原 View | Web 入口 | 必须保持的专用行为 | 初始状态 |
|---|---|---|---|
| `NPCInfoView` | `/data/npcs` | NPC、要求、页面和任务关系及专用编辑动作 | 待提取 |
| `NPCPageView` | `/data/npc-pages` | 页面类型、对话/动作及货币、实例、地图、物品引用 | 待提取 |
| `QuestInfoView` | `/data/quests` | 任务条件、奖励和 NPC/地图/怪物/区域引用 | 待提取 |
| `MilestoneInfoView` | `/data/milestones` | 里程碑条件、奖励及跨类型引用 | 待提取 |
| `NPCListView` | `/runtime/npcs` | 运行中 NPC 列表和原只读字段；使用游戏线程快照 | 待提取 |

### 4.4 物品、套装、商店与奖励

| 原 View | Web 入口 | 必须保持的专用行为 | 初始状态 |
|---|---|---|---|
| `ItemInfoView` | `/data/items` | 物品字段、套装/怪物引用及原查找行为 | 待提取 |
| `ItemInfoStatView` | `/data/item-stats` | 物品属性主从数据 | 待提取 |
| `SetInfoView` | `/data/sets` | 套装与套装属性主从数据 | 待提取 |
| `StoreInfoView` | `/data/stores` | 商店与商品子项、物品引用 | 待提取 |
| `WeaponCraftStatInfoView` | `/data/weapon-craft-stats` | 武器制作属性数据 | 待提取 |
| `BundleInfoView` | `/data/bundles` | 礼包和礼包物品主从关系 | 待提取 |
| `LootBoxInfoView` | `/data/loot-boxes` | 宝箱内容、货币和物品引用 | 待提取 |

### 4.5 怪物与掉落

| 原 View | Web 入口 | 必须保持的专用行为 | 初始状态 |
|---|---|---|---|
| `MonsterInfoView` | `/data/monsters` | 怪物、属性、刷新和掉落主从数据，指定位置插入及原专用动作 | 待提取 |
| `MonsterInfoStatView` | `/data/monster-stats` | 怪物属性独立管理及 JSON 行为 | 待提取 |
| `DropInfoView` | `/data/drops` | 怪物掉落、物品引用与原筛选语义 | 待提取 |

### 4.6 用户库与运营游戏数据

| 原 View | Web 入口 | 必须保持的专用行为 | 初始状态 |
|---|---|---|---|
| `AccountView` | `/runtime/accounts` | 账号数据、关联查找、运行期只读/可写边界 | 待提取 |
| `CharacterView` | `/runtime/characters` | 角色数据和账号关联、运行期只读/可写边界 | 待提取 |
| `GameGoldPaymentView` | `/runtime/payments` | 元宝支付记录和账号关联 | 待提取 |
| `GameStoreSaleView` | `/runtime/store-sales` | 商城销售记录及账号、物品关联 | 待提取 |
| `UserConquestStatsView` | `/runtime/conquest-stats` | 用户攻城统计和原只读字段 | 待提取 |
| `UserDropView` | `/runtime/user-drops` | 用户掉落记录及账号、物品关联 | 待提取 |
| `UserMailView` | `/runtime/mails` | 邮件记录、附件/收件人等原字段 | 待提取 |

## 5. 页面功能基线

一对一移植是行为对等，不要求逐像素复制 WinForms。每个 Web 页面至少应逐项核对：

- 原主表、明细表和标签页是否全部存在，列的可见性、只读性和编辑类型是否一致。
- 原查找编辑器的数据源、过滤条件、显示文本和空值语义是否一致。
- 新增、删除、插入位置、级联关系、默认值和保存时机是否一致。
- 原 View 提供 JSON 的，Web 必须使用同一序列化配置和引用解析规则提供导入导出；原 View 未提供的，不因通用组件而擅自增加。
- 原专用按钮、行操作和选择规则是否存在等价 Web 交互。
- 失败时不得留下半次导入、部分引用或未保存的内存状态；错误需要给出可操作提示。
- 大数据表支持服务端分页、排序和筛选，不将整个数据库集合序列化到浏览器。

### 5.1 表格与交互还原标准

每个数据页面的主表和明细表必须使用 Telerik Blazor Grid（`TelerikGrid`）实现，并以对应 `.Designer.cs` 中的 DevExpress Grid 配置及原 View 的事件处理为还原基准：

- **列定义**：还原列顺序、标题、宽度、显示/隐藏、固定、只读、格式化、枚举文本和空值显示；不得依赖模型反射自动生成最终列。
- **编辑方式**：根据原交互选择行内、弹窗或表单编辑，保留下拉查找、复选框、数值、日期时间、多行文本等合适的编辑器，以及新增、保存、取消、删除和插入行的操作顺序。
- **选择方式**：还原单选/多选、行选择/单元格选择、选中行批量导出以及选择变化后刷新明细表的行为；Web 无法完全等价的交互需记录批准差异。
- **数据操作**：启用与原 Grid 对应的排序、筛选、搜索、分页、列宽调整和必要的列固定；大数据集合使用 Telerik Grid 的服务端数据绑定，不在浏览器内全量处理。
- **主从关系**：原 View 的嵌套 Grid、标签页和关联集合应使用 Telerik Grid 的 DetailTemplate、独立明细 Grid 或等价标签页实现，并保持父行选择、子项新增和级联关系语义。
- **工具栏与菜单**：将原 Ribbon、工具栏、右键菜单和专用按钮映射为 Telerik 工具栏、命令列、上下文菜单或页面操作区，操作的可见性、可用条件和确认步骤与原端一致。
- **状态反馈**：保留加载、无数据、校验失败、保存成功、并发冲突和危险操作确认；刷新后应尽量保留当前筛选、排序、页码、展开行和选中项。
- **键盘与双击**：对高频编辑页面尽量保留 Enter、Esc、Delete、双击打开/编辑等桌面端习惯，同时不能阻断浏览器和辅助技术的标准键盘操作。
- **文案**：优先复用 `.Designer.cs`、资源文件和原事件提示中的中文标题、列名、按钮名、确认语和错误说明；仅在 Web 语境确有必要时调整，并在验收表记录差异。

公共封装可以统一 Telerik Grid 的外观、加载、分页、错误状态和审计触发，但不得用一套反射式通用列/编辑器重新制造 DataExplorer。每个页面仍需显式声明列、模板、命令和专用行为。

允许的 UI 差异仅限于 Web 交互自然要求，例如 Ribbon 改为页面工具栏、窗口改为路由、消息框改为确认对话框。视觉尺寸可以响应式调整，但信息层级、操作路径和文案含义应尽量与 Windows 端一致。任何功能或重要交互差异都必须记录到对照表并经明确批准。

## 6. 实施阶段

### 阶段 0：冻结旧实现并建立对照基线

1. 禁止继续向 `DataExplorer` 和 `SystemCatalogService` 增加数据类型或业务特例。
2. 为每个纳入范围的 View 记录主表/明细表、列、编辑器、查找源、按钮、事件处理和导入导出类型。
3. 准备脱敏的 `System.db`、`Users.db` 回归副本及文件哈希，记录原 WinForms 操作结果。

完成条件：39 个 View 均有可执行的行为清单和代表性测试数据。

### 阶段 1：提取共享数据管理层

1. 建立跨平台共享项目和单一会话所有权。
2. 将应用启动时的 `SystemDataBootstrapper.Apply`、`ConfigurationService` 使用的保存路径/重载能力接入新会话所有者，避免移除旧服务后破坏初始化、同步或重新加密。
3. 从 `JsonImporter`、`JsonExporter` 中分离文件对话框/消息框，复用 DBObject 转换器和延迟引用解析。
4. 提取保存、回滚/重载、指定位置插入、默认货币和各 View 专用规则。
5. 让原 WinForms View 改用共享层，并先完成原端回归，证明提取没有改变现有行为。

完成条件：共享层不引用 WinForms、DevExpress、Blazor 或 ASP.NET；WinForms 原功能回归通过。

### 阶段 2：建立 Web 页面骨架与公共 UI 组件

1. 建立独立路由和导航分组，不再使用 `/data/{Category}` 的类型切换工作台。
2. 以 Telerik Blazor Grid 建立公共表格基线，统一分页、服务端绑定、加载/空状态、选择、命令列和错误反馈；查找、枚举编辑、主从面板、确认框、上传和下载可复用 UI 组件。
3. 公共 UI 组件只接收明确的页面 DTO 和命令，不接收 `Type`、`PropertyInfo` 或任意属性字典。
4. 逐 View 从 `.Designer.cs`、`.resx` 和事件处理代码提取列配置、操作条件和中文文案，显式配置到对应 Telerik Grid 页面。
5. 为写操作统一加入授权、CSRF、防重复提交、审计和乐观并发控制。

完成条件：所有计划路由可访问，且空页面已连接到共享层的只读查询。

### 阶段 3：分批移植 System.db 页面

建议按依赖顺序推进：

1. 基础数据：基础属性、技能、修炼、货币、物品、套装、怪物。
2. 关联数据：地图、区域、刷新、掉落、安全区、商店、礼包、宝箱。
3. 复杂聚合：伙伴、实例、地牢、移动、钓鱼、城堡。
4. 复杂规则：NPC、NPC 页面、任务、里程碑、帮助、事件。

每完成一个 View，立即执行一对一验收，不等待整批完成后再补验证。

### 阶段 4：移植 Users.db 与运行时游戏数据页面

1. 通过游戏线程快照读取运行时集合，避免 Web 枚举正在变化的 Binding 集合。
2. 明确每个字段的只读/可写策略；原 View 仅展示的页面默认只读，不因 Web 已有通用修改接口而开放写入。
3. 必须修改运行期对象时，通过受控命令进入游戏线程并在服务端重新校验版本。

完成条件：账号、角色、支付、销售、攻城、掉落、邮件和 NPC 列表分别通过与原 View 的对照验收。

### 阶段 5：切换、删除与清理

仅当全部新页面至少达到 `Web 完成`，且关键写路径达到 `已验证` 后执行：

1. 从导航移除旧 `/data/player`、`/data/maps`、`/data/npc`、`/data/items`、`/data/monsters` 聚合入口。
2. 删除 `Server.Web/Components/Pages/DataExplorer.razor`。
3. 删除仅服务于 DataExplorer 的 `Server.Web/Models/AdminEntityModels.cs`。
4. 删除 `SystemCatalogService` 中的反射式通用 CRUD。删除前必须完成以下调用迁移：启动初始化和 Session 所有权迁至共享数据会话；`ConfigurationService` 的保存路径与重载依赖迁至共享数据会话；地图编辑器使用的区域列表、点位读取和保存 API 迁至 `MapRegionManager`。
5. 从 `Program.cs` 的依赖注入和启动解析、样式、测试和文档中清理旧类型与旧路由。
6. 将旧路由设置为明确的 404 或迁移提示，避免静默进入错误页面。

完成条件：全仓库不存在 `DataExplorer`、`AdminEntityType`、`AdminEntityField`、`AdminEntityRow` 或旧反射 CRUD 的运行时引用；构建和浏览器回归通过。

## 7. 验收与测试策略

每个 View 至少执行以下验证：

1. **结构对照**：对照 `.Designer.cs` 和事件处理代码，检查列、主从关系、按钮和只读规则。
2. **交互与文案对照**：比较 DevExpress Grid 与 Telerik Grid 的列顺序、标题、编辑器、选择、排序、筛选、分页、主从展开、工具栏/菜单、快捷操作和中文提示；记录所有批准差异。
3. **读取对照**：使用同一数据库副本，比较 WinForms 与 Web 的记录数、关键字段、查找显示和筛选结果。
4. **写入对照**：分别验证新增、修改、删除、插入、关联和解除关联；重启进程后再次读取。
5. **JSON 对照**：导出结果能被原实现读取；导入同一文件后对象、索引和引用结果一致。
6. **失败原子性**：无效枚举、悬空引用、重复唯一值和损坏 JSON 不改变数据库哈希或内存集合。
7. **并发与运行期**：两个浏览器编辑同一记录时，后提交者收到冲突；运行中的游戏数据通过快照/命令处理。
8. **安全审计**：未授权请求不可读写；每次写操作记录操作者、对象、动作、结果和时间。

全局退出条件：

- 39 个纳入范围的 View 均为 `已验证`，或存在书面批准的差异。
- `System.db` 与 `Users.db` 的备份、保存、加密和重载流程未被破坏。
- 新页面不包含反射式任意字段写入通道。
- 在真实数据规模下完成分页、筛选、连续编辑和批量导入回归，无未处理浏览器错误。
- 旧 DataExplorer 代码、路由、服务注册和文档声明已全部清理。

## 8. 主要风险与控制

| 风险 | 控制措施 |
|---|---|
| 把“复用”做成复制原 View 代码 | 先提取共享业务代码，再接两个 UI；代码评审拒绝重复规则 |
| Web 与游戏进程同时写同一数据库 | 单一会话所有权；运行期命令进入游戏线程；禁止第二写 Session |
| 通用表格掩盖 View 专用行为 | 以 View 为验收单位，公共组件只复用显示交互，不推导业务规则 |
| DBObject 引用、索引插入或级联关系损坏 | 复用原 Session 和转换器；使用数据库副本做哈希与重启回归 |
| 原 View 本身存在隐式控件规则 | 同时审查 `.cs`、`.Designer.cs`、模型属性和真实操作，不只读取数据源绑定代码 |
| 一次性替换范围过大 | 按依赖分批上线；旧入口保留到对应新页面通过验收 |

## 9. 交付物

- 可被 `Server` 和 `Server.Web` 共同引用的游戏数据管理共享层。
- 39 个与原 View 一一对应的 Web 管理入口及导航。
- 每个 View 的行为对照清单、自动化测试和手工浏览器验收记录。
- 更新后的 `docs/server-web-parity.md`，准确反映每个页面的迁移状态和批准差异。
- 完成切换后删除 DataExplorer 及其专用模型、服务和旧路由。

## 10. 本次执行结果

- 已删除 `DataExplorer.razor`、`AdminEntityModels.cs` 和 `SystemCatalogService.cs`，并迁移启动初始化、配置保存/重载和地图区域 API 的依赖。
- 已建立单一 `GameDataSessionService`，继续使用 MirDB `Session`、`SystemDataBootstrapper`、`DBObjectArrayConverter` 和 `ImportReferenceResolver` 的数据库、保存、导入导出、引用与索引语义。
- 已为 31 个 `System.db` View 和 8 个用户库/运行时 View 建立独立 Web 路由与导航入口；事件页保留世界、玩家、怪物三个数据表。
- 主表和明细表统一使用 Telerik Blazor Grid，提供行内增删改、删除确认、分页、排序、筛选、搜索、列宽调整、列重排、列菜单、选择、JSON 导入导出、插入行、引用/枚举编辑以及主从子项管理。
- 已迁移原 View 中已识别的地图唯一性、区域类型、货币物品类型等服务端校验；其余专用行为继续按功能对照矩阵使用真实数据逐项验收。
- `Server.Web` 构建为 0 警告、0 错误；代表页面浏览器回归覆盖新增后取消、搜索、事件页切换、主从关系和运行时读取，浏览器控制台及服务端均无异常。
- 正式环境须配置 Telerik UI for Blazor 商业许可证；未配置时 Telerik 会显示试用提示和水印。
