using System.Globalization;
using Bogus;
using ContributionArt.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ContributionArt.Tests.Services
{
    [UnitTestTrait]
    public class GitHubServiceCollectionExtensionsTests
    {
        private const string ApiVersionHeader = "X-GitHub-Api-Version";

        private static readonly Faker Faker = new("en") { Random = new Randomizer(42) };

        public static IEnumerable<object?[]> InvalidSettings()
        {
            yield return [null, ValidUserAgent(), ValidApiVersion()];                                  // missing base address
            yield return ["", ValidUserAgent(), ValidApiVersion()];                                    // empty base address
            yield return ["   ", ValidUserAgent(), ValidApiVersion()];                                 // whitespace base address
            yield return [$"{Faker.Lorem.Word()}/", ValidUserAgent(), ValidApiVersion()];              // relative base address
            yield return [$"ftp://{Faker.Internet.DomainName()}/", ValidUserAgent(), ValidApiVersion()]; // unsupported scheme
            yield return [$"https://{Faker.Internet.DomainName()}/{Faker.Lorem.Word()}", ValidUserAgent(), ValidApiVersion()]; // no trailing slash
            yield return [ValidBaseAddress(), null, ValidApiVersion()];                                // missing user agent
            yield return [ValidBaseAddress(), "", ValidApiVersion()];                                  // empty user agent
            yield return [ValidBaseAddress(), "   ", ValidApiVersion()];                               // whitespace user agent
            yield return [ValidBaseAddress(), ValidUserAgent(), null];                                 // missing API version
            yield return [ValidBaseAddress(), ValidUserAgent(), ""];                                   // empty API version
            yield return [ValidBaseAddress(), ValidUserAgent(), "   "];                                // whitespace API version
            yield return [null, null, ValidApiVersion()];                                              // two invalid
            yield return [$"{Faker.Lorem.Word()}/", ValidUserAgent(), null];                           // two invalid, one by format
            yield return [null, null, null];                                                           // all invalid
        }

        [Fact]
        public void AddGitHubClient_WithValidConfiguration_ResolvesGitHubClient()
        {
            using var provider = BuildProvider(ValidBaseAddress(), ValidUserAgent(), ValidApiVersion());

            var result = provider.GetRequiredService<IGitHubClient>();

            result.Should().BeOfType<GitHubClient>();
        }

        [Fact]
        public void AddGitHubClient_WithValidConfiguration_BindsOptions()
        {
            var baseAddress = ValidBaseAddress();
            var userAgent = ValidUserAgent();
            var apiVersion = ValidApiVersion();
            using var provider = BuildProvider(baseAddress, userAgent, apiVersion);

            var result = provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value;

            result.BaseAddress.Should().Be(baseAddress);
            result.UserAgent.Should().Be(userAgent);
            result.ApiVersion.Should().Be(apiVersion);
        }

        [Fact]
        public void AddGitHubClient_WithValidConfiguration_ConfiguresHttpClientFromOptions()
        {
            var baseAddress = ValidBaseAddress();
            var userAgent = ValidUserAgent();
            var apiVersion = ValidApiVersion();
            using var provider = BuildProvider(baseAddress, userAgent, apiVersion);

            using var result = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IGitHubClient));

            result.BaseAddress.Should().Be(new Uri(baseAddress));
            result.DefaultRequestHeaders.UserAgent.ToString().Should().Be(userAgent);
            result.DefaultRequestHeaders.Accept.ToString().Should().Be("application/vnd.github+json");
            result.DefaultRequestHeaders.GetValues(ApiVersionHeader).Should().Equal(apiVersion);
        }

        [Fact]
        public void AddGitHubClient_WithValidConfiguration_PassesStartupValidation()
        {
            using var provider = BuildProvider(ValidBaseAddress(), ValidUserAgent(), ValidApiVersion());
            var validator = provider.GetRequiredService<IStartupValidator>();

            var act = validator.Validate;

            act.Should().NotThrow();
        }

        [Theory]
        [MemberData(nameof(InvalidSettings))]
        public void AddGitHubClient_WithInvalidConfiguration_FailsStartupValidation(string? baseAddress, string? userAgent, string? apiVersion)
        {
            using var provider = BuildProvider(baseAddress, userAgent, apiVersion);
            var validator = provider.GetRequiredService<IStartupValidator>();

            var act = validator.Validate;

            act.Should().Throw<OptionsValidationException>();
        }

        private static ServiceProvider BuildProvider(string? baseAddress, string? userAgent, string? apiVersion)
        {
            var settings = new Dictionary<string, string?>
            {
                [Key(nameof(GitHubClientOptions.BaseAddress))] = baseAddress,
                [Key(nameof(GitHubClientOptions.UserAgent))] = userAgent,
                [Key(nameof(GitHubClientOptions.ApiVersion))] = apiVersion,
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            var services = new ServiceCollection();
            services.AddGitHubClient(configuration);

            return services.BuildServiceProvider();
        }

        private static string Key(string propertyName)
        {
            return $"{GitHubClientOptions.GitHub}:{propertyName}";
        }

        private static string ValidBaseAddress()
        {
            return $"https://{Faker.Internet.DomainName()}/";
        }

        private static string ValidUserAgent()
        {
            return $"{Faker.Lorem.Word()}/{Faker.System.Semver()}";
        }

        private static string ValidApiVersion()
        {
            return Faker.Date.Past().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
