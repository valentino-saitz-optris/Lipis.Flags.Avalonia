using System.Globalization;
using Avalonia.Data.Converters;

namespace Lipis.Flags.Avalonia;

/// <summary>
/// Converts an ISO 3166-1 alpha-2 country code into that country's flag, ready to bind to an image.
/// </summary>
/// <remarks>
/// An unrecognised country code converts to <see langword="null"/>, which leaves the target empty
/// rather than raising.
/// </remarks>
/// <example>
/// <code language="xaml">
/// &lt;Image Source="{Binding Iso2, Converter={x:Static flags:CountryIdToFlagImageSourceConverter.Instance}}" /&gt;
/// </code>
/// </example>
public sealed class CountryIdToFlagImageSourceConverter : IValueConverter
{
    /// <summary>
    /// A shared instance, so consumers can reference the converter without declaring it as a resource.
    /// </summary>
    public static CountryIdToFlagImageSourceConverter Instance { get; } = new();

    /// <inheritdoc />
    /// <remarks>The converter parameter selects the <see cref="FlagAspectRatio"/>.</remarks>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => FlagAssets.GetImage(value as string, ParseAspectRatio(parameter));

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always. The converter is one-way; nothing in a binding needs to turn a rendered flag back into a code.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException($"{nameof(CountryIdToFlagImageSourceConverter)} is a one-way converter.");

    // Reads the aspect ratio from a converter parameter. Strings are accepted because
    // ConverterParameter="1x1" is the form XAML reaches for first; without them that markup would
    // silently select the 4:3 default.
    internal static FlagAspectRatio ParseAspectRatio(object? parameter) => parameter switch
    {
        FlagAspectRatio aspectRatio => aspectRatio,
        string text when text.Equals("1x1", StringComparison.OrdinalIgnoreCase)
                      || text.Equals(nameof(FlagAspectRatio.OneByOne), StringComparison.OrdinalIgnoreCase)
            => FlagAspectRatio.OneByOne,
        _ => FlagAspectRatio.FourByThree,
    };
}
