using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Svg;

namespace Lipis.Flags.Avalonia;

/// <summary>
/// Maps a <see cref="CultureInfo"/> to the flag of the country it belongs to.
/// </summary>
/// <remarks>
/// Never constructs a <see cref="RegionInfo"/> from a neutral culture name. <see cref="RegionInfo"/>
/// accepts a bare language subtag and reinterprets it as a country code, so "af" (Afrikaans)
/// resolves to Afghanistan and "ca" (Catalan) to Canada. Neutral cultures go through
/// <see cref="CultureFlagOptions.Overrides"/> and the configured <see cref="NeutralCulturePolicy"/>
/// instead.
/// </remarks>
public static class CultureFlags
{
    /// <summary>
    /// Resolves the flag code for a culture.
    /// </summary>
    /// <param name="culture">The culture to resolve.</param>
    /// <param name="options">Resolution options, or <see langword="null"/> for <see cref="CultureFlagOptions.Default"/>.</param>
    /// <returns>A lower-cased flag code with an embedded asset, or <see langword="null"/> if none could be resolved.</returns>
    public static string? GetFlagCode(this CultureInfo? culture, CultureFlagOptions? options = null)
    {
        options ??= CultureFlagOptions.Default;

        var resolved = Resolve(culture, options);

        return resolved ?? Validate(options.FallbackFlagCode);
    }

    /// <summary>
    /// Resolves the flag code for a culture.
    /// </summary>
    /// <param name="culture">The culture to resolve.</param>
    /// <param name="flagCode">The resolved flag code, or <see langword="null"/>.</param>
    /// <param name="options">Resolution options, or <see langword="null"/> for <see cref="CultureFlagOptions.Default"/>.</param>
    /// <returns><see langword="true"/> if a flag code was resolved; otherwise <see langword="false"/>.</returns>
    public static bool TryGetFlagCode(
        this CultureInfo? culture,
        [NotNullWhen(true)] out string? flagCode,
        CultureFlagOptions? options = null)
    {
        flagCode = GetFlagCode(culture, options);

        return flagCode is not null;
    }

    /// <summary>
    /// Resolves a culture directly to its flag, ready to bind to an image.
    /// </summary>
    /// <param name="culture">The culture to resolve.</param>
    /// <param name="aspectRatio">The aspect ratio to resolve. Defaults to 4:3.</param>
    /// <param name="options">Resolution options, or <see langword="null"/> for <see cref="CultureFlagOptions.Default"/>.</param>
    /// <returns>The flag, or <see langword="null"/> if no flag could be resolved.</returns>
    public static SvgImage? GetFlagImage(
        this CultureInfo? culture,
        FlagAspectRatio aspectRatio = FlagAspectRatio.FourByThree,
        CultureFlagOptions? options = null)
        => FlagAssets.GetImage(GetFlagCode(culture, options), aspectRatio);

    // Neutral cultures where this runtime disagrees with CLDR's likelySubtags.xml, which is what
    // LikelySubtags promises. Four of them (la, pap, prg, yi) otherwise expand to a UN M.49
    // macro-region, which has no flag at all.
    private static readonly Dictionary<string, string> CldrCorrections = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ar"] = "eg",
        ["la"] = "va",
        ["pap"] = "cw",
        ["prg"] = "pl",
        ["sw"] = "tz",
        ["syr"] = "iq",
        ["ti"] = "et",
        ["tzm"] = "ma",
        ["yi"] = "ua",
        ["zh-Hant"] = "tw",
    };

    private static string? Resolve(CultureInfo? culture, CultureFlagOptions options)
    {
        // The invariant culture has an empty name and no country, and is not reported as neutral,
        // so a check on IsNeutralCulture misses it; the name length is the only reliable test.
        if (culture is null || culture.Name.Length == 0)
        {
            return null;
        }

        if (TryOverride(options, culture.Name) is { } exact)
        {
            return exact;
        }

        // Returning here is what keeps a language override ("en" -> "gb") from hijacking en-US:
        // a specific culture carries its own country and never reaches the neutral step below.
        if (!culture.IsNeutralCulture)
        {
            return ResolveSpecific(culture);
        }

        if (TryOverride(options, culture.TwoLetterISOLanguageName) is { } byLanguage)
        {
            return byLanguage;
        }

        if (options.NeutralCulturePolicy != NeutralCulturePolicy.LikelySubtags)
        {
            return null;
        }

        return (CldrCorrections.TryGetValue(culture.Name, out var corrected) ? Validate(corrected) : null)
               ?? ResolveViaLikelySubtags(culture);
    }

    private static string? ResolveSpecific(CultureInfo culture)
    {
        try
        {
            return Validate(new RegionInfo(culture.Name).TwoLetterISORegionName);
        }
        catch (ArgumentException)
        {
            // Custom, pseudo and otherwise unmappable cultures. There is no flag to show.
            return null;
        }
    }

    // Expands a neutral culture to a specific one via CLDR likely-subtags, then reads its country.
    // CreateSpecificCulture fails in two ways that look like success: it returns the invariant
    // culture for neutral cultures CLDR cannot expand, and it maps others onto three-character UN
    // M.49 macro-region codes such as 001 (World) and 419 (Latin America). Both are filtered out
    // here rather than returned as flag codes.
    private static string? ResolveViaLikelySubtags(CultureInfo culture)
    {
        try
        {
            var specific = CultureInfo.CreateSpecificCulture(culture.Name);

            return specific.Name.Length == 0 ? null : ResolveSpecific(specific);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? TryOverride(CultureFlagOptions options, string key)
        => options.Overrides is { } overrides && overrides.TryGetValue(key, out var code)
            ? Validate(code)
            : null;

    // Accepts a candidate only if a flag is actually embedded for it, so a three-character
    // macro-region code, a typo in an override or a country with no artwork resolves to nothing
    // rather than to a code no asset backs.
    private static string? Validate(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = code.Trim().ToLowerInvariant();

        return CountryData.HasFlag(normalized) ? normalized : null;
    }
}
