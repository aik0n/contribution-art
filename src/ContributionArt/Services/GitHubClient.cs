using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContributionArt.Services
{
    /// <summary>Thin wrapper over the few GitHub REST/GraphQL calls the app needs.</summary>
    public sealed class GitHubClient(HttpClient http) : IGitHubClient
    {
        private const string BearerScheme = "Bearer";
        private const int MaxErrorSnippetLength = 300;

        /// <inheritdoc />
        public async Task<GitHubUser> GetUserAsync(string token, CancellationToken ct = default)
        {
            using var request = CreateRequest(HttpMethod.Get, "user", token);

            var json = await SendForJsonAsync(request, ct);

            var name = json.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
            return new GitHubUser(json.GetProperty("login").GetString()!, json.GetProperty("id").GetInt64(), name);
        }

        /// <inheritdoc />
        public async Task<int> GetMaxDailyContributionsAsync(string token, string login, CancellationToken ct = default)
        {
            const string query =
                "query($login: String!) { user(login: $login) { contributionsCollection { " +
                "contributionCalendar { weeks { contributionDays { contributionCount } } } } } }";

            using var request = CreateRequest(HttpMethod.Post, "graphql", token);
            request.Content = JsonContent.Create(new { query, variables = new { login } });

            var json = await SendForJsonAsync(request, ct);
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

        /// <inheritdoc />
        public async Task<bool> IsRepositoryEmptyAsync(string token, string owner, string repo, CancellationToken ct = default)
        {
            using var request = CreateRequest(HttpMethod.Get,
                $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/commits?per_page=1", token);

            using var response = await http.SendAsync(request, ct);
            return response.StatusCode switch
            {
                HttpStatusCode.Conflict => true, // GitHub answers 409 "Git Repository is empty."
                HttpStatusCode.OK => false,
                HttpStatusCode.NotFound => throw new InvalidOperationException(
                    $"Repository {owner}/{repo} was not found, or the token has no access to it. Create it on GitHub first (empty, no README)."),
                _ => throw await CreateApiErrorAsync(response, ct),
            };
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string uri, string token)
        {
            var request = new HttpRequestMessage(method, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, token);
            return request;
        }

        private async Task<JsonElement> SendForJsonAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw await CreateApiErrorAsync(response, ct);
            }

            return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        }

        private static async Task<InvalidOperationException> CreateApiErrorAsync(HttpResponseMessage response, CancellationToken ct)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var snippet = body.Length > MaxErrorSnippetLength ? body[..MaxErrorSnippetLength] + "…" : body;
            return new InvalidOperationException($"GitHub API returned {(int)response.StatusCode}: {snippet}");
        }
    }
}
