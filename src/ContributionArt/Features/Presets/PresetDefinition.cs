namespace ContributionArt.Features.Presets
{
    /// <summary>A preset as stored in JSON. Each row is one day, and each digit is the level for one week.</summary>
    public sealed class PresetDefinition
    {
        public string Name { get; set; } = "";

        public string[] Rows { get; set; } = [];
    }
}
