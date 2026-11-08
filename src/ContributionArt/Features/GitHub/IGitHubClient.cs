namespace ContributionArt.Services
{
    /// <summary>
    /// The GitHub operations the app needs. The token is per user, so it is passed on every call
    /// and an implementation must not keep it. Failures must surface as
    /// <see cref="InvalidOperationException"/> with a message fit to show to the user.
    /// </summary>
    public interface IGitHubClient
    {
        /// <summary>Gets the account the token belongs to.</summary>
        Task<GitHubUser> GetUserAsync(string token, CancellationToken ct = default);

        /// <summary>Highest number of contributions on a single day in the last year (used to scale colour levels).</summary>
        Task<int> GetMaxDailyContributionsAsync(string token, string login, CancellationToken ct = default);

        /// <summary>
        /// True only if the repository exists and has no commits yet. A missing or inaccessible
        /// repository is a failure, not a false result.
        /// </summary>
        Task<bool> IsRepositoryEmptyAsync(string token, string owner, string repo, CancellationToken ct = default);
    }
}
