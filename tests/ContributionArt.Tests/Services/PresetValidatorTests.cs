using Bogus;
using ContributionArt.Services;
using FluentAssertions;
using Xunit;

namespace ContributionArt.Tests.Services
{
    [UnitTestTrait]
    public class PresetValidatorTests
    {
        private const string ValidDigits = "01234";
        private const int ValidWidth = 9;

        private static readonly Faker Faker = new("en") { Random = new Randomizer(42) };

        public static IEnumerable<object[]> ValidPresets()
        {
            yield return [Preset(ValidName(), Rows(ValidWidth))];                                     // baseline
            yield return [Preset(ValidName(), Rows(1))];                                              // narrowest
            yield return [Preset(ValidName(), Rows(ContributionGrid.Weeks))];                         // widest
            yield return [Preset(Faker.Random.String2(1), Rows(ValidWidth))];                         // shortest name
            yield return [Preset(Faker.Random.String2(PresetValidator.MaxNameLength), Rows(ValidWidth))]; // longest name
            yield return [Preset(ValidName(), RowsWithFirstCell("0"))];                               // lowest level
            yield return [Preset(ValidName(), RowsWithFirstCell("4"))];                               // highest level
        }

        public static IEnumerable<object[]> InvalidPresets()
        {
            yield return [Preset(null!, Rows(ValidWidth))];                                           // missing name
            yield return [Preset("", Rows(ValidWidth))];                                              // empty name
            yield return [Preset("   ", Rows(ValidWidth))];                                           // whitespace name
            yield return [Preset(Faker.Random.String2(PresetValidator.MaxNameLength + 1), Rows(ValidWidth))]; // name too long
            yield return [Preset(ValidName(), null!)];                                                // missing rows
            yield return [Preset(ValidName(), [])];                                                   // empty rows
            yield return [Preset(ValidName(), Rows(ValidWidth, ContributionGrid.Days - 1))];          // too few rows
            yield return [Preset(ValidName(), Rows(ValidWidth, ContributionGrid.Days + 1))];          // too many rows
            yield return [Preset(ValidName(), Rows(0))];                                              // no columns
            yield return [Preset(ValidName(), Rows(ContributionGrid.Weeks + 1))];                     // too wide
            yield return [Preset(ValidName(), WithRow(Rows(ValidWidth), Faker.Random.String2(ValidWidth + 1, ValidDigits)))]; // uneven width
            yield return [Preset(ValidName(), WithRow(Rows(ValidWidth), null!))];                     // missing row
            yield return [Preset(ValidName(), RowsWithFirstCell("5"))];                               // level above maximum
            yield return [Preset(ValidName(), RowsWithFirstCell("x"))];                               // not a digit
            yield return [Preset(ValidName(), RowsWithFirstCell("-"))];                               // sign
        }

        public static IEnumerable<object[]> MultipleProblems()
        {
            yield return [Preset(null!, Rows(ValidWidth, ContributionGrid.Days - 1))];                // no name, too few rows
            yield return [Preset("   ", RowsWithFirstCell("5"))];                                     // whitespace name, bad level
        }

        [Theory]
        [MemberData(nameof(ValidPresets))]
        public void Validate_WithValidPreset_ReturnsNoErrors(PresetDefinition preset)
        {
            var result = PresetValidator.Validate([preset]);

            result.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(InvalidPresets))]
        public void Validate_WithInvalidPreset_ReturnsOneError(PresetDefinition preset)
        {
            var result = PresetValidator.Validate([preset]);

            result.Should().ContainSingle();
        }

        [Theory]
        [MemberData(nameof(MultipleProblems))]
        public void Validate_WithSeveralProblems_ReturnsEveryError(PresetDefinition preset)
        {
            var result = PresetValidator.Validate([preset]);

            result.Should().HaveCount(2);
        }

        [Fact]
        public void Validate_WithSameNameInOtherCase_ReportsDuplicate()
        {
            var name = ValidName();
            var presets = new[] { Preset(name, Rows(ValidWidth)), Preset(name.ToUpperInvariant(), Rows(ValidWidth)) };

            var result = PresetValidator.Validate(presets);

            result.Should().ContainSingle().Which.Should().Contain("already exists");
        }

        [Fact]
        public void Validate_WithSameNameAndExtraSpaces_ReportsDuplicate()
        {
            var name = ValidName();
            var presets = new[] { Preset(name, Rows(ValidWidth)), Preset($"  {name}  ", Rows(ValidWidth)) };

            var result = PresetValidator.Validate(presets);

            result.Should().ContainSingle().Which.Should().Contain("already exists");
        }

        [Fact]
        public void Validate_WithNullCollection_ThrowsArgumentNullException()
        {
            var act = () => PresetValidator.Validate(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Encode_WithPattern_ReturnsDigitRows()
        {
            int[][] pattern = [[0, 1, 2], [3, 4, 0]];

            var result = PresetValidator.Encode(pattern);

            result.Should().Equal("012", "340");
        }

        [Fact]
        public void Decode_WithDigitRows_ReturnsPattern()
        {
            string[] rows = ["012", "340"];

            var result = PresetValidator.Decode(rows);

            result.Should().BeEquivalentTo(new[] { new[] { 0, 1, 2 }, new[] { 3, 4, 0 } }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void Decode_AfterEncode_ReturnsOriginalPattern()
        {
            var rows = Rows(ValidWidth);

            var result = PresetValidator.Encode(PresetValidator.Decode(rows));

            result.Should().Equal(rows);
        }

        private static PresetDefinition Preset(string name, string[] rows)
        {
            return new PresetDefinition { Name = name, Rows = rows };
        }

        private static string ValidName()
        {
            return Faker.Random.AlphaNumeric(12);
        }

        private static string[] Rows(int width, int count = ContributionGrid.Days)
        {
            var rows = new string[count];
            for (var day = 0; day < count; day++)
            {
                rows[day] = Faker.Random.String2(width, ValidDigits);
            }

            return rows;
        }

        private static string[] RowsWithFirstCell(string cell)
        {
            var rows = Rows(ValidWidth);
            rows[0] = cell + rows[0][1..];
            return rows;
        }

        private static string[] WithRow(string[] rows, string row)
        {
            rows[^1] = row;
            return rows;
        }
    }
}
