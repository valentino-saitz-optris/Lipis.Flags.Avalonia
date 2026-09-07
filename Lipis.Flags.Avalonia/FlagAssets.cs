using System.Collections.Concurrent;
using Avalonia.Platform;
using Avalonia.Svg;

namespace Lipis.Flags.Avalonia;

/// <summary>
/// Resolves ISO 3166-1 alpha-2 country codes to the flag artwork embedded in this assembly.
/// </summary>
public static class FlagAssets
{
    private const string AssemblyName = "Lipis.Flags.Avalonia";

    // Parsing an SVG costs far more than locating it, and a converter runs once per container
    // realisation, so a scrolling list re-requests the same flag constantly. A null value caches
    // the "no such flag" answer too.
    private static readonly ConcurrentDictionary<(string Code, FlagAspectRatio AspectRatio), SvgImage?> Cache = new();

    /// <summary>
    /// Gets a value indicating whether a flag exists for the supplied country code.
    /// </summary>
    /// <param name="countryId">A country code. Case and surrounding whitespace are ignored.</param>
    public static bool Exists(string? countryId) => Normalize(countryId) is not null;

    /// <summary>
    /// Gets a country's flag, ready to bind to an image.
    /// </summary>
    /// <param name="countryId">A country code. Case and surrounding whitespace are ignored.</param>
    /// <param name="aspectRatio">The aspect ratio to resolve. Defaults to 4:3.</param>
    /// <returns>The flag, or <see langword="null"/> if no flag exists for the code.</returns>
    public static SvgImage? GetImage(string? countryId, FlagAspectRatio aspectRatio = FlagAspectRatio.FourByThree)
    {
        var code = Normalize(countryId);

        return code is null ? null : Cache.GetOrAdd((code, aspectRatio), static key => Load(key.Code, key.AspectRatio));
    }

    private static SvgImage? Load(string code, FlagAspectRatio aspectRatio)
    {
        var source = SvgSource.Load(PathFor(code, aspectRatio), null);

        return source is null ? null : new SvgImage { Source = source };
    }

    private static string PathFor(string code, FlagAspectRatio aspectRatio)
        => $"avares://{AssemblyName}/Assets/{FolderFor(aspectRatio)}/{code}.svg";

    /// <summary>
    /// Gets the folder name corresponding to the aspect ratio. (e.g., "4x3" or "1x1")
    /// </summary>
    internal static string FolderFor(FlagAspectRatio aspectRatio) => aspectRatio switch
    {
        FlagAspectRatio.OneByOne => "1x1",
        _ => "4x3",
    };

    // ToLowerInvariant rather than ToLower: under a Turkish or Azerbaijani current culture,
    // "IT".ToLower() yields a dotless "it", which matches no asset. Roughly one code in twelve
    // contains an I.
    private static string? Normalize(string? countryId)
    {
        if (string.IsNullOrWhiteSpace(countryId))
        {
            return null;
        }

        var code = countryId.Trim().ToLowerInvariant();

        return AssetLoader.Exists(new Uri(PathFor(code, FlagAspectRatio.FourByThree), UriKind.Absolute)) ? code : null;
    }
}
