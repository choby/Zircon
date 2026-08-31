using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GameData.AiTranslation;

internal sealed class CodexRemoteTranslationService(CommandArguments options)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private sealed record TranslationGroup(string Id, string Type, string Property, string Source, string Context, string[] Rules, List<TranslationEntry> Entries);

    public async Task<RemoteTranslationResult> TranslateAsync()
    {
        string inputFile = options.InputFile ?? throw new CommandLineException("必须指定 --input。");
        if (!File.Exists(inputFile)) throw new FileNotFoundException("找不到汉化文件。", inputFile);

        TranslationDocument document = JsonSerializer.Deserialize<TranslationDocument>(await File.ReadAllTextAsync(inputFile), JsonOptions)
            ?? throw new InvalidDataException("汉化文件不是有效的 JSON 文档。");
        if (document.EntryCount != document.Entries.Count)
            throw new InvalidDataException("汉化条目数量与文档元数据不一致。");

        string backupFile = Path.Combine(
            Path.GetDirectoryName(inputFile)!,
            $"translations.before-remote-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.Copy(inputFile, backupFile, overwrite: false);

        if (options.OverwriteTranslations)
            foreach (TranslationEntry entry in document.Entries) entry.Translation = string.Empty;

        List<TranslationGroup> groups = document.Entries
            .GroupBy(entry => (entry.Type, entry.Property, entry.Source))
            .Select(group => new TranslationGroup(
                group.First().Id,
                group.Key.Type,
                group.Key.Property,
                group.Key.Source,
                group.First().Context,
                group.First().Rules,
                group.ToList()))
            .Where(group => group.Entries.Any(entry => string.IsNullOrEmpty(entry.Translation)))
            .ToList();

        List<List<TranslationGroup>> batches = BuildBatches(groups, options.BatchCharacters);
        int translatedCount = 0;
        int skippedCount = 0;
        for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            List<TranslationGroup> batch = batches[batchIndex];
            CodexTranslationResponse response = await TranslateBatchWithRetryAsync(batch, batchIndex + 1, batches.Count);
            Dictionary<string, CodexTranslationItem> results = response.Translations.ToDictionary(item => item.Id, StringComparer.Ordinal);

            foreach (TranslationGroup group in batch)
            {
                if (!results.TryGetValue(group.Id, out CodexTranslationItem? result))
                    throw new InvalidDataException($"远程模型遗漏条目 {group.Id}。");
                if (string.IsNullOrWhiteSpace(result.Translation) || string.Equals(result.Translation, group.Source, StringComparison.Ordinal))
                {
                    skippedCount += group.Entries.Count;
                    continue;
                }

                foreach (TranslationEntry entry in group.Entries)
                {
                    TranslationSafety.ValidateText(entry, result.Translation);
                    entry.Translation = result.Translation;
                    translatedCount++;
                }
            }

            await SaveAtomicallyAsync(inputFile, document);
            Console.WriteLine($"[{batchIndex + 1}/{batches.Count}] 已保存，本批 {batch.Count:N0} 条唯一文本，总进度 {(batchIndex + 1d) / batches.Count:P1}");
        }

        return new RemoteTranslationResult(translatedCount, skippedCount, backupFile);
    }

    private static List<List<TranslationGroup>> BuildBatches(List<TranslationGroup> groups, int characterLimit)
    {
        List<List<TranslationGroup>> batches = [];
        List<TranslationGroup> current = [];
        int characters = 0;
        foreach (TranslationGroup group in groups)
        {
            int size = group.Source.Length + group.Context.Length + group.Rules.Sum(rule => rule.Length) + 100;
            if (current.Count > 0 && (characters + size > characterLimit || current.Count >= 300))
            {
                batches.Add(current);
                current = [];
                characters = 0;
            }
            current.Add(group);
            characters += size;
        }
        if (current.Count > 0) batches.Add(current);
        return batches;
    }

    private async Task<CodexTranslationResponse> TranslateBatchWithRetryAsync(List<TranslationGroup> batch, int number, int total)
    {
        Exception? lastError = null;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                CodexTranslationResponse response = await TranslateBatchAsync(batch, number, total);
                Dictionary<string, CodexTranslationItem> results = response.Translations.ToDictionary(item => item.Id, StringComparer.Ordinal);
                foreach (TranslationGroup group in batch)
                {
                    CodexTranslationItem result = results[group.Id];
                    if (string.IsNullOrWhiteSpace(result.Translation) || string.Equals(result.Translation, group.Source, StringComparison.Ordinal))
                        continue;
                    foreach (TranslationEntry entry in group.Entries)
                        TranslationSafety.ValidateText(entry, result.Translation);
                }
                return response;
            }
            catch (Exception exception)
            {
                lastError = exception;
                Console.Error.WriteLine($"批次 {number}/{total} 第 {attempt} 次失败: {exception.Message}");
            }
        }
        throw new InvalidOperationException($"批次 {number}/{total} 连续失败。", lastError);
    }

    private async Task<CodexTranslationResponse> TranslateBatchAsync(List<TranslationGroup> batch, int number, int total)
    {
        string tempOutput = Path.Combine(Path.GetTempPath(), $"zircon-codex-translation-{Environment.ProcessId}-{number}.json");
        string schemaFile = Path.Combine(AppContext.BaseDirectory, "codex-translation-result.schema.json");
        if (!File.Exists(schemaFile)) throw new FileNotFoundException("找不到远程模型输出 schema。", schemaFile);

        ProcessStartInfo start = new("codex")
        {
            WorkingDirectory = Path.GetTempPath(),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in new[]
        {
            "exec", "--ephemeral", "--ignore-user-config", "--sandbox", "read-only",
            "--skip-git-repo-check", "--model", options.Model,
            "-c", "model_reasoning_effort=\"low\"",
            "--output-schema", schemaFile, "--output-last-message", tempOutput, "-"
        }) start.ArgumentList.Add(argument);

        var records = batch.Select(group => new
        {
            id = group.Id,
            type = group.Type,
            property = group.Property,
            source = group.Source,
            context = group.Context,
            rules = group.Rules
        });
        string payload = JsonSerializer.Serialize(new { records }, JsonOptions);
        string prompt = $$"""
            你是精通官方《传奇3》术语的资深游戏本地化专家。将 records 全部汉化为简体中文。
            只输出符合给定 JSON Schema 的对象；每个输入 id 必须原样且恰好返回一次，不得添加 id。
            translation 只能是译文字符串；确实无需汉化的缩写、代码或纯数字返回原文。
            严格遵守每条 rules，保持占位符、变量键、颜色码、按钮 ID、逗号分段数、下划线数和格式结构。
            相同术语保持一致。采用传奇3经典术语，例如 Sabuk Wall=沙巴克城、Bichon=比奇、Mongchon=盟重、Wooma=沃玛、Zuma=祖玛、Prajna=潘夜、Warrior=战士、Wizard=法师、Taoist=道士。
            不解释，不使用 Markdown。当前批次 {{number}}/{{total}}。

            {{payload}}
            """;

        using Process process = Process.Start(start) ?? throw new InvalidOperationException("无法启动 codex CLI。");
        await process.StandardInput.WriteAsync(prompt);
        process.StandardInput.Close();
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        string errorText = await stderr;
        _ = await stdout;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"codex CLI 退出码 {process.ExitCode}: {LastLine(errorText)}");

        try
        {
            CodexTranslationResponse response = JsonSerializer.Deserialize<CodexTranslationResponse>(await File.ReadAllTextAsync(tempOutput), JsonOptions)
                ?? throw new InvalidDataException("远程模型返回空 JSON。");
            string[] expected = batch.Select(group => group.Id).Order(StringComparer.Ordinal).ToArray();
            string[] actual = response.Translations.Select(item => item.Id).Order(StringComparer.Ordinal).ToArray();
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidDataException("远程模型返回的 id 集合与批次不一致。");
            return response;
        }
        finally
        {
            if (File.Exists(tempOutput)) File.Delete(tempOutput);
        }
    }

    private static async Task SaveAtomicallyAsync(string path, TranslationDocument document)
    {
        string temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(document, JsonOptions), new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    private static string LastLine(string value) =>
        value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? "未知错误";
}
