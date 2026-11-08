using Microsoft.Extensions.Options;

namespace ContributionArt.Features.Presets
{
    /// <summary>Registers the preset store. This is the one place to swap the implementation.</summary>
    public static class PresetServiceCollectionExtensions
    {
        public static IServiceCollection AddPresets(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddSingleton<IValidateOptions<PresetOptions>, PresetOptionsValidator>();
            services.AddOptions<PresetOptions>()
                .Bind(configuration.GetSection(PresetOptions.Presets))
                .ValidateOnStart();

            services.AddSingleton<IPresetStore, PresetStore>();

            return services;
        }
    }
}
