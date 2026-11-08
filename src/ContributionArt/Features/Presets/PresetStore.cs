using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContributionArt.Services
{
    /// <summary>
    /// Reads presets from the validated options and from the user file. Configuration can only read,
    /// so saving writes the user file directly and then loads it back.
    /// </summary>
    public sealed class PresetStore : IPresetStore
    {
        private const string UserFolderName = "ContributionArt";
        private const string UserFileName = "presets-user.json";

        // Keeps names in other alphabets readable in the file. The file is never embedded in a web page.
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private readonly Lock _gate = new();
        private readonly IReadOnlyList<PresetDefinition> _predefined;
        private readonly string _userFilePath;
        private volatile IReadOnlyList<Preset> _presets;

        public PresetStore(IOptions<PresetOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _predefined = options.Value.Predefined;
            _userFilePath = ResolveUserFilePath(options.Value.UserFilePath);
            _presets = BuildPresets([]);
        }

        public IReadOnlyList<Preset> Presets
        {
            get { return _presets; }
        }

        public void ReloadUserPresets()
        {
            lock (_gate)
            {
                _presets = BuildPresets(ReadValidUserPresets());
            }
        }

        public void SaveUserPreset(string name, int[][] pattern)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(pattern);

            lock (_gate)
            {
                // Reading first keeps hand edits, and an invalid file stops here instead of being overwritten.
                var userPresets = ReadValidUserPresets();
                userPresets.Add(new PresetDefinition { Name = name.Trim(), Rows = PresetValidator.Encode(pattern) });
                ThrowIfInvalid(userPresets, "");

                WriteUserFile(userPresets);

                _presets = BuildPresets(ReadValidUserPresets());
            }
        }

        private static string ResolveUserFilePath(string configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            return Path.Combine(localData, UserFolderName, UserFileName);
        }

        private static bool IsFileFailure(Exception exception)
        {
            return exception is IOException or UnauthorizedAccessException or InvalidDataException or FormatException;
        }

        private List<PresetDefinition> ReadValidUserPresets()
        {
            var userPresets = ReadUserPresets();
            ThrowIfInvalid(userPresets, $"{_userFilePath}: ");
            return userPresets;
        }

        private List<PresetDefinition> ReadUserPresets()
        {
            IConfigurationRoot root;
            try
            {
                root = new ConfigurationBuilder()
                    .AddJsonFile(_userFilePath, optional: true, reloadOnChange: false)
                    .Build();
            }
            catch (Exception exception) when (IsFileFailure(exception))
            {
                var detail = exception.InnerException ?? exception;
                throw new PresetStoreException($"{_userFilePath} cannot be read: {detail.Message}", exception);
            }

            ThrowIfUnexpectedContent(root);

            return root.GetSection(PresetOptions.Presets).Get<List<PresetDefinition>>() ?? [];
        }

        // A misspelled section would read as "no presets", and the next save would then replace the file.
        private void ThrowIfUnexpectedContent(IConfigurationRoot root)
        {
            foreach (var section in root.GetChildren())
            {
                // An empty JSON array is exposed by configuration as an empty value.
                if (!string.Equals(section.Key, PresetOptions.Presets, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(section.Value))
                {
                    throw new PresetStoreException($"{_userFilePath} must contain only a '{PresetOptions.Presets}' array.");
                }
            }
        }

        private void ThrowIfInvalid(List<PresetDefinition> userPresets, string messagePrefix)
        {
            var all = new List<PresetDefinition>(_predefined);
            all.AddRange(userPresets);

            var errors = PresetValidator.Validate(all);
            if (errors.Count > 0)
            {
                throw new PresetStoreException(messagePrefix + string.Join(" ", errors));
            }
        }

        private void WriteUserFile(List<PresetDefinition> userPresets)
        {
            var content = new Dictionary<string, List<PresetDefinition>>
            {
                [PresetOptions.Presets] = userPresets,
            };
            var json = JsonSerializer.Serialize(content, WriteOptions);

            try
            {
                var directory = Path.GetDirectoryName(_userFilePath)!;
                Directory.CreateDirectory(directory);
                ReplaceFile(directory, json);
            }
            catch (Exception exception) when (IsFileFailure(exception))
            {
                throw new PresetStoreException($"{_userFilePath} cannot be written: {exception.Message}", exception);
            }
        }

        // The temporary file is beside the target so the final move is a rename, and a failed write leaves the old file intact.
        private void ReplaceFile(string directory, string json)
        {
            var temporaryPath = Path.Combine(directory, $"{Path.GetFileName(_userFilePath)}.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, _userFilePath, overwrite: true);
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }

        private List<Preset> BuildPresets(List<PresetDefinition> userPresets)
        {
            var presets = new List<Preset>(_predefined.Count + userPresets.Count);
            AddDecoded(presets, _predefined);
            AddDecoded(presets, userPresets);
            return presets;
        }

        private static void AddDecoded(List<Preset> presets, IEnumerable<PresetDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                presets.Add(new Preset(definition.Name.Trim(), PresetValidator.Decode(definition.Rows)));
            }
        }
    }
}
