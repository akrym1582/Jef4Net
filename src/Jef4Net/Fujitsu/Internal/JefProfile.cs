namespace Jef4Net.Fujitsu.Internal;

/// <summary>Identifies the available JEF mapping profiles.</summary>
internal enum JefProfile
{
    /// <summary>The standard JEF mapping.</summary>
    Normal,

    /// <summary>The reversible JEF mapping.</summary>
    Roundtrip,

    /// <summary>The Hanyo Denshi JEF mapping.</summary>
    HanyoDenshi,
}
