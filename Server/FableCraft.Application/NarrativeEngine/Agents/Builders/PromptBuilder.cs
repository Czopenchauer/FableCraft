namespace FableCraft.Application.NarrativeEngine.Agents.Builders;

internal static class PromptBuilder
{
    /// <summary>
    ///     Replaces placeholders in a prompt with provided values
    /// </summary>
    public static string ReplacePlaceholders(string prompt, params (string placeholder, string value)[] replacements)
    {
        foreach (var (placeholder, value) in replacements)
        {
            prompt = prompt.Replace(placeholder, value);
        }

        return prompt;
    }
}