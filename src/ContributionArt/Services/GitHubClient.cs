using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContributionArt.Services
{
    /// <summary>Thin wrapper over the few GitHub REST/GraphQL calls the app needs.</summary>
    public sealed class GitHubClient(HttpClient http)
    {
        public sealed record GitHubUser(string Login, long Id, string? Name)
        {
            /// <summary>GitHub's private no-reply address. Commits with it always count toward the account.</summary>
            public string NoReplyEmail => $"{Id}+{Login}@users.noreply.github.com";
        }

        public async Task<GitHubUser> GetUserAsync(string token, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await http.SendAsync(request, ct);
            await EnsureSuccessAsync(response, ct);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var name = json.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
            return new GitHubUser(json.GetProperty("login").GetString()!, json.GetProperty("id").GetInt64(), name);
        }

        /// <summary>Highest number of contributions on a single day in the last year (used to scale colour levels).</summary>
        public async Task<int> GetMaxDailyContributionsAsync(string token, string login, CancellationToken ct = default)
        {
            const string query =
                "query($login: String!) { user(login: $login) { contributionsCollection { " +
                "contributionCalendar { weeks { contributionDays { contributionCount } } } } } }";

            using var request = new HttpRequestMessage(HttpMethod.Post, "graphql")
            {
                Content = JsonContent.Create(new { query, variables = new { login } }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await http.SendAsync(request, ct);
            await EnsureSuccessAsync(response, ct);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (json.TryGetProperty("errors", out var errors))
            {
                throw new InvalidOperationException($"GitHub GraphQL error: {errors}");
            }

            var weeks = json.GetProperty("data").GetProperty("user")
                .GetProperty("contributionsCollection").GetProperty("contributionCalendar").GetProperty("weeks");

            var max = 0;
            foreach (var week in weeks.EnumerateArray())
            {
                foreach (var day in week.GetProperty("contributionDays").EnumerateArray())
                {
                    max = Math.Max(max, day.GetProperty("contributionCount").GetInt32());
                }
            }

            return max;
        }

        /// <summary>True if the repository exists and has no commits yet.</summary>
        public async Task<bool> IsRepositoryEmptyAsync(string token, string owner, string repo, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/commits?per_page=1");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await http.SendAsync(request, ct);
            return response.StatusCode switch
            {
                HttpStatusCode.Conflict => true, // GitHub answers 409 "Git Repository is empty."
                HttpStatusCode.OK => false,
                HttpStatusCode.NotFound => throw new InvalidOperationException(
                    $"Repository {owner}/{repo} was not found, or the token has no access to it. Create it on GitHub first (empty, no README)."),
                _ => throw new InvalidOperationException(
                    $"GitHub API returned {(int)response.StatusCode}: {await ReadSnippetAsync(response, ct)}"),
            };
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"GitHub API returned {(int)response.StatusCode}: {await ReadSnippetAsync(response, ct)}");
            }
        }

        private static async Task<string> ReadSnippetAsync(HttpResponseMessage response, CancellationToken ct)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return body.Length > 300 ? body[..300] + "…" : body;
        }
    }
}
