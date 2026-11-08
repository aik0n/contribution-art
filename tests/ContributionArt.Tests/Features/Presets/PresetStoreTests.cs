using System.Text.Json;
using Bogus;
using ContributionArt.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ContributionArt.Tests.Services
{
    [UnitTestTrait]
    public sealed class PresetStoreTests : IDisposable
    {
        private const string Digits = "01234";
        private const int Width = 5;

        private static readonly Faker Faker = new("en") { Random = new Randomizer(42) };

        private readonly PresetDefinition _predefined = CreateDefinition();
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ContributionArtTests-{Guid.NewGuid():N}");
        private readonly string _userFilePath;
        private readonly PresetStore _sut;

        public PresetStoreTests()
        {
            Directory.CreateDirectory(_directory);
            _userFilePath = Path.Combine(_directory, "presets-user.json");
            _sut = CreateStore();
        }

        public void Dispose()
        {
            Directory.Delete(_directory, recursive: true);
        }

        [Fact]
        public void Presets_BeforeReload_ContainsPredefinedPresets()
        {
            var result = _sut.Presets;

            result.Should().ContainSingle().Which.Name.Should().Be(_predefined.Name);
        }

        [Fact]
        public void ReloadUserPresets_WhenFileIsMissing_KeepsOnlyPredefinedPresets()
        {
            _sut.ReloadUserPresets();

            _sut.Presets.Should().ContainSingle().Which.Name.Should().Be(_predefined.Name);
        }

        [Fact]
        public void ReloadUserPresets_WithHandEditedFile_AddsItsPresets()
        {
            var edited = CreateDefinition();
            WriteUserFile(edited);

            _sut.ReloadUserPresets();

            Names().Should().Equal(_predefined.Name, edited.Name);
        }

        [Fact]
        public void ReloadUserPresets_WithEmptyPresetsArray_KeepsOnlyPredefinedPresets()
        {
            File.WriteAllText(_userFilePath, """{ "Presets": [] }""");

            _sut.ReloadUserPresets();

            Names().Should().Equal(_predefined.Name);
        }

        [Fact]
        public void ReloadUserPresets_WithManyPresets_KeepsFileOrder()
        {
            var definitions = new PresetDefinition[12];
            var expected = new List<string> { _predefined.Name };
            for (var i = 0; i < definitions.Length; i++)
            {
                definitions[i] = CreateDefinition();
                expected.Add(definitions[i].Name);
            }

            WriteUserFile(definitions);

            _sut.ReloadUserPresets();

            Names().Should().Equal(expected);
        }

        [Fact]
        public void ReloadUserPresets_AfterFileChanges_RemovesDeletedPreset()
        {
            WriteUserFile(CreateDefinition());
            _sut.ReloadUserPresets();
            WriteUserFile();

            _sut.ReloadUserPresets();

            Names().Should().Equal(_predefined.Name);
        }

        [Fact]
        public void ReloadUserPresets_WithCorruptFile_ThrowsAndKeepsLastGoodPresets()
        {
            var good = CreateDefinition();
            WriteUserFile(good);
            _sut.ReloadUserPresets();
            File.WriteAllText(_userFilePath, Faker.Random.AlphaNumeric(20));

            var act = _sut.ReloadUserPresets;

            act.Should().Throw<PresetStoreException>().Which.Message.Should().Contain(_userFilePath);
            Names().Should().Equal(_predefined.Name, good.Name);
        }

        [Fact]
        public void ReloadUserPresets_WithInvalidPreset_ThrowsAndKeepsLastGoodPresets()
        {
            var invalid = CreateDefinition();
            invalid.Rows[0] = "5" + invalid.Rows[0][1..];
            WriteUserFile(invalid);

            var act = _sut.ReloadUserPresets;

            act.Should().Throw<PresetStoreException>().Which.Message.Should().Contain(invalid.Name);
            Names().Should().Equal(_predefined.Name);
        }

        [Fact]
        public void ReloadUserPresets_WithNameOfPredefinedPreset_Throws()
        {
            var duplicate = CreateDefinition();
            duplicate.Name = _predefined.Name.ToUpperInvariant();
            WriteUserFile(duplicate);

            var act = _sut.ReloadUserPresets;

            act.Should().Throw<PresetStoreException>().Which.Message.Should().Contain("already exists");
        }

        [Fact]
        public void SaveUserPreset_WithValidPreset_AddsPresetAfterPredefinedOnes()
        {
            var name = CreateName();
            var pattern = CreatePattern();

            _sut.SaveUserPreset(name, pattern);

            Names().Should().Equal(_predefined.Name, name);
            _sut.Presets[1].Pattern.Should().BeEquivalentTo(pattern, options => options.WithStrictOrdering());
        }

        [Fact]
        public void SaveUserPreset_WithValidPreset_NewStoreReadsItFromFile()
        {
            var name = CreateName();
            var pattern = CreatePattern();
            _sut.SaveUserPreset(name, pattern);
            var restarted = CreateStore();

            restarted.ReloadUserPresets();

            restarted.Presets.Should().HaveCount(2);
            restarted.Presets[1].Name.Should().Be(name);
            restarted.Presets[1].Pattern.Should().BeEquivalentTo(pattern, options => options.WithStrictOrdering());
        }

        [Fact]
        public void SaveUserPreset_WhenFolderIsMissing_CreatesIt()
        {
            var nestedPath = Path.Combine(_directory, Faker.Random.AlphaNumeric(8), "presets-user.json");
            var store = CreateStore(nestedPath);

            store.SaveUserPreset(CreateName(), CreatePattern());

            File.Exists(nestedPath).Should().BeTrue();
        }

        [Fact]
        public void SaveUserPreset_WithSurroundingSpaces_TrimsName()
        {
            var name = CreateName();

            _sut.SaveUserPreset($"  {name}  ", CreatePattern());

            _sut.Presets[1].Name.Should().Be(name);
        }

        [Fact]
        public void SaveUserPreset_WithNonLatinName_KeepsNameReadableInFile()
        {
            var name = "Сердце";

            _sut.SaveUserPreset(name, CreatePattern());

            File.ReadAllText(_userFilePath).Should().Contain(name);
        }

        [Fact]
        public void SaveUserPreset_AfterSuccess_LeavesOnlyTheUserFile()
        {
            _sut.SaveUserPreset(CreateName(), CreatePattern());

            Directory.GetFiles(_directory).Should().Equal(_userFilePath);
        }

        [Fact]
        public void SaveUserPreset_WithValidHandEdit_KeepsHandEditedPreset()
        {
            var edited = CreateDefinition();
            WriteUserFile(edited);
            var name = CreateName();

            _sut.SaveUserPreset(name, CreatePattern());

            Names().Should().Equal(_predefined.Name, edited.Name, name);
        }

        [Fact]
        public void SaveUserPreset_WithNameOfPredefinedPresetInOtherCase_ThrowsAndWritesNothing()
        {
            var act = () => _sut.SaveUserPreset(_predefined.Name.ToUpperInvariant(), CreatePattern());

            act.Should().Throw<PresetStoreException>().Which.Message.Should().Contain("already exists");
            File.Exists(_userFilePath).Should().BeFalse();
        }

        [Fact]
        public void SaveUserPreset_WithNameOfUserPresetInOtherCase_ThrowsAndLeavesFileUnchanged()
        {
            var name = CreateName();
            _sut.SaveUserPreset(name, CreatePattern());
            var before = File.ReadAllText(_userFilePath);

            var act = () => _sut.SaveUserPreset(name.ToUpperInvariant(), CreatePattern());

            act.Should().Throw<PresetStoreException>().Which.Message.Should().Contain("already exists");
            File.ReadAllText(_userFilePath).Should().Be(before);
        }

        [Fact]
        public void SaveUserPreset_WithEmptyPattern_ThrowsAndWritesNothing()
        {
            var act = () => _sut.SaveUserPreset(CreateName(), []);

            act.Should().Throw<PresetStoreException>();
            File.Exists(_userFilePath).Should().BeFalse();
        }

        [Fact]
        public void SaveUserPreset_WithBlankName_ThrowsAndWritesNothing()
        {
            var act = () => _sut.SaveUserPreset("   ", CreatePattern());

            act.Should().Throw<PresetStoreException>();
            File.Exists(_userFilePath).Should().BeFalse();
        }

        [Fact]
        public void SaveUserPreset_WithCorruptFile_ThrowsAndLeavesFileUnchanged()
        {
            var corrupt = Faker.Random.AlphaNumeric(20);
            File.WriteAllText(_userFilePath, corrupt);

            var act = () => _sut.SaveUserPreset(CreateName(), CreatePattern());

            act.Should().Throw<PresetStoreException>();
            File.ReadAllText(_userFilePath).Should().Be(corrupt);
        }

        [Fact]
        public void SaveUserPreset_WithMisspelledSection_ThrowsAndLeavesFileUnchanged()
        {
            var content = """{ "Presetz": [] , "Other": "value" }""";
            File.WriteAllText(_userFilePath, content);

            var act = () => _sut.SaveUserPreset(CreateName(), CreatePattern());

            act.Should().Throw<PresetStoreException>();
            File.ReadAllText(_userFilePath).Should().Be(content);
        }

        [Fact]
        public void SaveUserPreset_WithPresetsNotAnArray_ThrowsAndLeavesFileUnchanged()
        {
            var content = """{ "Presets": 5 }""";
            File.WriteAllText(_userFilePath, content);

            var act = () => _sut.SaveUserPreset(CreateName(), CreatePattern());

            act.Should().Throw<PresetStoreException>();
            File.ReadAllText(_userFilePath).Should().Be(content);
        }

        private static string CreateName()
        {
            return Faker.Random.AlphaNumeric(12);
        }

        private static int[][] CreatePattern()
        {
            var pattern = new int[ContributionGrid.Days][];
            for (var day = 0; day < pattern.Length; day++)
            {
                pattern[day] = new int[Width];
                for (var x = 0; x < Width; x++)
                {
                    pattern[day][x] = Faker.Random.Int(0, ContributionGrid.MaxLevel);
                }
            }

            return pattern;
        }

        private static PresetDefinition CreateDefinition()
        {
            var rows = new string[ContributionGrid.Days];
            for (var day = 0; day < rows.Length; day++)
            {
                rows[day] = Faker.Random.String2(Width, Digits);
            }

            return new PresetDefinition { Name = CreateName(), Rows = rows };
        }

        private PresetStore CreateStore()
        {
            return CreateStore(_userFilePath);
        }

        private PresetStore CreateStore(string userFilePath)
        {
            var options = new PresetOptions { Predefined = [_predefined], UserFilePath = userFilePath };
            return new PresetStore(Options.Create(options));
        }

        private void WriteUserFile(params PresetDefinition[] definitions)
        {
            var content = new Dictionary<string, PresetDefinition[]> { [PresetOptions.Presets] = definitions };
            File.WriteAllText(_userFilePath, JsonSerializer.Serialize(content));
        }

        private List<string> Names()
        {
            var names = new List<string>();
            foreach (var preset in _sut.Presets)
            {
                names.Add(preset.Name);
            }

            return names;
        }
    }
}
