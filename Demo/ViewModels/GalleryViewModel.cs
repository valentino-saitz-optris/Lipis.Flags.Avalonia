using Avalonia.Svg;
using Avalonia.Threading;
using Lipis.Flags.Avalonia;

namespace Demo.ViewModels;

/// <summary>
/// One cell of the flag grid.
/// </summary>
/// <param name="Code">The flag code, shown to the reader and used for the lookup.</param>
/// <param name="Name">The English country name, or a stand-in for the codes that are not countries.</param>
/// <param name="AspectRatio">The asset set this cell draws from.</param>
/// <param name="SlotWidth">Width of the slot the flag is drawn in.</param>
/// <param name="SlotHeight">Height of the slot the flag is drawn in.</param>
/// <remarks>
/// The slot size travels with the entry rather than being read off the parent view model: reaching
/// an ancestor's data context from inside a virtualised item template is the sort of binding that
/// compiles and then quietly resolves to nothing.
/// </remarks>
public sealed record FlagEntry(
    string Code,
    string Name,
    FlagAspectRatio AspectRatio,
    double SlotWidth,
    double SlotHeight)
{
    /// <summary>
    /// The flag itself, as an <see cref="SvgImage"/> ready for <c>Image.Source</c>.
    /// </summary>
    /// <remarks>
    /// Computed on read, not resolved when the entry is built: a binding only reads it when
    /// <c>ItemsRepeater</c> realises the container, so scrolling parses one SVG per visible tile
    /// rather than all 271 up front. <see cref="FlagAssets"/> caches what it parses.
    /// </remarks>
    public SvgImage? Flag => FlagAssets.GetImage(Code, AspectRatio);
}

public sealed class GalleryViewModel : ObservableObject
{
    /// <remarks>
    /// <see cref="CountryData.AllFlagCodes"/> is backed by a hash set, so it has no order worth
    /// relying on. Sorting once here keeps the grid stable between filter passes.
    /// </remarks>
    private static readonly string[] AllCodes =
        CountryData.AllFlagCodes.OrderBy(code => code, StringComparer.Ordinal).ToArray();

    private readonly DispatcherTimer _searchDebounce;

    private string _searchText = string.Empty;
    private bool _isSquare;
    private IReadOnlyList<FlagEntry> _flags = [];

    public GalleryViewModel()
    {
        // Without this, every keystroke re-projects all 271 entries.
        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            Rebuild();
        };

        Rebuild();
    }

    public IReadOnlyList<FlagEntry> Flags
    {
        get => _flags;
        private set
        {
            if (Set(ref _flags, value))
            {
                Raise(nameof(MatchSummary));
            }
        }
    }

    public string MatchSummary => $"Showing {_flags.Count} of {AllCodes.Length} flags";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
            {
                _searchDebounce.Stop();
                _searchDebounce.Start();
            }
        }
    }

    /// <summary>
    /// Switches the grid between the 4:3 and the 1:1 asset set, so both are exercised.
    /// </summary>
    public bool IsSquare
    {
        get => _isSquare;
        set
        {
            if (Set(ref _isSquare, value))
            {
                Rebuild();
            }
        }
    }

    private void Rebuild()
    {
        var ratio = _isSquare ? FlagAspectRatio.OneByOne : FlagAspectRatio.FourByThree;

        // The slot matches the asset's own ratio, so a Uniform stretch fills it exactly instead of
        // letterboxing the flag inside a box of the wrong shape.
        var slotWidth = _isSquare ? 48d : 64d;
        const double slotHeight = 48d;

        var search = _searchText.Trim();
        var results = new List<FlagEntry>(AllCodes.Length);

        foreach (var code in AllCodes)
        {
            var named = CountryData.TryGetName(code, out var name) && name is { Length: > 0 };

            if (search.Length > 0 && !Matches(code, named ? name : null, search))
            {
                continue;
            }

            results.Add(new FlagEntry(
                Code: code,
                Name: named ? name! : "not an ISO 3166-1 country",
                AspectRatio: ratio,
                SlotWidth: slotWidth,
                SlotHeight: slotHeight));
        }

        Flags = results;
    }

    private static bool Matches(string code, string? name, string search)
        => code.Contains(search, StringComparison.OrdinalIgnoreCase)
           || (name is not null && name.Contains(search, StringComparison.OrdinalIgnoreCase));
}
