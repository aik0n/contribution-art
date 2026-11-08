using System.Net;
using System.Text.Json;
using Bogus;
using ContributionArt.Features.GitHub;
using ContributionArt.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace ContributionArt.Tests.Features.GitHub
{
    [UnitTestTrait]
    public sealed class GitHubClientTests : IDisposable
    {
        private const int MaxErrorSnippetLength = 300;

        private static readonly Faker Faker = new("en") { Random = new Randomizer(42) };

        private readonly StubHttpMessageHandler _handler = new();
        private readonly HttpClient _httpClient;
        private readonly Uri _baseAddress = new($"https://{Faker.Internet.DomainName()}/");
        private readonly string _token = Faker.Random.AlphaNumeric(40);
        private readonly GitHubClient _sut;

        public GitHubClientTests()
        {
            _httpClient = new HttpClient(_handler) { BaseAddress = _baseAddress };
            _sut = new GitHubClient(_httpClient);
        }

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _handler.Dispose();
        }

        [Fact]
        public async Task GetUserAsync_WithToken_SendsBearerTokenToUserEndpoint()
        {
            _handler.RespondWith(HttpStatusCode.OK, UserJson(Faker.Internet.UserName(), Faker.Random.Long(1), Faker.Name.FullName()));

            await _sut.GetUserAsync(_token, Ct);

            _handler.LastMethod.Should().Be(HttpMethod.Get);
            _handler.LastUri.Should().Be(new Uri(_baseAddress, "user"));
            AssertBearerToken();
        }

        [Fact]
        public async Task GetUserAsync_WithFullProfile_ReturnsParsedUser()
        {
            var login = Faker.Internet.UserName();
            var id = Faker.Random.Long(1);
            var name = Faker.Name.FullName();
            _handler.RespondWith(HttpStatusCode.OK, UserJson(login, id, name));

            var result = await _sut.GetUserAsync(_token, Ct);

            result.Login.Should().Be(login);
            result.Id.Should().Be(id);
            result.Name.Should().Be(name);
        }

        [Fact]
        public async Task GetUserAsync_WithNullName_ReturnsUserWithoutName()
        {
            _handler.RespondWith(HttpStatusCode.OK, UserJson(Faker.Internet.UserName(), Faker.Random.Long(1), null));

            var result = await _sut.GetUserAsync(_token, Ct);

            result.Name.Should().BeNull();
        }

        [Fact]
        public async Task GetUserAsync_WhenResponseIsNotSuccess_ThrowsWithStatusAndBody()
        {
            var body = Faker.Random.AlphaNumeric(40);
            _handler.RespondWith(HttpStatusCode.Unauthorized, body);

            var act = () => _sut.GetUserAsync(_token, Ct);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be($"GitHub API returned 401: {body}");
        }

        [Fact]
        public async Task GetUserAsync_WhenErrorBodyExceedsLimit_TruncatesBodyInMessage()
        {
            var body = Faker.Random.AlphaNumeric(MaxErrorSnippetLength + 1);
            _handler.RespondWith(HttpStatusCode.InternalServerError, body);

            var act = () => _sut.GetUserAsync(_token, Ct);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be($"GitHub API returned 500: {body[..MaxErrorSnippetLength]}…");
        }

        [Fact]
        public async Task GetUserAsync_WhenErrorBodyIsAtLimit_KeepsWholeBodyInMessage()
        {
            var body = Faker.Random.AlphaNumeric(MaxErrorSnippetLength);
            _handler.RespondWith(HttpStatusCode.InternalServerError, body);

            var act = () => _sut.GetUserAsync(_token, Ct);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be($"GitHub API returned 500: {body}");
        }

        [Fact]
        public async Task GetMaxDailyContributionsAsync_WithLogin_PostsLoginToGraphqlWithBearerToken()
        {
            var login = Faker.Internet.UserName();
            _handler.RespondWith(HttpStatusCode.OK, CalendarJson([[Faker.Random.Int(0, 50)]]));

            await _sut.GetMaxDailyContributionsAsync(_token, login, Ct);

            _handler.LastMethod.Should().Be(HttpMethod.Post);
            _handler.LastUri.Should().Be(new Uri(_baseAddress, "graphql"));
            AssertBearerToken();
            using var document = JsonDocument.Parse(_handler.LastBody!);
            document.RootElement.GetProperty("variables").GetProperty("login").GetString().Should().Be(login);
        }

        [Fact]
        public async Task GetMaxDailyContributionsAsync_WithSeveralWeeks_ReturnsHighestDailyCount()
        {
            var highest = Faker.Random.Int(100, 200);
            _handler.RespondWith(HttpStatusCode.OK, CalendarJson([[1, 2], [3, highest, 4], [5]]));

            var result = await _sut.GetMaxDailyContributionsAsync(_token, Faker.Internet.UserName(), Ct);

            result.Should().Be(highest);
        }

        [Fact]
        public async Task GetMaxDailyContributionsAsync_WithNoWeeks_ReturnsZero()
        {
            _handler.RespondWith(HttpStatusCode.OK, CalendarJson([]));

            var result = await _sut.GetMaxDailyContributionsAsync(_token, Faker.Internet.UserName(), Ct);

            result.Should().Be(0);
        }

        [Fact]
        public async Task GetMaxDailyContributionsAsync_WhenResponseHasErrors_ThrowsGraphQlError()
        {
            var message = Faker.Lorem.Word();
            _handler.RespondWith(HttpStatusCode.OK, JsonSerializer.Serialize(new { errors = new[] { new { message } } }));

            var act = () => _sut.GetMaxDailyContributionsAsync(_token, Faker.Internet.UserName(), Ct);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().StartWith("GitHub GraphQL error:").And.Contain(message);
        }

        [Fact]
        public async Task GetMaxDailyContributionsAsync_WhenResponseIsNotSuccess_ThrowsWithStatusAndBody()
        {
            var body = Faker.Random.AlphaNumeric(40);
            _handler.RespondWith(HttpStatusCode.BadGateway, body);

            var act = () => _sut.GetMaxDailyContributionsAsync(_token, Faker.Internet.UserName(), Ct);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be($"GitHub API returned 502: {body}");
        }

        [Fact]
        public async Task IsRepositoryEmptyAsync_WhenGitHubReturnsConflict_ReturnsTrue()
        {
            _handler.RespondWith(HttpStatusCode.Conflict, Faker.Lorem.Sentence());

            var result = await _sut.IsRepositoryEmptyAsync(_token, Faker.Internet.UserName(), Faker.Lorem.Word(), Ct);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsRepositoryEmptyAsync_WhenGitHubReturnsOk_ReturnsFalse()
        {
            _handler.RespondWith(HttpStatusCode.OK, "[]");

            var result = await _sut.IsRepositoryEmptyAsync(_token, Faker.Internet.UserName(), Faker.Lorem.Word(), Ct);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsRepositoryEmptyAsync_WhenGitHubReturnsNotFound_ThrowsRepositoryNotFound()
        {
            var owner = Faker.Internet.UserName();
            var repo = Faker.Lorem.Word();
            _handler.RespondWith(HttpStatusCode.NotFound, Faker.Lorem.Sentence());

            var act = () => _sut.IsRepositoryEmptyAsync(_token, owner, repo, Ct);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().StartWith($"Repository {owner}/{repo} was not found");
        }

        [Fact]
        public async Task IsRepositoryEmptyAsync_WhenGitHubReturnsServerError_ThrowsWithStatusAndBody()
        {
            var body = Faker.Random.AlphaNumeric(40);
            _handler.RespondWith(HttpStatusCode.InternalServerError, body);

            var act = () => _sut.IsRepositoryEmptyAsync(_token, Faker.Internet.UserName(), Faker.Lorem.Word(), Ct);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be($"GitHub API returned 500: {body}");
        }

        [Fact]
        public async Task IsRepositoryEmptyAsync_WithToken_SendsBearerToken()
        {
            _handler.RespondWith(HttpStatusCode.Conflict, Faker.Lorem.Sentence());

            await _sut.IsRepositoryEmptyAsync(_token, Faker.Internet.UserName(), Faker.Lorem.Word(), Ct);

            AssertBearerToken();
        }

        [Fact]
        public async Task IsRepositoryEmptyAsync_WithReservedCharacters_EscapesOwnerAndRepository()
        {
            var ownerStart = Faker.Lorem.Word();
            var ownerEnd = Faker.Lorem.Word();
            var repoStart = Faker.Lorem.Word();
            var repoEnd = Faker.Lorem.Word();
            _handler.RespondWith(HttpStatusCode.Conflict, Faker.Lorem.Sentence());

            await _sut.IsRepositoryEmptyAsync(_token, $"{ownerStart} {ownerEnd}", $"{repoStart}/{repoEnd}", Ct);

            _handler.LastUri!.PathAndQuery.Should().Be($"/repos/{ownerStart}%20{ownerEnd}/{repoStart}%2F{repoEnd}/commits?per_page=1");
        }

        private void AssertBearerToken()
        {
            _handler.LastAuthorization!.Scheme.Should().Be("Bearer");
            _handler.LastAuthorization.Parameter.Should().Be(_token);
        }

        private static string UserJson(string login, long id, string? name)
        {
            return JsonSerializer.Serialize(new { login, id, name });
        }

        private static string CalendarJson(int[][] weeks)
        {
            var calendar = new
            {
                data = new
                {
                    user = new
                    {
                        contributionsCollection = new
                        {
                            contributionCalendar = new
                            {
                                weeks = weeks.Select(ToWeek).ToArray(),
                            },
                        },
                    },
                },
            };

            return JsonSerializer.Serialize(calendar);
        }

        private static object ToWeek(int[] counts)
        {
            return new { contributionDays = counts.Select(ToDay).ToArray() };
        }

        private static object ToDay(int count)
        {
            return new { contributionCount = count };
        }
    }
}
