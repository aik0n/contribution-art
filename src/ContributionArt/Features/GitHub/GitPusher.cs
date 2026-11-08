using ContributionArt.Features.Shared;
using LibGit2Sharp;

namespace ContributionArt.Features.GitHub
{
    /// <summary>
    /// Builds the whole history in a temporary local repository (backdated empty commits),
    /// then pushes it to GitHub in a single push over HTTPS.
    /// </summary>
    public sealed class GitPusher(ILogger<GitPusher> logger)
    {
        public Task<int> PushAsync(PushRequest request, IProgress<string> progress, CancellationToken ct) =>
            Task.Run(() => Push(request, progress, ct), ct);

        private int Push(PushRequest request, IProgress<string> progress, CancellationToken ct)
        {
            var workDir = Path.Combine(Path.GetTempPath(), "contribution-art-" + Guid.NewGuid().ToString("N"));
            try
            {
                Repository.Init(workDir);
                using var repo = new Repository(workDir);

                // 1. README, dated just before the visible graph so it doesn't tint any pixel.
                File.WriteAllText(Path.Combine(workDir, "README.md"), PublishedReadme.Text);
                Commands.Stage(repo, "README.md");
                var readmeSignature = Sign(request, request.ReadmeDate, 0);
                repo.Commit("Add README: this repository is contribution-graph art", readmeSignature, readmeSignature);

                // 2. One empty, backdated commit per unit of intensity.
                var total = request.Plan.Sum(p => p.Commits);
                var done = 0;
                var emptyCommit = new CommitOptions { AllowEmptyCommit = true };

                foreach (var item in request.Plan.OrderBy(p => p.Date))
                {
                    for (var i = 0; i < item.Commits; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var signature = Sign(request, item.Date, i);
                        repo.Commit("contribution-art", signature, signature, emptyCommit);

                        if (++done % 50 == 0 || done == total)
                        {
                            progress.Report($"Created {done} of {total} commits");
                        }
                    }
                }

                if (repo.Head.FriendlyName != "main")
                {
                    repo.Branches.Rename(repo.Head, "main");
                }

                // 3. Single push.
                progress.Report("Pushing to GitHub…");
                var remote = repo.Network.Remotes.Add("origin",
                    $"https://github.com/{request.Owner}/{request.Repository}.git");

                string? rejection = null;
                var pushOptions = new PushOptions
                {
                    CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials
                    {
                        Username = "x-access-token",
                        Password = request.Token,
                    },
                    OnPushStatusError = error => rejection = $"{error.Reference}: {error.Message}",
                    OnPushTransferProgress = (current, totalObjects, _) =>
                    {
                        if (totalObjects > 0 && (current % 200 == 0 || current == totalObjects))
                        {
                            progress.Report($"Uploading objects {current} of {totalObjects}");
                        }

                        return !ct.IsCancellationRequested;
                    },
                };

                repo.Network.Push(remote, "refs/heads/main", pushOptions);

                if (rejection is not null)
                {
                    throw new InvalidOperationException(
                        $"GitHub rejected the push ({rejection}). Make sure the repository is new and empty.");
                }

                logger.LogInformation("Pushed {Count} commits to {Owner}/{Repo}", done + 1, request.Owner, request.Repository);
                return done;
            }
            finally
            {
                TryDelete(workDir);
            }
        }

        private static Signature Sign(PushRequest request, DateOnly date, int index)
        {
            // Noon UTC keeps the commit on the same calendar day for almost every time zone.
            var when = new DateTimeOffset(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero).AddSeconds(index);
            return new Signature(request.AuthorName, request.AuthorEmail, when);
        }

        private void TryDelete(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                // Git object files are read-only, which blocks deletion on Windows.
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(path, recursive: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete temporary repository {Path}", path);
            }
        }
    }
}
