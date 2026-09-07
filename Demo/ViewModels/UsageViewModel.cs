namespace Demo.ViewModels;

/// <summary>
/// The copyable snippets shown on the Usage page.
/// </summary>
/// <remarks>
/// They live in C# raw string literals rather than in the XAML so that the angle brackets stay
/// readable instead of turning into a wall of <c>&amp;lt;</c>.
/// </remarks>
public sealed class UsageViewModel
{
    public string NamespacesSnippet =>
        """
        xmlns:flags="clr-namespace:Lipis.Flags.Avalonia;assembly=Lipis.Flags.Avalonia"
        """;

    public string CountrySnippet =>
        """
        <Image Width="64" Height="48" Stretch="Uniform"
               Source="{Binding CountryCode,
                                Converter={x:Static flags:CountryIdToFlagImageSourceConverter.Instance}}" />

        <!-- The 1:1 asset set, selected with the converter parameter. -->
        <Image Width="48" Height="48" Stretch="Uniform"
               Source="{Binding CountryCode,
                                Converter={x:Static flags:CountryIdToFlagImageSourceConverter.Instance},
                                ConverterParameter=1x1}" />
        """;

    public string CultureSnippet =>
        """
        <!-- Takes a CultureInfo or a culture name such as "de-DE". -->
        <Image Width="64" Height="48" Stretch="Uniform"
               Source="{Binding CultureName,
                                Converter={x:Static flags:CultureToFlagImageSourceConverter.Instance}}" />
        """;

    public string CodeSnippet =>
        """
        using System.Globalization;
        using Avalonia.Svg;
        using Lipis.Flags.Avalonia;

        // An SvgImage, which is an IImage: assign it to Image.Source or a Button.Content.
        // Parsed once and cached, so asking again is a dictionary hit.
        SvgImage? flag = FlagAssets.GetImage("DE");
        SvgImage? square = FlagAssets.GetImage("de", FlagAspectRatio.OneByOne);

        FlagAssets.Exists("de");   // true
        FlagAssets.Exists("zz");   // false, and GetImage("zz") is null

        // The raw markup, for code that renders or writes out the artwork itself.
        using Stream? svg = FlagAssets.OpenSvgStream("de");

        // "za". Not "af": the culture is Afrikaans, the country is South Africa.
        string? code = CultureInfo.GetCultureInfo("af").GetFlagCode();

        // Straight from a culture to something bindable, skipping the code.
        SvgImage? cultureFlag = CultureInfo.GetCultureInfo("af").GetFlagImage();

        // Every code that has an embedded flag, and the English name where there is one.
        foreach (string flagCode in CountryData.AllFlagCodes)
        {
            CountryData.TryGetName(flagCode, out string? name);
        }
        """;

    public string OptionsSnippet =>
        """
        var options = new CultureFlagOptions
        {
            // Never guess a country for a language. Show nothing instead.
            NeutralCulturePolicy = NeutralCulturePolicy.OverridesOnly,

            // Except where the application has an opinion of its own.
            Overrides = new Dictionary<string, string>
            {
                ["zh-Hant"] = "tw",
                ["en"] = "gb",
            },

            // flag-icons ships an "xx" placeholder for the nothing-matched case.
            FallbackFlagCode = "xx",
        };

        string? code = culture.GetFlagCode(options);
        """;
}
