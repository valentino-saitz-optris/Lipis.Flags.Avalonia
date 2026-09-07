using System.Globalization;
using Xunit;

namespace Lipis.Flags.Avalonia.Test;

/// <summary>
/// Guards the invariant lower-casing in <see cref="FlagAssets"/> against the Turkish-I regression.
/// </summary>
/// <remarks>
/// <para>
/// Turkish and Azerbaijani have a dotless lower-case i. Under either culture, culture-sensitive
/// <see cref="string.ToLower()"/> maps <c>'I'</c> to <c>'ı'</c>, so <c>"IT"</c> becomes
/// <c>"ıt"</c> and matches no asset. Roughly one flag code in twelve carries an <c>I</c>, so
/// the failure is wide but invisible to anyone not running a Turkic locale, which is exactly why
/// it survives review.
/// </para>
/// <para>
/// This is deliberately an ordinary unit test in the ordinary test job. It must not be moved into
/// an AOT-only or published-binary job: NativeAOT is commonly published with
/// <c>InvariantGlobalization</c>, where <see cref="CultureInfo.CurrentCulture"/> is the invariant
/// culture, <c>new CultureInfo("tr-TR")</c> yields an invariant-behaving culture, and the bug
/// cannot reproduce at all. The suite would go green while testing nothing.
/// </para>
/// </remarks>
public class TurkishLocaleTests
{
    /// <summary>
    /// Every flag code containing a dotted I, paired with each Turkic culture, derived from the
    /// generated table rather than transcribed, so a newly added code is covered automatically.
    /// </summary>
    public static TheoryData<string, string> DottedCodesUnderTurkicCultures
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (var cultureName in TurkicCultureNames)
            {
                foreach (var code in CountryData.AllFlagCodes.Where(code => code.Contains('i', StringComparison.OrdinalIgnoreCase)))
                {
                    data.Add(cultureName, code);
                }
            }

            return data;
        }
    }

    private static readonly string[] TurkicCultureNames = ["tr-TR", "az-AZ"];

    [Theory]
    [MemberData(nameof(DottedCodesUnderTurkicCultures))]
    public void FlagLookup_UnderTurkicCurrentCulture_StillResolvesEveryCodeContainingI(string cultureName, string code)
    {
        // Upper-cased on purpose. The defect lives in the lower-casing step, so a code that is
        // already lower-case slips past it unchanged; only an input carrying a dotted capital I
        // exercises the conversion that used to break.
        var upperCased = code.ToUpperInvariant();

        using var _ = new CultureScope(cultureName);

        var image = FlagAssets.GetImage(upperCased);

        Assert.NotNull(image);
        Assert.Same(FlagAssets.GetImage(code), image);
        Assert.Equal(FlagTestData.ReadFlag(code), FlagTestData.ReadFlag(upperCased));
    }

    [Theory]
    [MemberData(nameof(DottedCodesUnderTurkicCultures))]
    public void Exists_UnderTurkicCurrentCulture_StillFindsEveryCodeContainingI(string cultureName, string code)
    {
        using var _ = new CultureScope(cultureName);

        Assert.True(FlagAssets.Exists(code.ToUpperInvariant()));
    }

    /// <summary>
    /// Confirms the trap the sweep above depends on is actually armed on this host.
    /// </summary>
    /// <remarks>
    /// A failure here does not mean the library regressed; it means <c>InvariantGlobalization</c>
    /// is set in this environment, culture-sensitive casing behaves like invariant casing, and
    /// every assertion in this class is therefore vacuous. Fix the environment, not the library.
    /// </remarks>
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("az-AZ")]
    public void CurrentCulture_UnderTurkicCulture_LowerCasesDottedICulturallyButNotInvariantly(string cultureName)
    {
        using var _ = new CultureScope(cultureName);

        Assert.Equal("ıt", "IT".ToLower());
        Assert.Equal("it", "IT".ToLowerInvariant());
    }

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("az-AZ")]
    public void CodesContainingI_AreNumerousEnoughForTheSweepToBeMeaningful(string cultureName)
    {
        using var _ = new CultureScope(cultureName);

        var affected = CountryData.AllFlagCodes.Count(code => code.Contains('i', StringComparison.OrdinalIgnoreCase));

        Assert.True(affected >= 20, $"Only {affected} flag codes contain an I; the sweep has lost its teeth.");
    }

    /// <summary>
    /// Swaps the ambient culture for the duration of a test and puts the original back, whatever
    /// the test does.
    /// </summary>
    /// <remarks>
    /// Both the culture and the UI culture are swapped: they are separate ambient values, and a
    /// test that set only one would leave a reader guessing which of them the code under test
    /// reads. Restoration matters because these are thread properties on a pooled thread, so a
    /// leak would follow whatever test xUnit schedules there next.
    /// </remarks>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture;
        private readonly CultureInfo _uiCulture;

        public CultureScope(string cultureName)
        {
            _culture = CultureInfo.CurrentCulture;
            _uiCulture = CultureInfo.CurrentUICulture;

            var target = new CultureInfo(cultureName);

            CultureInfo.CurrentCulture = target;
            CultureInfo.CurrentUICulture = target;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }
}
