using AngleSharp.Dom;
using System.Collections.Generic;

namespace AdvisoryMap
{
    public enum AdvisoryLevel
    {
        Invalid,
        Normal,
        Caution,
        AvoidNonEssentialTravel,
        AvoidAllTravel
    }

    public readonly record struct AdvisoryEntry(string DisplayName, string Directory, string IsoCode, AdvisoryLevel Level, DateTimeOffset LastUpdated);
}
