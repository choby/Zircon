using GameData.AiTranslation;

try
{
    CommandArguments options = CommandArguments.Parse(args);
    TranslationService service = new(options);

    switch (options.Command)
    {
        case CommandName.Export:
            ExportResult export = service.Export();
            Console.WriteLine($"已导出 {export.EntryCount:N0} 条可汉化文本。");
            Console.WriteLine($"汉化文件: {export.TranslationFile}");
            Console.WriteLine($"AI 指令: {export.InstructionsFile}");
            break;
        case CommandName.TranslateRemote:
            RemoteTranslationResult remote = await new CodexRemoteTranslationService(options).TranslateAsync();
            Console.WriteLine($"远程汉化完成：写入 {remote.TranslatedCount:N0} 条，跳过 {remote.SkippedCount:N0} 条无需汉化文本。");
            Console.WriteLine($"汉化前备份: {remote.BackupFile}");
            break;
        case CommandName.Validate:
            ImportResult validation = service.Import(save: false);
            Console.WriteLine($"校验通过，{validation.ChangedCount:N0} 条汉化可安全回写。数据库未修改。");
            break;
        case CommandName.Import:
            ImportResult import = service.Import(save: true);
            Console.WriteLine($"已回写 {import.ChangedCount:N0} 条汉化。");
            if (import.BackupDirectory is not null)
                Console.WriteLine($"原始数据库备份: {import.BackupDirectory}");
            break;
        default:
            throw new InvalidOperationException($"不支持的命令: {options.Command}");
    }
}
catch (CommandLineException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(CommandArguments.Usage);
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"操作失败: {exception.Message}");
    return 1;
}

return 0;
