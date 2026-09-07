using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Lipis.Flags.Avalonia.Test;

/// <summary>
/// Shared fixtures for the asset tests: the exhaustive code sweep, the bytes behind one flag, and
/// the location of the vendored artwork on disk.
/// </summary>
public static class FlagTestData
{
    /// <summary>
    /// Every code the generated table claims to have a flag for.
    /// </summary>
    public static TheoryData<string> FlagCodes
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var code in CountryData.AllFlagCodes)
            {
                data.Add(code);
            }

            return data;
        }
    }

    /// <summary>
    /// Both aspect ratios, in a fixed order, for tests that sweep them without a theory.
    /// </summary>
    public static IReadOnlyList<FlagAspectRatio> AspectRatios { get; } =
        [FlagAspectRatio.FourByThree, FlagAspectRatio.OneByOne];

    /// <summary>
    /// Every (code, aspect ratio) pair the library claims to serve.
    /// </summary>
    public static IReadOnlyList<(string Code, FlagAspectRatio AspectRatio)> EveryFlag { get; } =
        CountryData.AllFlagCodes
            .SelectMany(code => AspectRatios.Select(aspectRatio => (Code: code, AspectRatio: aspectRatio)))
            .ToArray();

    /// <summary>
    /// Reads a flag's SVG markup out of the embedded archive, asserting that it is there at all.
    /// </summary>
    public static byte[] ReadFlag(string code, FlagAspectRatio aspectRatio = FlagAspectRatio.FourByThree)
    {
        using var stream = FlagAssets.OpenSvgStream(code, aspectRatio);

        Assert.NotNull(stream);

        return Drain(stream);
    }

    /// <summary>
    /// Reads a flag without asserting, for callers that must not throw, such as worker threads.
    /// </summary>
    public static byte[]? TryReadFlag(string code, FlagAspectRatio aspectRatio)
    {
        using var stream = FlagAssets.OpenSvgStream(code, aspectRatio);

        return stream is null ? null : Drain(stream);
    }

    public static byte[] Drain(Stream stream)
    {
        var buffer = new MemoryStream();

        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>
    /// Reads the <c>viewBox</c> a flag declares, which is the shape of the artwork itself.
    /// </summary>
    /// <remarks>
    /// Parsed out of the markup rather than read off <see cref="Avalonia.Svg.SvgImage.Size"/>,
    /// which cannot be touched from a test thread. See the note on <see cref="FlagArchiveTests"/>.
    /// </remarks>
    public static (double Width, double Height) ReadViewBox(string code, FlagAspectRatio aspectRatio)
    {
        var what = $"{FolderFor(aspectRatio)}/{code}.svg";
        var markup = Encoding.UTF8.GetString(ReadFlag(code, aspectRatio));
        var match = Regex.Match(markup, "viewBox\\s*=\\s*\"(?<box>[^\"]*)\"", RegexOptions.CultureInvariant);

        Assert.True(match.Success, $"{what} declares no viewBox, so its shape cannot be checked.");

        var box = match.Groups["box"].Value.Split([' ', ',', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        Assert.True(box.Length == 4, $"{what} has a malformed viewBox: '{match.Groups["box"].Value}'.");

        var width = double.Parse(box[2], CultureInfo.InvariantCulture);
        var height = double.Parse(box[3], CultureInfo.InvariantCulture);

        Assert.True(width > 0 && height > 0, $"{what} declares a degenerate viewBox: '{match.Groups["box"].Value}'.");

        return (width, height);
    }

    /// <summary>
    /// Walks up from the test binaries to the repository root and returns the vendored asset
    /// directory for an aspect ratio.
    /// </summary>
    /// <remarks>
    /// Throws rather than returning an empty enumerable when the directory cannot be found: a
    /// silent miss would turn the orphan sweep into a test that passes on nothing.
    /// </remarks>
    public static string FindAssetDirectory(string folder)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Lipis.Flags.Avalonia", "Assets", folder);

            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate Lipis.Flags.Avalonia/Assets/{folder} above '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// The archive directory holding flags of a given aspect ratio.
    /// </summary>
    /// <remarks>
    /// Deliberately duplicated from the library's own internal switch rather than reached through
    /// <c>InternalsVisibleTo</c>. A test that imports the mapping it is checking cannot catch the
    /// two arms being swapped; one that restates it can.
    /// </remarks>
    public static string FolderFor(FlagAspectRatio aspectRatio) =>
        aspectRatio == FlagAspectRatio.OneByOne ? "1x1" : "4x3";
}
