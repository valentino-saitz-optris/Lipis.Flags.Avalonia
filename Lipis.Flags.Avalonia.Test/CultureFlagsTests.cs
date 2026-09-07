using System.Globalization;
using Xunit;
using Xunit.Sdk;

namespace Lipis.Flags.Avalonia.Test;

/// <summary>
/// Protects the culture-to-flag mapping, which is where every subtle mistake in this library lives.
/// </summary>
/// <remarks>
/// <para>
/// The expectations here that come from CLDR are opinions held by the host's ICU, not laws, apart
/// from the ten cultures the library corrects back to CLDR itself. They are pinned deliberately: a
/// silent change in what "pt" or "ca" resolves to is precisely the kind of drift a consumer would
/// want a failing build to tell them about.
/// </para>
/// </remarks>
public class CultureFlagsTests
{
    /// <summary>
    /// The regression table for neutral cultures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What <c>new RegionInfo(neutralName)</c> returns instead, and why every one of these rows
    /// exists: <c>af</c> (Afrikaans) becomes Afghanistan, <c>ar</c> (Arabic) becomes Argentina,
    /// <c>sv</c> (Swedish) becomes El Salvador, <c>cs</c> (Czech) becomes Serbia, <c>sl</c>
    /// (Slovene) becomes Sierra Leone, <c>sr</c> (Serbian) becomes Suriname, <c>ca</c> (Catalan)
    /// becomes Canada, <c>la</c> (Latin) becomes Laos and <c>pt</c> becomes Portugal rather than
    /// CLDR's Brazil. It is right for <c>de</c> and <c>es</c> only by coincidence, the language and
    /// the country sharing a code, which is exactly why the bug survives a spot-check on a German
    /// or Spanish machine. For <c>en</c>, <c>zh</c> and <c>ja</c> it does not answer at all: it
    /// throws.
    /// </para>
    /// <para>
    /// <c>ar</c>, <c>la</c> and <c>yi</c> are rows this runtime gets wrong too: it expands
    /// <c>ar</c> to Saudi Arabia and leaves the other two on the 001 "World" macro-region, where
    /// CLDR's likelySubtags.xml says Egypt, the Vatican and Ukraine. The library corrects those,
    /// so these three pin the correction rather than the host's ICU.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("af", "za")]
    [InlineData("ar", "eg")]
    [InlineData("sv", "se")]
    [InlineData("cs", "cz")]
    [InlineData("sl", "si")]
    [InlineData("sr", "rs")]
    [InlineData("ca", "es")]
    [InlineData("la", "va")]
    [InlineData("yi", "ua")]
    [InlineData("de", "de")]
    [InlineData("en", "us")]
    [InlineData("zh", "cn")]
    [InlineData("ja", "jp")]
    [InlineData("pt", "br")]
    [InlineData("es", "es")]
    public void GetFlagCode_ForANeutralCulture_UsesLikelySubtagsNotRegionInfo(string cultureName, string expected)
    {
        var culture = new CultureInfo(cultureName);

        Assert.True(culture.IsNeutralCulture);
        Assert.Equal(expected, culture.GetFlagCode());
    }

    [Theory]
    [InlineData("en-GB", "gb")]
    [InlineData("en-US", "us")]
    [InlineData("de-AT", "at")]
    [InlineData("de-CH", "ch")]
    [InlineData("zh-TW", "tw")]
    [InlineData("pt-PT", "pt")]
    public void GetFlagCode_ForASpecificCulture_UsesItsOwnCountry(string cultureName, string expected)
        => Assert.Equal(expected, new CultureInfo(cultureName).GetFlagCode());

    [Fact]
    public void GetFlagCode_ForANullCulture_ReturnsNull() => Assert.Null(((CultureInfo?)null).GetFlagCode());

    /// <remarks>
    /// The invariant culture is the trap in this group. Its <see cref="CultureInfo.IsNeutralCulture"/>
    /// is <see langword="false"/>, so a guard written against that property waves it straight
    /// through to <see cref="RegionInfo"/>; only the empty <see cref="CultureInfo.Name"/> actually
    /// identifies it.
    /// </remarks>
    [Fact]
    public void GetFlagCode_ForTheInvariantCulture_ReturnsNull()
    {
        Assert.False(CultureInfo.InvariantCulture.IsNeutralCulture);
        Assert.Empty(CultureInfo.InvariantCulture.Name);
        Assert.Null(CultureInfo.InvariantCulture.GetFlagCode());
    }

    /// <remarks>
    /// These expand to something shaped like a country but three characters long, or to nothing at
    /// all. <c>es-419</c> carries the UN M.49 code for Latin America, and <c>ckb</c> (Central
    /// Kurdish) and <c>sat</c> (Santali) expand to the invariant culture without raising anything.
    /// Left unfiltered they would be returned as flag codes that no asset backs.
    /// </remarks>
    [Theory]
    [InlineData("es-419")]
    [InlineData("ckb")]
    [InlineData("sat")]
    public void GetFlagCode_ForAMacroRegionOrUnexpandableCulture_ReturnsNull(string cultureName)
        => Assert.Null(new CultureInfo(cultureName).GetFlagCode());

    [Fact]
    public void GetFlagCode_UnderOverridesOnly_RefusesToGuessAtNeutralCultures()
    {
        var options = new CultureFlagOptions { NeutralCulturePolicy = NeutralCulturePolicy.OverridesOnly };

        Assert.Null(new CultureInfo("en").GetFlagCode(options));

        // A specific culture carries its own country and never reaches the neutral step, so the
        // policy must leave it alone.
        Assert.Equal("us", new CultureInfo("en-US").GetFlagCode(options));
    }

    [Fact]
    public void GetFlagCode_UnderOverridesOnly_StillHonoursAnOverride()
    {
        var options = new CultureFlagOptions
        {
            NeutralCulturePolicy = NeutralCulturePolicy.OverridesOnly,
            Overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "gb" },
        };

        Assert.Equal("gb", new CultureInfo("en").GetFlagCode(options));
    }

    /// <remarks>
    /// The bit a prototype got wrong. An override keyed on a language subtag is an answer for the
    /// language, not a rewrite rule for everything beneath it: a consumer who pins "en" to the
    /// Union Jack still expects an American flag next to en-US.
    /// </remarks>
    [Fact]
    public void GetFlagCode_WithALanguageOverride_DoesNotHijackSpecificCulturesUnderIt()
    {
        var options = new CultureFlagOptions
        {
            Overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "gb" },
        };

        Assert.Equal("gb", new CultureInfo("en").GetFlagCode(options));
        Assert.Equal("us", new CultureInfo("en-US").GetFlagCode(options));
        Assert.Equal("au", new CultureInfo("en-AU").GetFlagCode(options));
    }

    [Fact]
    public void GetFlagCode_WithAnExactNameOverride_WinsOverLikelySubtags()
    {
        // This runtime expands zh-Hant to Hong Kong and CLDR to Taiwan, so the library corrects it.
        // Both halves are asserted: the corrected default, and an override beating it.
        Assert.Equal("tw", new CultureInfo("zh-Hant").GetFlagCode());

        var options = new CultureFlagOptions
        {
            Overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["zh-Hant"] = "hk" },
        };

        Assert.Equal("hk", new CultureInfo("zh-Hant").GetFlagCode(options));
    }

    /// <remarks>
    /// An override is caller-supplied text and gets the same validation as everything else, so a
    /// typo falls through to the next rule instead of being returned.
    /// </remarks>
    [Fact]
    public void GetFlagCode_WithAnOverrideNamingAMissingFlag_FallsThroughInsteadOfReturningIt()
    {
        var options = new CultureFlagOptions
        {
            Overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "zzz" },
        };

        var resolved = new CultureInfo("en").GetFlagCode(options);

        Assert.NotEqual("zzz", resolved);
        Assert.Equal(new CultureInfo("en").GetFlagCode(), resolved);
    }

    [Fact]
    public void GetFlagCode_WithAFallback_UsesItOnlyWhenNothingElseResolves()
    {
        var options = new CultureFlagOptions
        {
            NeutralCulturePolicy = NeutralCulturePolicy.OverridesOnly,
            FallbackFlagCode = "xx",
        };

        Assert.Equal("xx", new CultureInfo("en").GetFlagCode(options));
        Assert.Equal("xx", ((CultureInfo?)null).GetFlagCode(options));
        Assert.Equal("xx", CultureInfo.InvariantCulture.GetFlagCode(options));

        // Resolvable cultures must never reach the fallback.
        Assert.Equal("us", new CultureInfo("en-US").GetFlagCode(options));
        Assert.Equal("de", new CultureInfo("de-DE").GetFlagCode(options));
    }

    [Fact]
    public void GetFlagCode_WithAFallbackThatHasNoFlag_ReturnsNull()
    {
        var options = new CultureFlagOptions
        {
            NeutralCulturePolicy = NeutralCulturePolicy.OverridesOnly,
            FallbackFlagCode = "zzz",
        };

        Assert.Null(new CultureInfo("en").GetFlagCode(options));
        Assert.Null(((CultureInfo?)null).GetFlagCode(options));
    }

    /// <remarks>
    /// The broad safety net. Every culture the host knows about is pushed through the resolver: no
    /// exception may escape onto a UI thread, and no answer may be a code the asset loader would
    /// then fail to find. The resolved-count floor keeps the sweep honest, since a resolver that
    /// returned <see langword="null"/> for everything would satisfy both other assertions.
    /// </remarks>
    [Fact]
    public void GetFlagCode_AcrossEveryInstalledCulture_NeverThrowsAndOnlyYieldsEmbeddedCodes()
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

        Assert.NotEmpty(cultures);

        var resolved = 0;

        foreach (var culture in cultures)
        {
            string? code;

            try
            {
                code = culture.GetFlagCode();
            }
            catch (Exception exception)
            {
                throw new XunitException(
                    $"GetFlagCode threw {exception.GetType().Name} for culture '{culture.Name}': {exception.Message}");
            }

            if (code is null)
            {
                continue;
            }

            resolved++;

            Assert.True(code.Length == 2, $"Culture '{culture.Name}' resolved to '{code}', which is not a two-letter code.");
            Assert.True(CountryData.HasFlag(code), $"Culture '{culture.Name}' resolved to '{code}', which has no embedded flag.");
        }

        Assert.True(
            resolved > cultures.Length / 2,
            $"Only {resolved} of {cultures.Length} cultures resolved to a flag; the resolver has stopped resolving.");
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("en")]
    [InlineData("es-419")]
    [InlineData("ckb")]
    [InlineData("")]
    public void TryGetFlagCode_AgreesWithGetFlagCode(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var expected = culture.GetFlagCode();

        Assert.Equal(expected is not null, culture.TryGetFlagCode(out var actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryGetFlagCode_ForANullCulture_ReturnsFalse()
    {
        Assert.False(((CultureInfo?)null).TryGetFlagCode(out var flagCode));
        Assert.Null(flagCode);
    }

    [Fact]
    public void GetFlagImage_ResolvesTheCultureAndHonoursTheAspectRatio()
    {
        var culture = new CultureInfo("de-AT");

        Assert.NotNull(culture.GetFlagImage());
        Assert.Same(FlagAssets.GetImage("at"), culture.GetFlagImage());
        Assert.Same(FlagAssets.GetImage("at", FlagAspectRatio.OneByOne), culture.GetFlagImage(FlagAspectRatio.OneByOne));
        Assert.NotSame(culture.GetFlagImage(), culture.GetFlagImage(FlagAspectRatio.OneByOne));

        // The wrong country would resolve to an image just as happily, so the negative case is
        // pinned too: a culture with no flag must produce nothing at all.
        Assert.NotSame(FlagAssets.GetImage("de"), culture.GetFlagImage());
        Assert.Null(new CultureInfo("es-419").GetFlagImage());
    }

    /// <remarks>
    /// <c>GetFlagImage</c> is a thin wrapper, and the temptation in a wrapper is to forget to pass
    /// one of its arguments on. Options reaching the resolver is asserted here rather than assumed
    /// from the code-resolution tests above, all of which call <c>GetFlagCode</c> directly.
    /// </remarks>
    [Fact]
    public void GetFlagImage_PassesTheOptionsThrough()
    {
        Assert.Same(FlagAssets.GetImage("us"), new CultureInfo("en").GetFlagImage());

        var conservative = new CultureFlagOptions { NeutralCulturePolicy = NeutralCulturePolicy.OverridesOnly };

        Assert.Null(new CultureInfo("en").GetFlagImage(options: conservative));

        var pinned = new CultureFlagOptions
        {
            Overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "gb" },
        };

        Assert.Same(FlagAssets.GetImage("gb"), new CultureInfo("en").GetFlagImage(options: pinned));
        Assert.Same(
            FlagAssets.GetImage("gb", FlagAspectRatio.OneByOne),
            new CultureInfo("en").GetFlagImage(FlagAspectRatio.OneByOne, pinned));
    }

    [Fact]
    public void GetFlagImage_ForACultureWithNoFlag_ReturnsNull()
    {
        Assert.Null(((CultureInfo?)null).GetFlagImage());
        Assert.Null(CultureInfo.InvariantCulture.GetFlagImage());
        Assert.Null(new CultureInfo("ckb").GetFlagImage(FlagAspectRatio.OneByOne));
    }
}
