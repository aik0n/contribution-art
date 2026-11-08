using Microsoft.Extensions.Options;

namespace ContributionArt.Services
{
    /// <summary>Registers the GitHub client. This is the one place to swap the implementation.</summary>
    public static class GitHubServiceCollectionExtensions
    {
        private const string AcceptMediaType = "application/vnd.github+json";
        private const string ApiVersionHeader = "X-GitHub-Api-Version";

        private static readonly TimeSpan ConnectionLifetime = TimeSpan.FromMinutes(2);

        public static IServiceCollection AddGitHubClient(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<GitHubClientOptions>()
                .Bind(configuration.GetSection(GitHubClientOptions.GitHub))
                .ValidateDataAnnotations()
                .Validate(HasValidBaseAddress, $"{nameof(GitHubClientOptions.BaseAddress)} must be an absolute http(s) URL ending with a slash.")
                .ValidateOnStart();

            // A Blazor component keeps its injected client for the whole circuit, which outlives the
            // factory's handler rotation. Recycling connections in the handler itself keeps DNS fresh.
            services.AddHttpClient<IGitHubClient, GitHubClient>(ConfigureClient)
                .UseSocketsHttpHandler(ConfigureHandler)
                .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

            return services;
        }

        private static void ConfigureClient(IServiceProvider services, HttpClient client)
        {
            var options = services.GetRequiredService<IOptions<GitHubClientOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseAddress);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd(AcceptMediaType);
            client.DefaultRequestHeaders.Add(ApiVersionHeader, options.ApiVersion);
        }

        private static void ConfigureHandler(SocketsHttpHandler handler, IServiceProvider services)
        {
            handler.PooledConnectionLifetime = ConnectionLifetime;
        }

        private static bool HasValidBaseAddress(GitHubClientOptions options)
        {
            return Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                && options.BaseAddress.EndsWith('/');
        }
    }
}
