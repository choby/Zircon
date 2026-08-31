# GameData.AiTranslation

面向 AI 的 Zircon 游戏数据库汉化工具。它只导出经过审核的展示文本，并通过 `对象类型 + Index + 属性名` 将译文原位写回现有 MirDB 对象，不创建、删除或重新编号数据。

## AI 远程汉化前置条件

`translate-remote` 不直接调用 OpenAI HTTP API，而是启动本机的 `codex exec`，因此使用前需要完成以下准备：

1. 安装 .NET 10 SDK，并在解决方案根目录确认项目可以正常还原和构建：

   ```bash
   dotnet --version
   dotnet restore GameData.AiTranslation/GameData.AiTranslation.csproj
   dotnet build GameData.AiTranslation/GameData.AiTranslation.csproj
   ```

   `dotnet --version` 应输出 `10.x`。以下示例命令也均假定当前目录为解决方案根目录。
2. 安装 [Codex CLI](https://learn.chatgpt.com/docs/codex/cli)，并确保 `codex` 命令已加入 `PATH`。macOS 和 Linux 可按官方文档执行：

   ```bash
   curl -fsSL https://chatgpt.com/codex/install.sh | sh
   codex --version
   ```

   Windows 的安装方式请参照 Codex CLI 官方文档。

3. 在运行远程汉化的同一用户环境中完成 Codex 登录。可以使用 ChatGPT 账号订阅额度登录：

   ```bash
   codex login
   codex login status
   ```

   也可以使用按 API 用量计费的 OpenAI API Key；以下为 macOS/Linux 示例：

   ```bash
   printenv OPENAI_API_KEY | codex login --with-api-key
   codex login status
   ```

   认证方式、工作区权限和计费规则以 [Codex 认证文档](https://learn.chatgpt.com/docs/auth) 为准。账号或 API 项目必须有权使用命令指定的模型，并具有足够的可用额度。
4. 确保运行环境能够访问 OpenAI 服务。远程汉化会分批提交请求；网络中断、限流、额度不足或模型不可用都会导致当前批次失败。
5. 先执行 `export`，生成结构完整的 `translations.json`。由于所有子命令共用参数解析，`translate-remote` 仍要求 `--database` 指向一个可读取且包含 `System.db` 的目录；远程阶段只检查该文件存在，不会打开、修改或保存数据库，也不需要数据库加密密钥。
6. 确保 `translations.json` 所在目录和系统临时目录可写。程序会在同目录创建 `translations.before-remote-<时间戳>.json`，并在每个成功批次后原子保存进度。
7. 确认允许将每条待翻译记录的 `id`、`type`、`property`、`source`、`context` 和 `rules` 发送给所选远程模型。`translation`、保护哈希和数据库文件不会发送。导出白名单会排除已知敏感或结构性字段，但仍应按实际数据和组织政策检查输入文件。

每个批次都通过 `codex exec --ephemeral --ignore-user-config --sandbox read-only` 运行：Codex 子进程不保存会话、不加载用户 `config.toml`，也不能通过模型生成的命令写入本地文件；外层 .NET 程序仍会按上述规则备份并更新 `translations.json`。认证缓存仍由 Codex CLI 正常读取。`--model` 可以省略，默认使用 `gpt-5.6-sol`；用户配置中的模型或自定义提供商不会覆盖此行为。

每个失败批次会自动重试，连续失败 3 次后命令退出。任务中断后可直接重新执行原命令：记录按 `type + property + source` 分组，译文已经完整填写的分组会被跳过；组内只要仍有空译文，该组就会重新请求模型并统一写入同一译法。只有明确使用 `--overwrite` 才会清空全部已有译文并从头翻译。`--batch-chars` 默认值为 `18000`，最小值为 `1000`，调整批次大小不会清空已保存进度。

## 工作流程

1. 执行完整汉化流程时停止 Server 和 Server.Web，确保导出到回写期间没有进程修改数据库。单独重跑 `translate-remote` 不会访问数据库，无需为该命令停服；但数据库发生变化后，后续 `validate` 或 `import` 可能触发整库哈希校验失败。
2. 导出可汉化文本和 AI 指令：

   ```bash
   dotnet run --project GameData.AiTranslation -- export \
     --database /path/to/database \
     --output /path/to/translation-work
   ```

3. 可将 `AI_INSTRUCTIONS.md` 和 `translations.json` 交给 AI，或直接调用远程 Codex 大模型。远程命令按唯一原文分批翻译、校验 ID 与格式约束，并在每批后断点保存：

   ```bash
   dotnet run --project GameData.AiTranslation -- translate-remote \
     --database /path/to/database \
     --input /path/to/translation-work/translations.json \
     --model gpt-5.6-sol
   ```

   增加 `--overwrite` 可清空已有译文后从头翻译；`--batch-chars` 可调整每批字符预算。无论采用哪种方式，AI 都只能填写 `entries[].translation`。

4. 在不修改数据库的情况下预演：

   ```bash
   dotnet run --project GameData.AiTranslation -- validate \
     --database /path/to/database \
     --input /path/to/translation-work/translations.json
   ```

5. 校验通过后回写：

   ```bash
   dotnet run --project GameData.AiTranslation -- import \
     --database /path/to/database \
     --input /path/to/translation-work/translations.json
   ```

数据库加密时，`export`、`validate` 和 `import` 命令都应增加 `--key <32字节密钥的Base64文本>`。

如果导出后数据库存在确认无关的修改，可增加 `--allow-database-changes` 跳过整库 SHA-256 比较；工具仍会逐条校验原文、对象 Index 和保护哈希。

## 文件格式

`translations.json` 是 UTF-8 JSON。每条记录包含：

- `id`：稳定定位信息，不可修改。
- `type`、`index`、`property`：数据库对象和字段，不可修改。
- `source`：导出时原文，不可修改。
- `translation`：AI 唯一允许修改的字段；空字符串表示跳过。
- `context`：游戏上下文。
- `rules`：该字段的汉化约束。
- `isIdentity`：是否参与 JSON Identity。
- `protectionHash`：受保护字段校验值。

## 安全策略

- 字段采用代码白名单；`MapInfo.FileName`、系统版本、脚本键、NPC 数据分类、事件计时器、Buff 枚举和玩家命令不会导出。
- NPC 消息动作和事件 `PlayerMessage` 会按动作类型导出，其他同名参数不会导出。
- 任务占位符、NPC 变量、颜色代码、按钮 ID、NPC 名称下划线结构会在导入前校验。
- 相同类型、字段和原文不得产生多个译法。
- Identity 校验采用迁移前后差分：保留数据库原有重复组合，但拒绝汉化新增或扩大冲突。
- 货币名称变化会同步更新 NPC 的 `GiveCurrency`、`TakeCurrency` 和 `Currency` 检查参数。
- 回写前自动复制 `System.db`、`Users.db` 和汉化文件到数据库目录下的 `AITranslationBackups/<时间戳>`。
- MirDB 保存后会重新加载数据库并逐条验证译文。

货币掉落物的默认初始化逻辑已改为优先使用 `CurrencyType -> DropItem` 引用，因此 `Gold`、`Fame Point`、`Contribution Point` 等物品名可以安全汉化，不会因英文名称查找而重复创建。
