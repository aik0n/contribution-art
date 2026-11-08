using Microsoft.Extensions.Options;

namespace ContributionArt.Services
{
    /// <summary>Stops the application at startup when <c>presets.json</c> breaks a preset rule.</summary>
    public sealed class PresetOptionsValidator : IValidateOptions<PresetOptions>
    {
        public ValidateOptionsResult Validate(string? name, PresetOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.Predefined.Count == 0)
            {
                return ValidateOptionsResult.Fail($"{PresetOptions.PredefinedFileName} must define at least one preset.");
            }

            var errors = PresetValidator.Validate(options.Predefined);
            if (errors.Count > 0)
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }
}
