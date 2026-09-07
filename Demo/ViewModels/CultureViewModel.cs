using System.Globalization;
using Avalonia.Svg;
using Lipis.Flags.Avalonia;

namespace Demo.ViewModels;

/// <summary>
/// One culture, resolved twice: once by this library and once by the obvious-but-wrong
/// <see cref="RegionInfo"/> approach, so the two answers can be read side by side.
/// </summary>
public sealed record CultureRow(
    string CultureName,
    string EnglishName,
    string Resolution,
    string LibraryLabel,
    string NaiveLabel,
    bool NaiveDiffers,
    SvgImage? Flag);

public sealed class CultureViewModel : ObservableObject
{
    /// <summary>
    /// The culture used to demonstrate <see cref="CultureFlagOptions.Overrides"/>: the library
    /// follows CLDR to Taiwan, so an application that wants Hong Kong has to say so. Overrides are
    /// checked before the library's own corrections, which is what makes the flag change.
    /// </summary>
    private const string OverrideCulture = "zh-Hant";

    private const string OverrideFlagCode = "hk";

    /// <remarks>
    /// Ordered by the point each culture makes, not alphabetically. The first block is the set a
    /// naive mapping gets wrong, the second the set that legitimately has no flag, the rest are
    /// everyday cultures that behave.
    /// </remarks>
    private static readonly string[] CandidateCultures =
    [
        "af", "ar", "sv", "cs", "ca", "sr", "sl", "pt", "en",
        "", "es-419", "la", "ckb",
        "de", "de-DE", "en-US", "en-GB", "fr-FR", "es-ES", "pt-BR",
        "it-IT", "ja-JP", "ko-KR", "ru-RU", "hi-IN", "nl-NL", "pl-PL", "tr-TR", "he-IL",
        "zh-Hans", "zh-Hant",
    ];

    private bool _useOverridesOnly;
    private bool _applyOverride;

    private IReadOnlyList<CultureRow> _mismatchRows = [];
    private IReadOnlyList<CultureRow> _noFlagRows = [];
    private IReadOnlyList<CultureRow> _everydayRows = [];
    private CultureRow? _overrideRow;

    public CultureViewModel() => Rebuild();

    /// <summary>
    /// Switches <see cref="NeutralCulturePolicy"/> live. Rows re-group as they change answer.
    /// </summary>
    public bool UseOverridesOnly
    {
        get => _useOverridesOnly;
        set
        {
            if (Set(ref _useOverridesOnly, value))
            {
                Raise(nameof(PolicyDescription));
                Rebuild();
            }
        }
    }

    /// <summary>
    /// Adds the single override zh-Hant to hk.
    /// </summary>
    public bool ApplyOverride
    {
        get => _applyOverride;
        set
        {
            if (Set(ref _applyOverride, value))
            {
                Raise(nameof(OverrideDescription));
                Rebuild();
            }
        }
    }

    public string PolicyDescription => _useOverridesOnly
        ? "OverridesOnly: a neutral culture resolves only when an override says so. Every other neutral culture drops into the no-flag group below."
        : "LikelySubtags: a neutral culture is expanded through CLDR's likely-subtags data, with the ten cultures this runtime gets wrong corrected back to it.";

    public string OverrideDescription => _applyOverride
        ? "Overrides = { [zh-Hant] = hk }"
        : "Overrides = null";

    public IReadOnlyList<CultureRow> MismatchRows
    {
        get => _mismatchRows;
        private set
        {
            if (Set(ref _mismatchRows, value))
            {
                Raise(nameof(MismatchSummary));
            }
        }
    }

    public IReadOnlyList<CultureRow> NoFlagRows
    {
        get => _noFlagRows;
        private set => Set(ref _noFlagRows, value);
    }

    public IReadOnlyList<CultureRow> EverydayRows
    {
        get => _everydayRows;
        private set => Set(ref _everydayRows, value);
    }

    /// <summary>
    /// The zh-Hant row on its own, next to the override toggle, so the flag visibly changes from
    /// Taiwan to Hong Kong as the switch is thrown.
    /// </summary>
    public CultureRow? OverrideRow
    {
        get => _overrideRow;
        private set => Set(ref _overrideRow, value);
    }

    public string MismatchSummary =>
        $"{_mismatchRows.Count} of the {CandidateCultures.Length} cultures on this page resolve to a different flag than new RegionInfo(culture.Name) would.";

    private void Rebuild()
    {
        var options = new CultureFlagOptions
        {
            NeutralCulturePolicy = _useOverridesOnly
                ? NeutralCulturePolicy.OverridesOnly
                : NeutralCulturePolicy.LikelySubtags,
            Overrides = _applyOverride
                ? new Dictionary<string, string> { [OverrideCulture] = OverrideFlagCode }
                : null,
        };

        var mismatched = new List<CultureRow>();
        var unresolved = new List<CultureRow>();
        var everyday = new List<CultureRow>();

        foreach (var name in CandidateCultures)
        {
            var culture = TryGetCulture(name);
            var libraryCode = culture.GetFlagCode(options);
            var (naiveCode, naiveLabel) = ResolveNaively(culture);
            var differs = !string.Equals(naiveCode, libraryCode, StringComparison.OrdinalIgnoreCase);

            var row = new CultureRow(
                CultureName: name.Length == 0 ? "(invariant)" : name,
                EnglishName: DescribeCulture(culture),
                Resolution: DescribeResolution(culture),
                LibraryLabel: libraryCode ?? "no flag",
                NaiveLabel: naiveLabel,
                NaiveDiffers: differs,
                Flag: culture.GetFlagImage(options: options));

            // Grouping is decided by the answers this machine actually produced rather than by a
            // hand-written list, so the page cannot claim a mapping the runtime does not agree with.
            if (libraryCode is null)
            {
                unresolved.Add(row);
            }
            else if (differs)
            {
                mismatched.Add(row);
            }
            else
            {
                everyday.Add(row);
            }

            if (name == OverrideCulture)
            {
                OverrideRow = row;
            }
        }

        MismatchRows = mismatched;
        NoFlagRows = unresolved;
        EverydayRows = everyday;
    }

    private static CultureInfo? TryGetCulture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            // Which cultures exist depends on the host's ICU build. A culture this sample asks for
            // but the machine has never heard of is worth showing as such, not worth throwing over.
            return null;
        }
    }

    private static string DescribeCulture(CultureInfo? culture) => culture switch
    {
        null => "unavailable",
        { Name.Length: 0 } => "Invariant culture",
        _ => culture.EnglishName,
    };

    /// <summary>
    /// Says what the platform itself would do with the culture, so each row explains its own answer.
    /// </summary>
    private static string DescribeResolution(CultureInfo? culture)
    {
        if (culture is null)
        {
            return "not present in this machine's ICU data";
        }

        if (culture.Name.Length == 0)
        {
            return "names no country, and is not reported as a neutral culture either";
        }

        if (!culture.IsNeutralCulture)
        {
            return "specific culture, carries its own country";
        }

        try
        {
            var specific = CultureInfo.CreateSpecificCulture(culture.Name);

            return specific.Name.Length == 0
                ? "neutral, and CreateSpecificCulture cannot expand it: it returns the invariant culture"
                : $"neutral, the runtime expands it to {specific.Name}";
        }
        catch (CultureNotFoundException)
        {
            return "neutral, and CreateSpecificCulture throws for it";
        }
        catch (ArgumentException)
        {
            return "neutral, and CreateSpecificCulture rejects it";
        }
    }

    /// <summary>
    /// The mapping everyone writes first, kept so the page can show its answer beside the
    /// library's. Not merely imprecise but confidently wrong: a bare language subtag is accepted
    /// and reinterpreted as a country code, so Afrikaans becomes Afghanistan.
    /// </summary>
    private static (string? Code, string Label) ResolveNaively(CultureInfo? culture)
    {
        if (culture is null)
        {
            return (null, "n/a");
        }

        try
        {
            var region = new RegionInfo(culture.Name);
            var code = region.TwoLetterISORegionName.ToLowerInvariant();

            return (code, $"{code} {region.EnglishName}");
        }
        catch (ArgumentException)
        {
            return (null, "throws ArgumentException");
        }
    }
}
