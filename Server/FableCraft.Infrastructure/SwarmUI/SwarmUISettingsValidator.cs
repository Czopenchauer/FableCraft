using Microsoft.Extensions.Options;

namespace FableCraft.Infrastructure.SwarmUI;

internal sealed class SwarmUISettingsValidator : IValidateOptions<SwarmUISettings>
{
    public ValidateOptionsResult Validate(string? name, SwarmUISettings options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            errors.Add("ImageGeneration:SwarmUI:BaseUrl is required when Provider=SwarmUI.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            errors.Add("ImageGeneration:SwarmUI:Model is required when Provider=SwarmUI.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
