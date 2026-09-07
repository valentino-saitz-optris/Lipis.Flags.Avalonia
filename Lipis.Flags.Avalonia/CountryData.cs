using System.Diagnostics.CodeAnalysis;

namespace Lipis.Flags.Avalonia;

/// <summary>
/// A country recognised by ISO 3166-1, paired with the English name shipped alongside its flag.
/// </summary>
/// <remarks>
/// The backing tables are generated from the same upstream release as the flag assets, so names
/// and flags cannot drift apart. See <c>tools/FlagGenerator</c>.
/// </remarks>
public partial class CountryData
{
    /// <summary>
    /// The ISO 3166-1 alpha-2 codes of every named country, upper-cased.
    /// </summary>
    public static IEnumerable<string> AllCountryIds => _englishNameByIso2.Keys;

    /// <summary>
    /// Every named country, in code order.
    /// </summary>
    public static IEnumerable<CountryData> AllCountries => _allCountries;

    private static readonly IReadOnlyList<CountryData> _allCountries;

    // Built here rather than in a field initialiser: this class is split across two files, static
    // field initialisers run in compilation order, and _englishNameByIso2 lives in the other one. A
    // static constructor runs after every part's initialisers, whatever that order turns out to be.
    static CountryData() =>
        _allCountries = _englishNameByIso2.Select(pair => new CountryData(pair.Key, pair.Value)).ToArray();

    /// <summary>
    /// Every code that has an embedded flag, lower-cased.
    /// </summary>
    /// <remarks>
    /// A superset of <see cref="AllCountryIds"/>: flag-icons also ships flags for non-ISO entities
    /// such as the European Union, the United Nations, Kosovo and the four nations of the United
    /// Kingdom, which have artwork but no entry in the country table.
    /// </remarks>
    public static IEnumerable<string> AllFlagCodes => _flagCodes;

    /// <summary>
    /// Looks up a country's English name.
    /// </summary>
    /// <param name="iso2">An ISO 3166-1 alpha-2 code. Matching is case-insensitive.</param>
    /// <param name="name">The English name, or <see langword="null"/> if the code is not a named country.</param>
    /// <returns><see langword="true"/> if the code was found; otherwise <see langword="false"/>.</returns>
    public static bool TryGetName(string iso2, [NotNullWhen(true)] out string? name) => _englishNameByIso2.TryGetValue(iso2, out name);

    /// <summary>
    /// Gets a value indicating whether an embedded flag exists for the supplied code.
    /// </summary>
    /// <param name="code">A flag code. Matching is case-insensitive.</param>
    /// <returns><see langword="true"/> if a flag is embedded for the code; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Tests against the asset set rather than the country set, so the non-ISO flags remain
    /// reachable. Callers wanting countries only should use <see cref="TryGetName"/>.
    /// </remarks>
    public static bool HasFlag(string code) => _flagCodes.Contains(code);

    /// <summary>
    /// The country's ISO 3166-1 alpha-2 code, upper-cased.
    /// </summary>
    public string Iso2 { get; }

    /// <summary>
    /// The country's English name.
    /// </summary>
    public string Name { get; }

    private CountryData(string iso2, string name)
    {
        Iso2 = iso2;
        Name = name;
    }
}
