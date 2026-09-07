# Lipis.Flags.Avalonia

A small library for showing country flags in Avalonia.

Lipis.Flags.Avalonia embeds the [flag-icons](https://github.com/lipis/flag-icons) SVG set and provides
Avalonia value converters that turn an ISO 3166-1 alpha-2 country code, or a `CultureInfo`, into an
image of the matching flag.

It is based on FamFamFam.Flags.Wpf, from which it lifts part of its internal workings.

- Targets `net8.0` and `net10.0`.
- Depends on Avalonia 12.0.1 and Svg.Controls.Avalonia 12.0.0.17.
- 271 flags, each in a 4:3 and a 1:1 variant.
- Built with `IsAotCompatible`, so the library's own code is checked for trim and AOT safety at build
  time.

## Installation

Source from [NuGet](https://www.nuget.org/packages/Lipis.Flags.Avalonia/):

> Install-Package Lipis.Flags.Avalonia

Nothing else is needed to draw a flag. The converters return an `SvgImage`, which implements
`IImage`, so what comes back binds straight to `Image.Source`. The SVG renderer that produces it,
[Svg.Controls.Avalonia](https://www.nuget.org/packages/Svg.Controls.Avalonia/), arrives with the
package.

## Usage

Add the library's namespace to your XAML root element:

```xaml
xmlns:flags="clr-namespace:Lipis.Flags.Avalonia;assembly=Lipis.Flags.Avalonia"
```

`CountryIdToFlagImageSourceConverter` turns a two-letter country code into that country's flag. Bind
it to `Image.Source`:

```xaml
<Image Source="{Binding CountryCode, Converter={x:Static flags:CountryIdToFlagImageSourceConverter.Instance}}"
       Width="32" />
```

Earlier releases returned a path rather than an image, and a path bound to `Image.Source` produces no
flag. The converters now return the image itself, so an `Image` is all that is needed to show one.

Both converters expose a shared `Instance`, so `{x:Static}` is enough for the common case. If you
prefer a resource, or need to configure the converter, declare one instead:

```xaml
<Window.Resources>
  <flags:CountryIdToFlagImageSourceConverter x:Key="CountryIdToFlag" />
</Window.Resources>
```

```xaml
<Image Source="{Binding CountryCode, Converter={StaticResource CountryIdToFlag}}" Width="32" />
```

Codes are matched case-insensitively and surrounding whitespace is ignored, so `"DE"`, `"de"` and
`" de "` all resolve. A `null`, empty or unrecognised code converts to `null`, which leaves the target
empty. Both converters are one-way; `ConvertBack` throws `NotSupportedException`.

### A country picker

`CountryData.AllCountries` exposes the 249 named ISO 3166-1 countries, each with its upper-cased
`Iso2` code and English `Name`:

```xaml
<ComboBox ItemsSource="{x:Static flags:CountryData.AllCountries}">
  <ComboBox.ItemTemplate>
    <DataTemplate DataType="flags:CountryData">
      <StackPanel Orientation="Horizontal" Spacing="8">
        <Image Source="{Binding Iso2, Converter={x:Static flags:CountryIdToFlagImageSourceConverter.Instance}}"
               Width="24" Height="18" />
        <TextBlock Text="{Binding Name}" VerticalAlignment="Center" />
      </StackPanel>
    </DataTemplate>
  </ComboBox.ItemTemplate>
</ComboBox>
```

### Resolving flags from code

`FlagAssets` is the same lookup the converters use, without the binding machinery:

```csharp
using Lipis.Flags.Avalonia;

FlagAssets.Exists("de");                                    // true
FlagAssets.Exists("zz");                                    // false

var flag = FlagAssets.GetImage("DE");                       // 4:3, ready for Image.Source
var square = FlagAssets.GetImage("de", FlagAspectRatio.OneByOne);

FlagAssets.GetImage("zz");                                  // null
```

`GetImage` returns an `SvgImage`, which implements `IImage`, so it can be assigned to `Image.Source`
or to any other `IImage`-typed property; a code with no flag yields `null`. Results are cached per
code and aspect ratio, so a list that scrolls the same flag past repeatedly parses its SVG once.

`OpenSvgStream` hands back the raw SVG markup instead, for code that wants to render or write it
itself. The stream is a private copy: the caller owns it and should dispose it.

```csharp
using var svg = FlagAssets.OpenSvgStream("de");

if (svg is not null)
{
    using var file = File.Create("de.svg");
    svg.CopyTo(file);
}
```

`CountryData` also answers name and membership questions directly:

```csharp
CountryData.TryGetName("de", out var name);  // true, name == "Germany"
CountryData.HasFlag("eu");                   // true

var ids = CountryData.AllCountryIds;         // 249 upper-cased ISO 3166-1 alpha-2 codes
var codes = CountryData.AllFlagCodes;        // 271 lower-cased codes that have an embedded flag
```

`AllFlagCodes` is a superset of `AllCountryIds`. flag-icons ships flags for entities that are not
ISO 3166-1 countries: the European Union, the United Nations, Kosovo, the four nations of the
United Kingdom, three Spanish autonomous communities, a few regional organisations, the constituent
parts of Saint Helena, a handful of exceptionally reserved codes such as `ic` and `dg`, and an `xx`
placeholder. Those have a flag but no entry in the country table, so they resolve through
`FlagAssets` while remaining absent from `AllCountries`.

## Aspect ratios

Every flag ships in 4:3 (the default) and 1:1. Both converters read the aspect ratio from the
converter parameter, which accepts a `FlagAspectRatio` value:

```xaml
<Image Source="{Binding Iso2, Converter={x:Static flags:CountryIdToFlagImageSourceConverter.Instance},
                        ConverterParameter={x:Static flags:FlagAspectRatio.OneByOne}}" />
```

or the strings `"1x1"`, `"4x3"`, `"OneByOne"` and `"FourByThree"`, matched case-insensitively:

```xaml
<Image Source="{Binding Iso2, Converter={x:Static flags:CountryIdToFlagImageSourceConverter.Instance},
                        ConverterParameter=1x1}" />
```

Anything else selects 4:3.

## Mapping cultures to flags

Language pickers usually have a `CultureInfo` rather than a country code. `CultureFlags` provides
extension methods for that, and `CultureToFlagImageSourceConverter` is their converter form:

```csharp
using System.Globalization;
using Lipis.Flags.Avalonia;

CultureInfo.GetCultureInfo("de-AT").GetFlagCode();   // "at"
CultureInfo.GetCultureInfo("pt-PT").GetFlagCode();   // "pt"
CultureInfo.GetCultureInfo("pt").GetFlagCode();      // "br"  (see below)

CultureInfo.GetCultureInfo("de-DE").GetFlagImage();  // the German flag, as an SvgImage

if (CultureInfo.CurrentUICulture.TryGetFlagCode(out var code))
{
    // code is non-null here
}
```

The converter accepts either a `CultureInfo` or a culture name such as `"de-DE"`:

```xaml
<Image Source="{Binding CultureName, Converter={x:Static flags:CultureToFlagImageSourceConverter.Instance}}"
       Width="24" Height="18" />
```

A culture that carries a country, such as `de-AT`, `en-GB` or `pt-BR`, simply yields that country.
The interesting case is a *neutral* culture, one that names a language and no country.

### Why not `new RegionInfo(culture.Name)`

`RegionInfo` accepts a bare language subtag and reinterprets it as a country code, which is wrong for
most neutral cultures while looking right on a casual check:

| Neutral culture | `new RegionInfo(name)` gives | Which is |
| --- | --- | --- |
| `af` (Afrikaans) | `AF` | Afghanistan |
| `ar` (Arabic) | `AR` | Argentina |
| `sv` (Swedish) | `SV` | El Salvador |
| `cs` (Czech) | `CS` | Serbia |
| `ca` (Catalan) | `CA` | Canada |
| `de` (German) | `DE` | Germany; correct only because DE is both the language and the country code |

Others, such as `en` and `hi`, throw `ArgumentException` instead. This library never constructs a
`RegionInfo` from a neutral culture name: those five cultures resolve to `za`, `eg`, `se`, `cz` and
`es` instead.

### Neutral culture policy

`CultureFlagOptions.NeutralCulturePolicy` decides what happens instead.

`NeutralCulturePolicy.LikelySubtags` (the default) resolves a neutral culture through CLDR's
likely-subtags data, the same table `CultureInfo.CreateSpecificCulture` consults. The .NET runtime
departs from CLDR for ten neutral cultures, which the library corrects back to what CLDR says:
`ar` to `eg`, `la` to `va`, `pap` to `cw`, `prg` to `pl`, `sw` to `tz`, `syr` to `iq`, `ti` to `et`,
`tzm` to `ma`, `yi` to `ua` and `zh-Hant` to `tw`. An override still wins over a correction.

Measured against .NET 10, the policy resolves 852 of the 890 cultures
`CultureInfo.GetCultures(CultureTypes.AllCultures)` reports, and throws for none of them. Candidates
with no embedded flag, such as the invariant culture and the UN M.49 macro-regions `001` and `419`,
are filtered out and return `null`.

`NeutralCulturePolicy.OverridesOnly` resolves a neutral culture only when `Overrides` supplies an
answer, and returns `null` otherwise. On the same measurement 593 cultures resolve, which is exactly
the set that carries a country of its own. Choose this if you would rather show no flag than a
guessed one.

### Overrides

Likely-subtags data ships CLDR's opinions. Some of them will not be the answer your application
wants:

| Neutral culture | Default result |
| --- | --- |
| `en` | `us` |
| `pt` | `br` |
| `ar` | `eg` |
| `zh` | `cn` |
| `zh-Hant` | `tw` |

Outside the corrected cultures these answers come from the host's ICU data and can change with the
ICU version, so pin anything you care about:

```csharp
var options = new CultureFlagOptions
{
    Overrides = new Dictionary<string, string> { ["pt"] = "pt" },
};

CultureInfo.GetCultureInfo("pt").GetFlagCode(options);  // "pt", not the default "br"
```

Overrides are keyed by culture name and matched case-insensitively. `CultureFlagOptions` copies the
entries into a case-insensitive dictionary of its own on assignment, so the comparer your dictionary
was built with does not matter, and the copy is a snapshot: mutating your dictionary afterwards does
not change the options. Both full names and language subtags work, so an entry for `"zh-Hant"`
matches that culture, while an entry for `"zh"` matches any neutral `zh` culture that reaches the
neutral step.

An override on a language subtag does not hijack the specific cultures under it, because those never
reach the neutral step:

```csharp
var options = new CultureFlagOptions
{
    Overrides = new Dictionary<string, string> { ["en"] = "gb" },
};

CultureInfo.GetCultureInfo("en").GetFlagCode(options);     // "gb"
CultureInfo.GetCultureInfo("en-US").GetFlagCode(options);  // "us"
CultureInfo.GetCultureInfo("en-AU").GetFlagCode(options);  // "au"
```

`CultureFlagOptions.FallbackFlagCode` sets a last-resort code for cultures nothing else resolves.
flag-icons ships an `xx` placeholder that suits the purpose:

```csharp
var options = new CultureFlagOptions
{
    NeutralCulturePolicy = NeutralCulturePolicy.OverridesOnly,
    FallbackFlagCode = "xx",
};
```

To use configured options from XAML, set them on a converter instance and expose it as a static:

```csharp
public static class Converters
{
    public static CultureToFlagImageSourceConverter CultureToFlag { get; } = new()
    {
        Options = new CultureFlagOptions
        {
            Overrides = new Dictionary<string, string> { ["pt"] = "pt" },
        },
    };
}
```

```xaml
<Image Source="{Binding CultureName, Converter={x:Static local:Converters.CultureToFlag}}" />
```

### A note on flags and languages

Flags denote countries, not languages, and the two do not line up: many languages are spoken in
several countries, and some have no single home country at all. A language picker generally reads
better when it shows the language's own name and treats the flag as decoration beside it.

## How the flags are stored

The flags ship as one compressed archive embedded in the assembly rather than as one resource per
file. SVG is text and deflates about three to one, which puts the assembly at about 1.36 MB instead
of about 3.87 MB. The archive is read entry by entry, so only the flags an application actually asks
for are ever expanded, and each one is parsed at most once.

This is also why the package depends on Svg.Controls.Avalonia: the artwork is not reachable as an
Avalonia resource, so the library parses it and hands back an `SvgImage`. Beyond that, how the
artwork is stored is an implementation detail with no public surface; nothing in the API names an
asset path.

In the repository the flags remain individual `.svg` files under `Assets/4x3` and `Assets/1x1`, so a
flag update stays readable in a diff and the assets can still be replaced wholesale from upstream.
The archive is a build output.

## The country table

`CountryData` is generated from `country.json`, which is vendored verbatim from flag-icons, by a small
tool in this repository. The build runs it for you, writing the table into `obj/`, so it is never
committed and cannot fall out of date: the names and the artwork always come from the same upstream
release. It is skipped when nothing has changed, and runs once rather than once per target framework.

To run it by hand, for instance to check a sync before building:

```bash
dotnet run --project tools/FlagGenerator
```

It fails rather than emitting a table that references an SVG missing from disk. The library project
additionally asserts at build time that `Assets/4x3` and `Assets/1x1` are both fully populated and hold
the same number of files, so an incomplete checkout fails the build instead of packing a flagless
package.

## Credits

The Lipis flag icons were created by [Panayiotis Lipiridis](https://lipis.dev/).

The flag SVGs and `country.json` are vendored from
[lipis/flag-icons](https://github.com/lipis/flag-icons) under the MIT licence. The attribution ships
with the package as `THIRD-PARTY-NOTICES.txt`.

The original FamFamFam.Flags.Wpf was created by [Drew Noakes](https://github.com/drewnoakes).
