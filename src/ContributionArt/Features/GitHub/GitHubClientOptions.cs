using System.ComponentModel.DataAnnotations;

namespace ContributionArt.Features.GitHub
{
    /// <summary>Connection settings for the GitHub API, bound from the <c>GitHub</c> configuration section.</summary>
    public sealed class GitHubClientOptions
    {
        public const string GitHub = nameof(GitHub);

        /// <summary>Absolute API root. Must end with a slash, because requests use relative paths.</summary>
        [Required]
        public string BaseAddress { get; set; } = "";

        [Required]
        public string UserAgent { get; set; } = "";

        /// <summary>Value of the <c>X-GitHub-Api-Version</c> header.</summary>
        [Required]
        public string ApiVersion { get; set; } = "";
    }
}
