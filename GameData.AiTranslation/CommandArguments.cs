namespace GameData.AiTranslation;

internal enum CommandName
{
    Export,
    TranslateRemote,
    Validate,
    Import
}

internal sealed class CommandArguments
{
    public const string Usage = """
        用法:
          GameData.AiTranslation export   --database <数据库目录> [--output <输出目录>] [--language zh-CN] [--key <Base64密钥>]
          GameData.AiTranslation translate-remote --database <数据库目录> --input <translations.json> [--model gpt-5.6-sol] [--batch-chars 18000] [--overwrite]
          GameData.AiTranslation validate --database <数据库目录> --input <translations.json> [--key <Base64密钥>] [--allow-database-changes]
          GameData.AiTranslation import   --database <数据库目录> --input <translations.json> [--key <Base64密钥>] [--allow-database-changes]

        数据库目录必须包含 System.db。--allow-database-changes 只跳过整库哈希检查，逐条原文和保护哈希仍会校验。
        """;

    public required CommandName Command { get; init; }
    public required string DatabaseDirectory { get; init; }
    public string? InputFile { get; init; }
    public string OutputDirectory { get; init; } = Path.GetFullPath("ai-translation");
    public string Language { get; init; } = "zh-CN";
    public byte[]? EncryptionKey { get; init; }
    public bool AllowDatabaseChanges { get; init; }
    public string Model { get; init; } = "gpt-5.6-sol";
    public int BatchCharacters { get; init; } = 18_000;
    public bool OverwriteTranslations { get; init; }

    public static CommandArguments Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
            throw new CommandLineException("缺少命令。");

        CommandName command = args[0].ToLowerInvariant() switch
        {
            "export" => CommandName.Export,
            "translate-remote" => CommandName.TranslateRemote,
            "validate" => CommandName.Validate,
            "import" => CommandName.Import,
            _ => throw new CommandLineException($"未知命令: {args[0]}")
        };

        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 1; index < args.Length; index++)
        {
            string name = args[index];
            if (!name.StartsWith("--", StringComparison.Ordinal))
                throw new CommandLineException($"无法识别的参数: {name}");

            if (name is "--allow-database-changes" or "--overwrite")
            {
                values[name] = "true";
                continue;
            }

            if (++index >= args.Length)
                throw new CommandLineException($"参数 {name} 缺少值。");

            values[name] = args[index];
        }

        if (!values.TryGetValue("--database", out string? database) || string.IsNullOrWhiteSpace(database))
            throw new CommandLineException("必须指定 --database。");

        string databaseDirectory = Path.GetFullPath(database);
        if (!File.Exists(Path.Combine(databaseDirectory, "System.db")))
            throw new CommandLineException($"数据库目录中不存在 System.db: {databaseDirectory}");

        string? input = values.GetValueOrDefault("--input");
        if (command is CommandName.Import or CommandName.Validate or CommandName.TranslateRemote && string.IsNullOrWhiteSpace(input))
            throw new CommandLineException($"{command.ToString().ToLowerInvariant()} 命令必须指定 --input。");

        byte[]? key = null;
        if (values.TryGetValue("--key", out string? keyText) && !string.IsNullOrWhiteSpace(keyText))
        {
            try
            {
                key = Convert.FromBase64String(keyText);
            }
            catch (FormatException)
            {
                throw new CommandLineException("--key 必须是有效的 Base64 字符串。");
            }

            if (key.Length != 32)
                throw new CommandLineException("--key 解码后必须为 32 字节。");
        }

        int batchCharacters = 18_000;
        if (values.TryGetValue("--batch-chars", out string? batchText) &&
            (!int.TryParse(batchText, out batchCharacters) || batchCharacters < 1_000))
            throw new CommandLineException("--batch-chars 必须是大于或等于 1000 的整数。");

        return new CommandArguments
        {
            Command = command,
            DatabaseDirectory = databaseDirectory,
            InputFile = input is null ? null : Path.GetFullPath(input),
            OutputDirectory = Path.GetFullPath(values.GetValueOrDefault("--output") ?? "ai-translation"),
            Language = values.GetValueOrDefault("--language") ?? "zh-CN",
            EncryptionKey = key,
            AllowDatabaseChanges = values.ContainsKey("--allow-database-changes"),
            Model = values.GetValueOrDefault("--model") ?? "gpt-5.6-sol",
            BatchCharacters = batchCharacters,
            OverwriteTranslations = values.ContainsKey("--overwrite")
        };
    }
}

internal sealed class CommandLineException(string message) : Exception(message);
