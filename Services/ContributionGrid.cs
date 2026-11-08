namespace ContributionArt.Services
{
    /// <summary>
    /// The drawing canvas. It mirrors GitHub's contribution calendar exactly:
    /// 53 week-columns (Sunday at the top) ending with the week that contains today.
    /// Each cell holds an intensity level from 0 (empty) to 4 (darkest).
    /// </summary>
    public sealed class ContributionGrid
    {
        public const int Weeks = 53;
        public const int Days = 7;
        public const int MaxLevel = 4;

        private readonly int[,] _levels = new int[Weeks, Days];

        public ContributionGrid(DateOnly today)
        {
            Today = today;
            var yearAgo = today.AddDays(-364);
            StartDate = yearAgo.AddDays(-(int)yearAgo.DayOfWeek); // back up to Sunday
        }

        public DateOnly Today { get; }

        /// <summary>The Sunday shown in the top-left cell.</summary>
        public DateOnly StartDate { get; }

        public DateOnly DateAt(int week, int day) => StartDate.AddDays(week * 7 + day);

        public bool IsFuture(int week, int day) => DateAt(week, day) > Today;

        public int Get(int week, int day) => _levels[week, day];

        public void Set(int week, int day, int level)
        {
            if (week is < 0 or >= Weeks || day is < 0 or >= Days || IsFuture(week, day))
            {
                return;
            }

            _levels[week, day] = Math.Clamp(level, 0, MaxLevel);
        }

        public void Clear() => Array.Clear(_levels);

        /// <summary>Copies a 7-row pattern onto the grid, starting at the given week column.</summary>
        public void Stamp(int[][] rows, int weekOffset, bool skipZeros = false)
        {
            for (var day = 0; day < Math.Min(rows.Length, Days); day++)
            {
                for (var x = 0; x < rows[day].Length; x++)
                {
                    var value = rows[day][x];
                    if (skipZeros && value == 0)
                    {
                        continue;
                    }

                    Set(weekOffset + x, day, value);
                }
            }
        }

        /// <summary>All painted cells, oldest first.</summary>
        public IEnumerable<(DateOnly Date, int Level)> Pixels()
        {
            for (var week = 0; week < Weeks; week++)
            {
                for (var day = 0; day < Days; day++)
                {
                    if (_levels[week, day] > 0)
                    {
                        yield return (DateAt(week, day), _levels[week, day]);
                    }
                }
            }
        }
    }
}
