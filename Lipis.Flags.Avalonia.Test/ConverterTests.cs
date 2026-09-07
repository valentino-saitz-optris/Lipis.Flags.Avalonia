using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Svg;
using Xunit;

namespace Lipis.Flags.Avalonia.Test;

/// <summary>
/// Protects the two <c>IValueConverter</c> implementations, and above all the type they hand back.
/// </summary>
/// <remarks>
/// Asserting the runtime type is the whole point: every wrong answer this code has given
/// type-checked perfectly. An early release returned a <see cref="Uri"/>, which satisfies an
/// <c>object</c>-typed binding target and renders nothing; the next returned an <c>avares://</c>
/// path string, which stopped resolving when the artwork moved inside the embedded archive. Both
/// looked correct everywhere except on screen.
/// </remarks>
public class ConverterTests
{
    /// <summary>
    /// Converter parameters paired with the aspect ratio they must select.
    /// </summary>
    /// <remarks>
    /// The <see cref="StringComparison"/> row guards the enum pattern from being written loosely
    /// enough to take any enum for an aspect ratio.
    /// </remarks>
    public static TheoryData<object?, FlagAspectRatio> AspectRatioParameters => new()
    {
        { FlagAspectRatio.OneByOne, FlagAspectRatio.OneByOne },
        { "1x1", FlagAspectRatio.OneByOne },
        { "OneByOne", FlagAspectRatio.OneByOne },
        { "ONEBYONE", FlagAspectRatio.OneByOne },
        { FlagAspectRatio.FourByThree, FlagAspectRatio.FourByThree },
        { null, FlagAspectRatio.FourByThree },
        { "4x3", FlagAspectRatio.FourByThree },
        { "FourByThree", FlagAspectRatio.FourByThree },
        { "nonsense", FlagAspectRatio.FourByThree },
        { 42, FlagAspectRatio.FourByThree },
        { StringComparison.OrdinalIgnoreCase, FlagAspectRatio.FourByThree },
    };

    [Fact]
    public void CountryIdConvert_ForAKnownCode_ReturnsABindableImageAndNotAPathOrUri()
    {
        var converted = Convert(CountryIdToFlagImageSourceConverter.Instance, "de");

        var image = Assert.IsType<SvgImage>(converted);

        Assert.IsAssignableFrom<IImage>(image);
        Assert.Same(FlagAssets.GetImage("de"), image);

        // Spelled out because these are the two shapes the bug took before.
        Assert.IsNotType<string>(converted);
        Assert.IsNotType<Uri>(converted);
    }

    [Theory]
    [InlineData("zz")]
    [InlineData("")]
    [InlineData(null)]
    public void CountryIdConvert_ForAnUnknownCode_ReturnsNull(string? value)
        => Assert.Null(Convert(CountryIdToFlagImageSourceConverter.Instance, value));

    /// <remarks>
    /// A binding can hand a converter anything at all. A non-string value must simply produce no
    /// flag rather than a lookup built from <c>ToString</c>.
    /// </remarks>
    [Fact]
    public void CountryIdConvert_ForNonStringInput_ReturnsNull()
    {
        Assert.Null(Convert(CountryIdToFlagImageSourceConverter.Instance, 42));
        Assert.Null(Convert(CountryIdToFlagImageSourceConverter.Instance, new object()));
    }

    [Theory]
    [MemberData(nameof(AspectRatioParameters))]
    public void CountryIdConvert_SelectsTheAspectRatioFromTheConverterParameter(object? parameter, FlagAspectRatio expected)
    {
        var converted = Convert(CountryIdToFlagImageSourceConverter.Instance, "de", parameter);

        AssertIsFlag(converted, "de", expected);
    }

    [Fact]
    public void CountryIdConvertBack_Throws()
        => Assert.Throws<NotSupportedException>(() =>
        {
            CountryIdToFlagImageSourceConverter.Instance.ConvertBack(
                FlagAssets.GetImage("de"),
                typeof(string),
                null,
                CultureInfo.InvariantCulture);
        });

    [Fact]
    public void CultureConvert_ForACultureInfo_ReturnsThatCulturesFlag()
        => AssertIsFlag(
            Convert(CultureToFlagImageSourceConverter.Instance, new CultureInfo("de-DE")),
            "de",
            FlagAspectRatio.FourByThree);

    [Fact]
    public void CultureConvert_ForACultureNameString_ReturnsThatCulturesFlag()
        => AssertIsFlag(
            Convert(CultureToFlagImageSourceConverter.Instance, "de-DE"),
            "de",
            FlagAspectRatio.FourByThree);

    [Theory]
    [MemberData(nameof(AspectRatioParameters))]
    public void CultureConvert_SelectsTheAspectRatioFromTheConverterParameter(object? parameter, FlagAspectRatio expected)
    {
        var converted = Convert(CultureToFlagImageSourceConverter.Instance, "de-DE", parameter);

        AssertIsFlag(converted, "de", expected);
    }

    /// <remarks>
    /// A stale or user-supplied culture name is a display concern, not grounds for tearing down
    /// the UI thread. Note that the miss is not necessarily an exception being swallowed: ICU
    /// accepts "not-a-culture" and hands back a culture named "not", which then simply resolves to
    /// no country. Either route has to end in <see langword="null"/>.
    /// </remarks>
    [Theory]
    [InlineData("not-a-culture")]
    [InlineData("")]
    [InlineData("   ")]
    public void CultureConvert_ForAnUnknownCultureName_ReturnsNullWithoutThrowing(string value)
        => Assert.Null(Convert(CultureToFlagImageSourceConverter.Instance, value));

    [Fact]
    public void CultureConvert_ForUnrelatedInput_ReturnsNull()
    {
        Assert.Null(Convert(CultureToFlagImageSourceConverter.Instance, 42));
        Assert.Null(Convert(CultureToFlagImageSourceConverter.Instance, null));
    }

    [Fact]
    public void CultureConvert_HonoursTheOptionsProperty()
    {
        // The shared Instance is never mutated: Options is settable, and a test that reconfigured
        // the singleton would leak that configuration into every other test in the assembly.
        Assert.Same(
            FlagAssets.GetImage("us"),
            Convert(CultureToFlagImageSourceConverter.Instance, "en"));

        var conservative = new CultureToFlagImageSourceConverter
        {
            Options = new CultureFlagOptions { NeutralCulturePolicy = NeutralCulturePolicy.OverridesOnly },
        };

        Assert.Null(Convert(conservative, "en"));

        var pinned = new CultureToFlagImageSourceConverter
        {
            Options = new CultureFlagOptions
            {
                Overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "gb" },
            },
        };

        Assert.Same(FlagAssets.GetImage("gb"), Convert(pinned, "en"));
    }

    [Fact]
    public void CultureConvertBack_Throws()
        => Assert.Throws<NotSupportedException>(() =>
        {
            CultureToFlagImageSourceConverter.Instance.ConvertBack(
                FlagAssets.GetImage("de"),
                typeof(CultureInfo),
                null,
                CultureInfo.InvariantCulture);
        });

    /// <summary>
    /// Asserts that a conversion produced a particular country's flag in a particular shape.
    /// </summary>
    /// <remarks>
    /// Both halves matter: reference equality says the converter asked <see cref="FlagAssets"/> for
    /// exactly this code and ratio, and the <c>NotSame</c> says the other ratio would have been a
    /// different answer, so a converter that ignored its parameter cannot pass both theory rows.
    /// </remarks>
    private static void AssertIsFlag(object? converted, string code, FlagAspectRatio aspectRatio)
    {
        var image = Assert.IsType<SvgImage>(converted);

        Assert.Same(FlagAssets.GetImage(code, aspectRatio), image);
        Assert.NotSame(FlagAssets.GetImage(code, Other(aspectRatio)), image);
    }

    private static FlagAspectRatio Other(FlagAspectRatio aspectRatio)
        => aspectRatio == FlagAspectRatio.OneByOne ? FlagAspectRatio.FourByThree : FlagAspectRatio.OneByOne;

    private static object? Convert(IValueConverter converter, object? value, object? parameter = null)
        => converter.Convert(value, typeof(object), parameter, CultureInfo.InvariantCulture);
}
