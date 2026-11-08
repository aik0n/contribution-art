using Bogus;
using ContributionArt.Features.Canvas;
using FluentAssertions;
using Xunit;

namespace ContributionArt.Tests.Features.Canvas
{
    [UnitTestTrait]
    public class ContributionGridTests
    {
        // A Saturday, so every cell of the last week is in the past and can be painted.
        private static readonly DateOnly Today = new(2026, 11, 7);

        private static readonly Faker Faker = new("en") { Random = new Randomizer(42) };

        private readonly ContributionGrid _sut = new(Today);

        [Fact]
        public void ToPattern_WithEmptyCanvas_ReturnsEmptyArray()
        {
            var result = _sut.ToPattern();

            result.Should().BeEmpty();
        }

        [Fact]
        public void ToPattern_WithSinglePixel_ReturnsOneColumnOfSevenRows()
        {
            var level = RandomLevel();
            var day = Faker.Random.Int(0, ContributionGrid.Days - 1);
            _sut.Set(Faker.Random.Int(0, ContributionGrid.Weeks - 1), day, level);

            var result = _sut.ToPattern();

            result.Should().HaveCount(ContributionGrid.Days);
            for (var row = 0; row < result.Length; row++)
            {
                result[row].Should().Equal(row == day ? level : 0);
            }
        }

        [Fact]
        public void ToPattern_WithPixelsInFirstAndLastWeek_ReturnsFullWidth()
        {
            _sut.Set(0, 0, RandomLevel());
            _sut.Set(ContributionGrid.Weeks - 1, 0, RandomLevel());

            var result = _sut.ToPattern();

            result[0].Should().HaveCount(ContributionGrid.Weeks);
        }

        [Fact]
        public void ToPattern_WithBlankWeekBetweenPixels_KeepsTheGap()
        {
            var first = RandomLevel();
            var second = RandomLevel();
            _sut.Set(10, 3, first);
            _sut.Set(12, 3, second);

            var result = _sut.ToPattern();

            result[3].Should().Equal(first, 0, second);
        }

        [Fact]
        public void ToPattern_AfterStampOnAnotherGrid_ReproducesTheDrawing()
        {
            _sut.Set(4, 1, RandomLevel());
            _sut.Set(6, 5, RandomLevel());
            _sut.Set(9, 2, RandomLevel());
            var pattern = _sut.ToPattern();
            var other = new ContributionGrid(Today);

            other.Stamp(pattern, 4);

            other.Pixels().Should().Equal(_sut.Pixels());
        }

        private static int RandomLevel()
        {
            return Faker.Random.Int(1, ContributionGrid.MaxLevel);
        }
    }
}
