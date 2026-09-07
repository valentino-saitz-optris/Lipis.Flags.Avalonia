using Xunit;

namespace Lipis.Flags.Avalonia.Test;

/// <summary>
/// Protects the generated country and flag tables: their shape, the relationship between them,
/// and the handful of names that the generator exists to keep current.
/// </summary>
/// <remarks>
/// The two tables are not the same set and must not be conflated. <c>AllCountryIds</c> is what
/// ISO 3166-1 names; <c>AllFlagCodes</c> is what flag-icons draws, which includes the European
/// Union, the United Nations, Kosovo, the nations of the United Kingdom and several Spanish
/// autonomous communities. Routing a flag lookup through the country table would drop all of them.
/// </remarks>
public class CountryDataTests
{
    [Theory]
    [InlineData("de", "Germany")]
    [InlineData("DE", "Germany")]
    [InlineData("De", "Germany")]
    [InlineData("dE", "Germany")]
    public void TryGetName_ForAKnownCode_IsCaseInsensitive(string iso2, string expected)
    {
        Assert.True(CountryData.TryGetName(iso2, out var name));
        Assert.Equal(expected, name);
    }

    /// <remarks>
    /// <c>eu</c> is the interesting case: it has a flag but is not a country, so it must be absent
    /// here even though <see cref="CountryData.HasFlag"/> accepts it.
    /// </remarks>
    [Theory]
    [InlineData("zz")]
    [InlineData("QQ")]
    [InlineData("eu")]
    [InlineData("toolong")]
    [InlineData("")]
    public void TryGetName_ForAnUnknownCode_ReturnsFalseAndNull(string iso2)
    {
        Assert.False(CountryData.TryGetName(iso2, out var name));
        Assert.Null(name);
    }

    [Fact]
    public void AllCountries_EveryEntry_HasATwoLetterUpperCaseCodeAndANonEmptyName()
    {
        var countries = CountryData.AllCountries.ToArray();

        Assert.NotEmpty(countries);

        foreach (var country in countries)
        {
            Assert.False(string.IsNullOrWhiteSpace(country.Iso2), "A country entry has a blank code.");
            Assert.False(string.IsNullOrWhiteSpace(country.Name), $"Country '{country.Iso2}' has a blank name.");
            Assert.Equal(2, country.Iso2.Length);
            Assert.Equal(country.Iso2.ToUpperInvariant(), country.Iso2);
        }
    }

    [Fact]
    public void AllCountries_ContainsNoDuplicateCodes()
    {
        var codes = CountryData.AllCountries.Select(country => country.Iso2).ToArray();

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void AllCountries_AgreesWithAllCountryIds()
    {
        Assert.Equal(
            CountryData.AllCountryIds.Order(StringComparer.Ordinal),
            CountryData.AllCountries.Select(country => country.Iso2).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AllFlagCodes_IsAStrictSupersetOfAllCountryIds()
    {
        var flagCodes = CountryData.AllFlagCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var countryIds = CountryData.AllCountryIds.ToArray();

        var withoutArtwork = countryIds.Where(id => !flagCodes.Contains(id)).Order(StringComparer.Ordinal).ToArray();

        Assert.True(
            withoutArtwork.Length == 0,
            $"Named countries with no flag: {string.Join(", ", withoutArtwork)}.");

        Assert.True(
            flagCodes.Count > countryIds.Length,
            $"Expected more flags ({flagCodes.Count}) than named countries ({countryIds.Length}); the non-ISO flags have gone missing.");
    }

    [Fact]
    public void AllFlagCodes_AreAllLowerCaseAndDistinct()
    {
        var codes = CountryData.AllFlagCodes.ToArray();

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        foreach (var code in codes)
        {
            Assert.Equal(code.ToLowerInvariant(), code);
        }
    }

    [Theory]
    [InlineData("eu")]
    [InlineData("un")]
    [InlineData("xk")]
    public void AllFlagCodes_ContainsEntitiesThatAreNotIsoCountries(string code)
    {
        Assert.Contains(code, CountryData.AllFlagCodes, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(code, CountryData.AllCountryIds, StringComparer.OrdinalIgnoreCase);
        Assert.True(CountryData.HasFlag(code));
        Assert.False(CountryData.TryGetName(code, out _));
    }

    [Theory]
    [InlineData("DE")]
    [InlineData("de")]
    [InlineData("Eu")]
    [InlineData("GB-ENG")]
    public void HasFlag_IsCaseInsensitive(string code) => Assert.True(CountryData.HasFlag(code));

    /// <remarks>
    /// Three renames the hand-maintained table had missed and one diacritic it had lost. The
    /// spellings come from the same upstream release as the artwork, so a regenerated table that
    /// reverted to "Swaziland" or mangled the ring would be caught here rather than in a screenshot.
    /// </remarks>
    [Theory]
    [InlineData("SZ", "Eswatini")]
    [InlineData("MK", "North Macedonia")]
    [InlineData("CV", "Cabo Verde")]
    [InlineData("AX", "Åland Islands")]
    public void TryGetName_ForCorrectedCountryNames_ReturnsTheCurrentSpelling(string iso2, string expected)
    {
        Assert.True(CountryData.TryGetName(iso2, out var name));
        Assert.Equal(expected, name);
    }

    /// <remarks>
    /// A separate assertion because the failure mode is a source-encoding accident rather than a
    /// data change: a generated file written or read as anything but UTF-8 replaces the
    /// ring-accented A with two replacement characters, while the rest of the table stays
    /// perfectly readable.
    /// </remarks>
    [Fact]
    public void TryGetName_ForAlandIslands_KeepsTheRingDiacritic()
    {
        Assert.True(CountryData.TryGetName("AX", out var name));
        Assert.NotNull(name);
        Assert.Equal('Å', name[0]);
        Assert.Equal("Åland Islands", name);
    }
}
