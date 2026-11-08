namespace ContributionArt.Services
{
    /// <summary>The rules every preset must follow, and the conversion between stored rows and patterns.</summary>
    public static class PresetValidator
    {
        public const int MaxNameLength = 40;

        /// <summary>Returns one message per problem found. An empty result means the presets are valid.</summary>
        public static IReadOnlyList<string> Validate(IEnumerable<PresetDefinition> presets)
        {
            ArgumentNullException.ThrowIfNull(presets);

            var errors = new List<string>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var preset in presets)
            {
                ValidateName(preset, names, errors);
                ValidateRows(preset, errors);
            }

            return errors;
        }

        public static string[] Encode(int[][] pattern)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            var rows = new string[pattern.Length];
            for (var day = 0; day < pattern.Length; day++)
            {
                var digits = new char[pattern[day].Length];
                for (var x = 0; x < digits.Length; x++)
                {
                    digits[x] = (char)('0' + pattern[day][x]);
                }

                rows[day] = new string(digits);
            }

            return rows;
        }

        public static int[][] Decode(string[] rows)
        {
            ArgumentNullException.ThrowIfNull(rows);

            var pattern = new int[rows.Length][];
            for (var day = 0; day < rows.Length; day++)
            {
                pattern[day] = new int[rows[day].Length];
                for (var x = 0; x < rows[day].Length; x++)
                {
                    pattern[day][x] = rows[day][x] - '0';
                }
            }

            return pattern;
        }

        private static void ValidateName(PresetDefinition preset, HashSet<string> names, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(preset.Name))
            {
                errors.Add("A preset has no name.");
                return;
            }

            var name = preset.Name.Trim();
            if (name.Length > MaxNameLength)
            {
                errors.Add($"Preset '{name}' has a name longer than {MaxNameLength} characters.");
            }

            if (!names.Add(name))
            {
                errors.Add($"A preset named '{name}' already exists.");
            }
        }

        private static void ValidateRows(PresetDefinition preset, List<string> errors)
        {
            if (preset.Rows is null || preset.Rows.Length != ContributionGrid.Days)
            {
                errors.Add($"Preset '{preset.Name}' must have exactly {ContributionGrid.Days} rows.");
                return;
            }

            var width = preset.Rows[0]?.Length ?? 0;
            if (width is < 1 or > ContributionGrid.Weeks)
            {
                errors.Add($"Preset '{preset.Name}' must be between 1 and {ContributionGrid.Weeks} columns wide.");
                return;
            }

            foreach (var row in preset.Rows)
            {
                if (row is null || row.Length != width)
                {
                    errors.Add($"Preset '{preset.Name}' must have rows of equal width.");
                    return;
                }

                if (!HasOnlyLevels(row))
                {
                    errors.Add($"Preset '{preset.Name}' may only use the digits 0 to {ContributionGrid.MaxLevel}.");
                    return;
                }
            }
        }

        private static bool HasOnlyLevels(string row)
        {
            foreach (var digit in row)
            {
                if (digit < '0' || digit > '0' + ContributionGrid.MaxLevel)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
