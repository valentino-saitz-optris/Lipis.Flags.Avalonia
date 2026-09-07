namespace Lipis.Flags.Avalonia;

/// <summary>
/// Controls how a neutral culture, one that names a language but no country, is resolved to a flag.
/// </summary>
/// <remarks>
/// A language is not a country: "en" is spoken in dozens of them, so picking one is an editorial
/// decision this library leaves to the consumer.
/// </remarks>
public enum NeutralCulturePolicy
{
    /// <summary>
    /// Resolve neutral cultures through CLDR's likely-subtags data, the same table
    /// <see cref="System.Globalization.CultureInfo.CreateSpecificCulture"/> consults.
    /// </summary>
    /// <remarks>
    /// Ships opinions: CLDR maps "en" to the United States and "pt" to Brazil. The answers come
    /// from the host's ICU data and move with it, apart from the ten cultures this library corrects
    /// back to CLDR. Pin the ones you care about with <see cref="CultureFlagOptions.Overrides"/>.
    /// </remarks>
    LikelySubtags,

    /// <summary>
    /// Resolve neutral cultures only when <see cref="CultureFlagOptions.Overrides"/> supplies an
    /// answer, and otherwise return <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// The conservative choice. A missing flag is visible and easy to diagnose; a confidently
    /// wrong one is neither.
    /// </remarks>
    OverridesOnly,
}
