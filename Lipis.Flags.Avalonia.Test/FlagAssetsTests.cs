using Xunit;

namespace Lipis.Flags.Avalonia.Test;

/// <summary>
/// Protects the asset lookup surface in both directions: every code the generated table advertises
/// must resolve to artwork in both aspect ratios, and every SVG actually vendored under
/// <c>Assets</c> must be advertised by the table.
/// </summary>
/// <remarks>
/// <para>
/// Checking a single direction is not enough. A sweep driven by the table alone passes when the
/// asset folder has grown flags nobody can reach; a sweep driven by the folder alone passes when
/// the table has grown codes with no artwork behind them. Both drift modes end the same way at
/// runtime, as a control bound to a flag that renders nothing.
/// </para>
/// </remarks>
public class FlagAssetsTests
{
    /// <summary>
    /// A floor, not a description. Every exhaustive assertion here is driven by the generated table
    /// or by the asset folder, so an empty table would produce an empty sweep, agree with an
    /// equally empty scan, and pass having checked nothing. Pinned at today's counts, so an
    /// upstream release that retires a flag fails here by design and these are what to lower.
    /// </summary>
    private const int MinimumFlagCodeCount = 271;

    /// <summary>
    /// The same floor for the named-country table.
    /// </summary>
    private const int MinimumCountryIdCount = 249;

    /// <summary>
    /// Every code the generated table claims to have a flag for.
    /// </summary>
    public static TheoryData<string> FlagCodes => FlagTestData.FlagCodes;

    [Theory]
    [MemberData(nameof(FlagCodes))]
    public void Resolve_ForEveryAdvertisedFlagCode_YieldsArtworkInBothAspectRatios(string code)
    {
        foreach (var aspectRatio in FlagTestData.AspectRatios)
        {
            Assert.NotNull(FlagAssets.GetImage(code, aspectRatio));

            using var stream = FlagAssets.OpenSvgStream(code, aspectRatio);

            Assert.NotNull(stream);
            Assert.True(stream.Length > 0, $"'{code}' in {aspectRatio} decompressed to an empty stream.");
        }
    }

    [Theory]
    [MemberData(nameof(FlagCodes))]
    public void Exists_ForEveryAdvertisedFlagCode_ReturnsTrue(string code) => Assert.True(FlagAssets.Exists(code));

    /// <summary>
    /// The two aspect ratios must return genuinely different artwork.
    /// </summary>
    /// <remarks>
    /// This is what the old <c>EndsWith("/1x1/de.svg")</c> assertion was really guarding: that the
    /// enum member reaches the shape it names. With the path gone the only honest way to say so is
    /// to compare the markup, and every one of the 271 flags does differ between its 4:3 and its
    /// square rendering, so the assertion is meaningful for the whole sweep rather than for a
    /// hand-picked few.
    /// </remarks>
    [Theory]
    [MemberData(nameof(FlagCodes))]
    public void OpenSvgStream_ForTheTwoAspectRatios_ReturnsDifferentArtwork(string code)
    {
        var fourByThree = FlagTestData.ReadFlag(code, FlagAspectRatio.FourByThree);
        var oneByOne = FlagTestData.ReadFlag(code, FlagAspectRatio.OneByOne);

        Assert.NotEmpty(fourByThree);
        Assert.NotEmpty(oneByOne);

        Assert.False(
            fourByThree.AsSpan().SequenceEqual(oneByOne),
            $"The 4:3 and 1:1 artwork for '{code}' is byte-identical; the aspect ratio is being ignored.");
    }

    /// <summary>
    /// And the artwork each ratio returns must actually have that shape.
    /// </summary>
    /// <remarks>
    /// Differing bytes alone would still be satisfied by a folder switch with its arms swapped, so
    /// the shape is read out of the <c>viewBox</c>: the ratio, not the numbers, in case upstream
    /// re-exports at a different resolution. Read from the markup and not from
    /// <c>SvgImage.Size</c>; see the note on <see cref="FlagArchiveTests"/>.
    /// </remarks>
    [Theory]
    [MemberData(nameof(FlagCodes))]
    public void OpenSvgStream_ForEachAspectRatio_ReturnsArtworkOfThatShape(string code)
    {
        var fourByThree = FlagTestData.ReadViewBox(code, FlagAspectRatio.FourByThree);
        var oneByOne = FlagTestData.ReadViewBox(code, FlagAspectRatio.OneByOne);

        Assert.Equal(4d / 3d, fourByThree.Width / fourByThree.Height, 3);
        Assert.Equal(1d, oneByOne.Width / oneByOne.Height, 3);
    }

    /// <remarks>
    /// The vendored files are the input to the build's <c>ZipDirectory</c> step, so scanning them
    /// checks the table against the artwork as committed. What actually made it into the archive
    /// is a separate question, asked in <see cref="FlagArchiveTests"/>.
    /// </remarks>
    [Theory]
    [InlineData(FlagAspectRatio.FourByThree, "4x3")]
    [InlineData(FlagAspectRatio.OneByOne, "1x1")]
    public void VendoredAssets_ForEachAspectRatio_MatchTheGeneratedTableExactly(
        FlagAspectRatio aspectRatio,
        string folder)
    {
        var directory = FlagTestData.FindAssetDirectory(folder);

        var onDisk = Directory
            .EnumerateFiles(directory, "*.svg")
            .Select(file => Path.GetFileNameWithoutExtension(file)!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(
            onDisk.Count >= MinimumFlagCodeCount,
            $"Only {onDisk.Count} SVG files found under Assets/{folder}; expected at least {MinimumFlagCodeCount}.");

        var orphans = onDisk.Where(name => !CountryData.HasFlag(name)).Order(StringComparer.Ordinal).ToArray();

        Assert.True(
            orphans.Length == 0,
            $"Assets/{folder} contains flags no code reaches: {string.Join(", ", orphans)}.");

        var missing = CountryData.AllFlagCodes.Where(code => !onDisk.Contains(code)).Order(StringComparer.Ordinal).ToArray();

        Assert.True(
            missing.Length == 0,
            $"The generated table advertises codes with no artwork under Assets/{folder}: {string.Join(", ", missing)}.");

        // Ties the enum member to the folder that was just verified, so a swapped arm in the
        // library's folder switch cannot leave both sweeps above green. Byte equality rather than
        // a path suffix, because the caller can no longer see a path.
        Assert.Equal(
            File.ReadAllBytes(Path.Combine(directory, "de.svg")),
            FlagTestData.ReadFlag("de", aspectRatio));
    }

    [Fact]
    public void GeneratedTables_AreAtLeastAsLargeAsTheKnownFloor()
    {
        Assert.True(
            CountryData.AllFlagCodes.Count() >= MinimumFlagCodeCount,
            $"Expected at least {MinimumFlagCodeCount} flag codes but the generated table has {CountryData.AllFlagCodes.Count()}.");

        Assert.True(
            CountryData.AllCountryIds.Count() >= MinimumCountryIdCount,
            $"Expected at least {MinimumCountryIdCount} country ids but the generated table has {CountryData.AllCountryIds.Count()}.");
    }

    [Theory]
    [InlineData("zz")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("toolong")]
    [InlineData("1")]
    public void Resolve_ForAnUnrecognisedCode_ReturnsNullRatherThanFailingLater(string? countryId)
    {
        Assert.Null(FlagAssets.GetImage(countryId));
        Assert.Null(FlagAssets.GetImage(countryId, FlagAspectRatio.OneByOne));
        Assert.Null(FlagAssets.OpenSvgStream(countryId));
        Assert.Null(FlagAssets.OpenSvgStream(countryId, FlagAspectRatio.OneByOne));
        Assert.False(FlagAssets.Exists(countryId));
    }

    [Theory]
    [InlineData("DE")]
    [InlineData("de")]
    [InlineData(" De ")]
    [InlineData("dE")]
    public void Resolve_IgnoresCaseAndSurroundingWhitespace(string countryId)
    {
        Assert.True(FlagAssets.Exists(countryId));
        Assert.Same(FlagAssets.GetImage("de"), FlagAssets.GetImage(countryId));
        Assert.Equal(FlagTestData.ReadFlag("de"), FlagTestData.ReadFlag(countryId));
    }

    /// <remarks>
    /// These eight had artwork all along but were absent from the hand-maintained country table
    /// that predated the generator, so every one of them rendered as an empty box.
    /// </remarks>
    [Theory]
    [InlineData("aq")]
    [InlineData("bl")]
    [InlineData("bq")]
    [InlineData("im")]
    [InlineData("je")]
    [InlineData("mf")]
    [InlineData("ss")]
    [InlineData("sx")]
    public void Resolve_ForCodesMissingFromTheOldHandTable_YieldsArtwork(string code)
    {
        Assert.True(FlagAssets.Exists(code));
        Assert.NotNull(FlagAssets.GetImage(code));
        Assert.NotEmpty(FlagTestData.ReadFlag(code));
    }

    /// <remarks>
    /// flag-icons ships flags for entities that ISO 3166-1 does not list. They have no country
    /// name, so a lookup routed through the country table would drop them; <see cref="FlagAssets"/>
    /// deliberately validates against the flag set instead.
    /// </remarks>
    [Theory]
    [InlineData("eu")]
    [InlineData("un")]
    [InlineData("xk")]
    [InlineData("gb-eng")]
    [InlineData("gb-sct")]
    [InlineData("gb-wls")]
    [InlineData("es-ct")]
    public void Resolve_ForNonIsoEntities_YieldsArtworkInBothAspectRatios(string code)
    {
        Assert.True(FlagAssets.Exists(code));

        foreach (var aspectRatio in FlagTestData.AspectRatios)
        {
            Assert.NotNull(FlagAssets.GetImage(code, aspectRatio));
            Assert.NotEmpty(FlagTestData.ReadFlag(code, aspectRatio));
        }
    }
}
