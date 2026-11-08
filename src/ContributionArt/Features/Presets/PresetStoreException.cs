namespace ContributionArt.Features.Presets
{
    /// <summary>A preset could not be read or saved. The message is written to be shown to the user.</summary>
    public sealed class PresetStoreException : Exception
    {
        public PresetStoreException()
        {
        }

        public PresetStoreException(string message)
            : base(message)
        {
        }

        public PresetStoreException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
