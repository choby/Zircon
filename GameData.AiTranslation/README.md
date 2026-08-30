# GameData.AiTranslation

面向 AI 的 Zircon 游戏数据库汉化工具。它只导出经过审核的展示文本，并通过 `对象类型 + Index + 属性名` 将译文原位写回现有 MirDB 对象，不创建、删除或重新编号数据。

## 工作流程

1. 停止 Server 和 Server.Web，确保没有进程同时写入数据库。
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
