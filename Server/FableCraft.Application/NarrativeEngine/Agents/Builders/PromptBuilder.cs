namespace FableCraft.Application.NarrativeEngine.Agents;

internal static class PromptBuilder
{
    private const string JailbrakeIncludePlaceholder = "{{jailbreak}}";

    public async static Task<string> BuildPromptAsync(string promptFileName)
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            promptFileName
        );

        var promptTemplate = await File.ReadAllTextAsync(promptPath);
        return await ReplacePlaceholders(promptTemplate);
    }

    /// <summary>
    ///     Builds a prompt and replaces placeholders with provided values
    /// </summary>
    public async static Task<string> BuildPromptAsync(string promptFileName, params (string placeholder, string value)[] replacements)
    {
        var prompt = await BuildPromptAsync(promptFileName);
        foreach (var (placeholder, value) in replacements)
        {
            prompt = prompt.Replace(placeholder, value);
        }

        return prompt;
    }

    private async static Task<string> ReplacePlaceholders(string promptTemplate)
    {
        if (!promptTemplate.Contains(JailbrakeIncludePlaceholder))
        {
            return promptTemplate;
        }

        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "Jailbrake.md"
        );

        if (File.Exists(filePath))
        {
            var fileContent = await File.ReadAllTextAsync(filePath);
            return promptTemplate.Replace(JailbrakeIncludePlaceholder, fileContent);
        }

        return promptTemplate;
    }
}