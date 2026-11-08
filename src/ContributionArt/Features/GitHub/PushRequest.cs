using ContributionArt.Features.Shared;

namespace ContributionArt.Features.GitHub
{
    public sealed record PushRequest(
        string Owner,
        string Repository,
        string Token,
        string AuthorName,
        string AuthorEmail,
        DateOnly ReadmeDate,
        IReadOnlyList<CommitPlanItem> Plan);
}
