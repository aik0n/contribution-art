namespace ContributionArt.Features.Presets
{
    /// <summary>Holds the predefined presets and the presets saved by the user.</summary>
    public interface IPresetStore
    {
        /// <summary>All presets, predefined ones first.</summary>
        IReadOnlyList<Preset> Presets { get; }

        /// <summary>Reads the user file again. On failure it throws and keeps the current presets.</summary>
        /// <exception cref="PresetStoreException">The user file is invalid or cannot be read.</exception>
        void ReloadUserPresets();

        /// <summary>Adds a preset to the user file and reads the file back.</summary>
        /// <exception cref="PresetStoreException">The preset is invalid, its name is taken, or the file cannot be read or written.</exception>
        void SaveUserPreset(string name, int[][] pattern);
    }
}
