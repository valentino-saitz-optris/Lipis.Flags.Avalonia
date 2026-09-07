using System.Collections.Concurrent;
using System.IO.Compression;
using Avalonia.Svg;

namespace Lipis.Flags.Avalonia;

/// <summary>
/// Resolves flag codes to the SVG flag artwork embedded in this assembly.
/// </summary>
/// <remarks>
/// The artwork lives in a single embedded archive and is expanded and parsed on demand, so a
/// consumer pays only for the flags it asks for. The accepted codes are
/// <see cref="CountryData.AllFlagCodes"/>, a superset of ISO 3166-1 alpha-2. See "How the flags
/// are stored" in the README.
/// </remarks>
public static class FlagAssets
{
    // Opened once and kept open: ZipArchive builds its entry index on construction, so reopening
    // per lookup would rescan it every time.
    private static readonly Lazy<ZipArchive> Archive = new(OpenArchive, LazyThreadSafetyMode.ExecutionAndPublication);

    // Parsed flags, keyed by code and aspect ratio. Parsing an SVG costs far more than
    // decompressing it, and a converter runs once per container realisation. A null value caches
    // the "no such flag" answer too.
    private static readonly ConcurrentDictionary<(string Code, FlagAspectRatio AspectRatio), SvgImage?> Cache = new();

    // Guards Archive: ZipArchive is not thread-safe and a converter can be called from more than
    // one thread. Held only across decompressing one entry, never across SVG parsing.
    private static readonly object ArchiveLock = new();

    /// <summary>
    /// Gets a value indicating whether a flag exists for the supplied country code.
    /// </summary>
    /// <param name="countryId">A flag code. Case and surrounding whitespace are ignored.</param>
    public static bool Exists(string? countryId) => Normalize(countryId) is not null;

    /// <summary>
    /// Gets a country's flag as an image ready to bind to a control.
    /// </summary>
    /// <param name="countryId">A flag code. Case and surrounding whitespace are ignored.</param>
    /// <param name="aspectRatio">The aspect ratio to resolve. Defaults to 4:3.</param>
    /// <returns>
    /// The flag, or <see langword="null"/> if no flag exists for the code. Repeated calls for the
    /// same code and aspect ratio return the same cached instance.
    /// </returns>
    public static SvgImage? GetImage(string? countryId, FlagAspectRatio aspectRatio = FlagAspectRatio.FourByThree)
    {
        var code = Normalize(countryId);

        // GetOrAdd may run the factory more than once under contention. Parsing a flag twice and
        // discarding one result is cheaper than holding a lock across parsing, and every caller
        // still gets the instance that was actually stored.
        return code is null ? null : Cache.GetOrAdd((code, aspectRatio), static key => Load(key.Code, key.AspectRatio));
    }

    /// <summary>
    /// Opens the raw SVG markup of a country's flag.
    /// </summary>
    /// <param name="countryId">A flag code. Case and surrounding whitespace are ignored.</param>
    /// <param name="aspectRatio">The aspect ratio to resolve. Defaults to 4:3.</param>
    /// <returns>A seekable stream over the decompressed SVG, or <see langword="null"/> if no flag exists for the code.</returns>
    /// <remarks>
    /// For callers that want to render the artwork themselves or write it out. The stream is a
    /// private copy; the caller owns it and should dispose it.
    /// </remarks>
    public static Stream? OpenSvgStream(string? countryId, FlagAspectRatio aspectRatio = FlagAspectRatio.FourByThree)
    {
        var code = Normalize(countryId);

        return code is null ? null : Decompress(code, aspectRatio);
    }

    private static SvgImage? Load(string code, FlagAspectRatio aspectRatio)
    {
        using var stream = Decompress(code, aspectRatio);

        if (stream is null)
        {
            return null;
        }

        var source = SvgSource.Load(stream);

        return source is null ? null : new SvgImage { Source = source };
    }

    // Expands one archive entry into memory. Copied out in full rather than handed back as a
    // deflate stream, because an SVG parser wants to seek and the caller may hold it for as long as
    // it likes.
    private static MemoryStream? Decompress(string code, FlagAspectRatio aspectRatio)
    {
        var name = $"{FolderFor(aspectRatio)}/{code}.svg";

        lock (ArchiveLock)
        {
            var entry = Archive.Value.GetEntry(name);

            if (entry is null)
            {
                return null;
            }

            var buffer = new MemoryStream((int)entry.Length);

            using (var source = entry.Open())
            {
                source.CopyTo(buffer);
            }

            buffer.Position = 0;

            return buffer;
        }
    }

    private static ZipArchive OpenArchive()
    {
        var stream = typeof(FlagAssets).Assembly.GetManifestResourceStream(ResourceName)
                     ?? throw new InvalidOperationException(
                         $"The embedded flag archive '{ResourceName}' is missing from " +
                         $"{typeof(FlagAssets).Assembly.GetName().Name}. This assembly was built without its assets.");

        return new ZipArchive(stream, ZipArchiveMode.Read);
    }

    // The manifest resource holding the flag archive. Written by the build; see the project file.
    private const string ResourceName = "flags.zip";

    // Gets the folder name corresponding to the aspect ratio. (e.g., "4x3" or "1x1")
    internal static string FolderFor(FlagAspectRatio aspectRatio) => aspectRatio switch
    {
        FlagAspectRatio.OneByOne => "1x1",
        _ => "4x3",
    };

    // ToLowerInvariant, not ToLower: under a Turkish or Azerbaijani culture "IT".ToLower() is a
    // dotless "ıt", which silently matches no flag. 22 of the 271 codes contain an I.
    private static string? Normalize(string? countryId)
    {
        if (string.IsNullOrWhiteSpace(countryId))
        {
            return null;
        }

        var code = countryId.Trim().ToLowerInvariant();

        return CountryData.HasFlag(code) ? code : null;
    }
}
