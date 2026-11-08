namespace ContributionArt.Features.Presets
{
    /// <summary>Preset settings, bound from the <c>Presets</c> configuration section.</summary>
    public sealed class PresetOptions
    {
        public const string Presets = nameof(Presets);

        public const string PredefinedFileName = "presets.json";

        /// <summary>The presets shipped with the application, read from <c>presets.json</c>.</summary>
        public List<PresetDefinition> Predefined { get; set; } = [];

        /// <summary>Path of the file with saved presets. When empty, a file in the user's local application data is used.</summary>
        public string UserFilePath { get; set; } = "";
    }
}
