using ContributionArt.Features.Presets;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ContributionArt.Tests.Features.Presets
{
    /// <summary>Checks the <c>presets.json</c> file that ships with the application.</summary>
    [UnitTestTrait]
    public class PresetsFileTests
    {
        [Fact]
        public void PresetsFile_AsShipped_PassesValidation()
        {
            var options = ReadShippedOptions();

            var result = new PresetOptionsValidator().Validate(null, options);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public void PresetsFile_AsShipped_ContainsShapesInDisplayOrder()
        {
            var options = ReadShippedOptions();

            var names = new List<string>();
            foreach (var preset in options.Predefined)
            {
                names.Add(preset.Name);
            }

            names.Should().Equal("Heart", "Cat", "Gliders", "Wave", "Gradient");
        }

        private static PresetOptions ReadShippedOptions()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(PresetOptions.PredefinedFileName, optional: false, reloadOnChange: false)
                .Build();

            return configuration.GetSection(PresetOptions.Presets).Get<PresetOptions>()!;
        }
    }
}
