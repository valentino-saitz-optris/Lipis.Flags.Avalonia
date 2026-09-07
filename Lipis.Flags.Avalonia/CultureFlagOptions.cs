namespace Lipis.Flags.Avalonia;

/// <summary>
/// Tunes how <see cref="CultureFlags"/> maps a culture to a flag.
/// </summary>
public sealed class CultureFlagOptions
{
    /// <summary>
    /// The options used when a caller does not supply any.
    /// </summary>
    public static CultureFlagOptions Default { get; } = new();

    /// <summary>
    /// How to resolve a culture that names a language but no country. Defaults to
    /// <see cref="NeutralCulturePolicy.LikelySubtags"/>.
    /// </summary>
    public NeutralCulturePolicy NeutralCulturePolicy { get; init; } = NeutralCulturePolicy.LikelySubtags;

    /// <summary>
    /// Culture-to-flag answers that win over every other rule.
    /// </summary>
    /// <remarks>
    /// Keyed by culture name, matched case-insensitively. Both full names and language subtags
    /// work: <c>"zh-Hant"</c> matches only that culture, <c>"zh"</c> matches any neutral <c>zh</c>
    /// culture. A language entry does not hijack the specific cultures under it: with
    /// <c>"en" -&gt; "gb"</c>, <c>en-US</c> still resolves to <c>us</c>, because it carries its own
    /// country.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? Overrides
    {
        get => _overrides;
        init => _overrides = Snapshot(value);
    }

    private readonly IReadOnlyDictionary<string, string>? _overrides;

    // Copies overrides into a case-insensitive dictionary. A caller's plain Dictionary{TKey,TValue}
    // compares ordinally, so an entry written "zh-hant" would silently miss the culture zh-Hant.
    // The copy also keeps these options immutable.
    private static IReadOnlyDictionary<string, string>? Snapshot(IReadOnlyDictionary<string, string>? source)
    {
        if (source is null)
        {
            return null;
        }

        var copy = new Dictionary<string, string>(source.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    /// <summary>
    /// A flag code to fall back to when nothing else resolves, or <see langword="null"/> to
    /// return no flag at all.
    /// </summary>
    /// <remarks>
    /// flag-icons ships an <c>xx</c> placeholder that suits this purpose.
    /// </remarks>
    public string? FallbackFlagCode { get; init; }
}
