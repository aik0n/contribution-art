using Bogus;
using ContributionArt.Services;
using FluentAssertions;
using Xunit;

namespace ContributionArt.Tests.Services
{
    [UnitTestTrait]
    public class GitHubUserTests
    {
        private static readonly Faker Faker = new("en") { Random = new Randomizer(42) };

        [Fact]
        public void NoReplyEmail_WithIdAndLogin_ReturnsGitHubNoReplyAddress()
        {
            var login = Faker.Internet.UserName();
            var id = Faker.Random.Long(1);
            var sut = new GitHubUser(login, id, Faker.Name.FullName());

            var result = sut.NoReplyEmail;

            result.Should().Be($"{id}+{login}@users.noreply.github.com");
        }
    }
}
