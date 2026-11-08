using Bogus;
using ContributionArt.Features.Canvas;
using ContributionArt.Features.Presets;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ContributionArt.Tests.Features.Presets
{
    [UnitTestTrait]
    public class PresetServiceCollectionExtensionsTests
    {
        private const string Digits = "01234";

        private static readonly Faker Faker = new("en") { Random = new Randomizer(42) };

        [Fact]
        public void AddPresets_WithValidConfiguration_PassesStartupValidation()
        {
            using var provider = BuildProvider(CreateSettings(CreateName(), CreateRow()));
            var validator = provider.GetRequiredService<IStartupValidator>();

            var act = validator.Validate;

            act.Should().NotThrow();
        }

        [Fact]
        public void AddPresets_WithValidConfiguration_ResolvesPresetStoreWithPresets()
        {
            var name = CreateName();
            using var provider = BuildProvider(CreateSettings(name, CreateRow()));

            var result = provider.GetRequiredService<IPresetStore>();

            result.Should().BeOfType<PresetStore>();
            result.Presets.Should().ContainSingle().Which.Name.Should().Be(name);
        }

        [Fact]
        public void AddPresets_WithUserFilePath_BindsOptions()
        {
            var userFilePath = Faker.System.FilePath();
            var settings = CreateSettings(CreateName(), CreateRow());
            settings[$"{PresetOptions.Presets}:{nameof(PresetOptions.UserFilePath)}"] = userFilePath;
            using var provider = BuildProvider(settings);

            var result = provider.GetRequiredService<IOptions<PresetOptions>>().Value;

            result.UserFilePath.Should().Be(userFilePath);
        }

        [Fact]
        public void AddPresets_WithNoPredefinedPresets_FailsStartupValidation()
        {
            using var provider = BuildProvider([]);
            var validator = provider.GetRequiredService<IStartupValidator>();

            var act = validator.Validate;

            act.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain(PresetOptions.PredefinedFileName);
        }

        [Fact]
        public void AddPresets_WithInvalidPreset_FailsStartupValidationNamingThePreset()
        {
            var name = CreateName();
            using var provider = BuildProvider(CreateSettings(name, "5" + CreateRow()[1..]));
            var validator = provider.GetRequiredService<IStartupValidator>();

            var act = validator.Validate;

            act.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain(name);
        }

        private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            var services = new ServiceCollection();
            services.AddPresets(configuration);

            return services.BuildServiceProvider();
        }

        private static Dictionary<string, string?> CreateSettings(string name, string row)
        {
            var prefix = $"{PresetOptions.Presets}:{nameof(PresetOptions.Predefined)}:0";
            var settings = new Dictionary<string, string?> { [$"{prefix}:{nameof(PresetDefinition.Name)}"] = name };
            for (var day = 0; day < ContributionGrid.Days; day++)
            {
                settings[$"{prefix}:{nameof(PresetDefinition.Rows)}:{day}"] = row;
            }

            return settings;
        }

        private static string CreateName()
        {
            return Faker.Random.AlphaNumeric(12);
        }

        private static string CreateRow()
        {
            return Faker.Random.String2(9, Digits);
        }
    }
}
