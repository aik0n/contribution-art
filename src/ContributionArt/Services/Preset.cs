namespace ContributionArt.Services
{
    /// <summary>A preset ready to stamp. The pattern is indexed as <c>rows[day][x]</c>.</summary>
    public sealed class Preset
    {
        private readonly string _name;
        private readonly int[][] _pattern;

        public Preset(string name, int[][] pattern)
        {
            _name = name;
            _pattern = pattern;
        }

        public string Name
        {
            get { return _name; }
        }

        public int[][] Pattern
        {
            get { return _pattern; }
        }
    }
}
