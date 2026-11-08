using Xunit.v3;

namespace ContributionArt.Tests
{
    /// <summary>Marks a test class as a unit test, for filtering with <c>Category=Unit</c>.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnitTestTraitAttribute : Attribute, ITraitAttribute
    {
        public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        {
            return [new KeyValuePair<string, string>("Category", "Unit")];
        }
    }
}
