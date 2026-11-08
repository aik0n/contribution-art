namespace ContributionArt.Services
{
    /// <summary>Ready-made shapes and a tiny 3x5 pixel font. Every pattern is 7 rows tall (Sunday..Saturday).</summary>
    public static class Presets
    {
        public static readonly IReadOnlyDictionary<string, int[][]> Shapes = new Dictionary<string, int[][]>
        {
            ["Heart"] =
            [
                [0, 3, 3, 0, 0, 0, 3, 3, 0],
                [3, 4, 4, 3, 0, 3, 4, 4, 3],
                [3, 4, 4, 4, 3, 4, 4, 4, 3],
                [0, 3, 4, 4, 4, 4, 4, 3, 0],
                [0, 0, 3, 4, 4, 4, 3, 0, 0],
                [0, 0, 0, 3, 4, 3, 0, 0, 0],
                [0, 0, 0, 0, 3, 0, 0, 0, 0],
            ],
            ["Cat"] =
            [
                [4, 0, 0, 0, 0, 0, 4],
                [4, 4, 0, 0, 0, 4, 4],
                [4, 4, 4, 4, 4, 4, 4],
                [4, 1, 4, 4, 4, 1, 4],
                [4, 4, 4, 2, 4, 4, 4],
                [0, 4, 4, 4, 4, 4, 0],
                [0, 0, 4, 4, 4, 0, 0],
            ],
            ["Gliders"] =
            [
                [0, 4, 0, 0, 0, 0, 0, 0, 0],
                [0, 0, 4, 0, 0, 0, 0, 0, 0],
                [4, 4, 4, 0, 0, 0, 0, 0, 0],
                [0, 0, 0, 0, 0, 0, 4, 0, 0],
                [0, 0, 0, 0, 0, 0, 0, 4, 0],
                [0, 0, 0, 0, 0, 4, 4, 4, 0],
                [0, 0, 0, 0, 0, 0, 0, 0, 0],
            ],
            ["Wave"] = BuildWave(26),
            ["Gradient"] = BuildGradient(),
        };

        // 3 columns x 5 rows per glyph, written row by row.
        private static readonly Dictionary<char, string> Font = new()
        {
            ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011",
            ['D'] = "110101101101110", ['E'] = "111100110100111", ['F'] = "111100110100100",
            ['G'] = "011100101101011", ['H'] = "101101111101101", ['I'] = "111010010010111",
            ['J'] = "001001001101010", ['K'] = "101101110101101", ['L'] = "100100100100111",
            ['M'] = "101111111101101", ['N'] = "110101101101101", ['O'] = "010101101101010",
            ['P'] = "110101110100100", ['Q'] = "010101101110011", ['R'] = "110101110101101",
            ['S'] = "011100010001110", ['T'] = "111010010010010", ['U'] = "101101101101111",
            ['V'] = "101101101101010", ['W'] = "101101111111101", ['X'] = "101101010101101",
            ['Y'] = "101101010010010", ['Z'] = "111001010100111",
            ['0'] = "111101101101111", ['1'] = "010110010010111", ['2'] = "110001010100111",
            ['3'] = "110001010001110", ['4'] = "101101111001001", ['5'] = "111100110001110",
            ['6'] = "011100111101111", ['7'] = "111001010010010", ['8'] = "111101111101111",
            ['9'] = "111101111001110",
            [' '] = "000000000000000", ['!'] = "010010010000010", ['-'] = "000000111000000",
            ['.'] = "000000000000010", ['?'] = "110001010000010",
        };

        public static bool IsSupported(char c) => Font.ContainsKey(char.ToUpperInvariant(c));

        /// <summary>Renders text in the 3x5 font, vertically centred (rows 1-5 of 7), one empty column between letters.</summary>
        public static int[][] Text(string text, int level)
        {
            var glyphs = text.ToUpperInvariant().Where(Font.ContainsKey).Select(c => Font[c]).ToList();
            var width = Math.Max(0, glyphs.Count * 4 - 1);
            var rows = Enumerable.Range(0, ContributionGrid.Days).Select(_ => new int[width]).ToArray();

            for (var g = 0; g < glyphs.Count; g++)
            {
                for (var y = 0; y < 5; y++)
                {
                    for (var x = 0; x < 3; x++)
                    {
                        if (glyphs[g][y * 3 + x] == '1')
                        {
                            rows[y + 1][g * 4 + x] = level;
                        }
                    }
                }
            }

            return rows;
        }

        private static int[][] BuildWave(int width)
        {
            var rows = Enumerable.Range(0, ContributionGrid.Days).Select(_ => new int[width]).ToArray();
            for (var x = 0; x < width; x++)
            {
                var crest = (int)Math.Round(3 + 2.6 * Math.Sin(x * Math.PI / 6.5));
                for (var y = 0; y < ContributionGrid.Days; y++)
                {
                    var distance = Math.Abs(y - crest);
                    rows[y][x] = distance switch { 0 => 4, 1 => 2, _ => 0 };
                }
            }

            return rows;
        }

        private static int[][] BuildGradient()
        {
            int[] levels = [1, 1, 2, 2, 3, 3, 4, 4, 3, 3, 2, 2, 1, 1];
            return Enumerable.Range(0, ContributionGrid.Days).Select(_ => levels.ToArray()).ToArray();
        }
    }
}
