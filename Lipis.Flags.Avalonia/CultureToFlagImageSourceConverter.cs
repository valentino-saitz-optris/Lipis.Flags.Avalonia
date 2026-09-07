using System.Globalization;
using Avalonia.Data.Converters;

namespace Lipis.Flags.Avalonia;

/// <summary>
/// Converts a <see cref="CultureInfo"/>, or a culture name such as <c>"de-DE"</c>, into that
/// culture's flag, ready to bind to an image.
/// </summary>
/// <remarks>
/// Flags denote countries, not languages, so a language picker generally reads better with the
/// flag beside the language's own name than in place of it.
/// </remarks>
/// <example>
/// <code language="xaml">
/// &lt;Image Source="{Binding CultureName, Converter={x:Static flags:CultureToFlagImageSourceConverter.Instance}}" /&gt;
/// </code>
/// </example>
public sealed class CultureToFlagImageSourceConverter : IValueConverter
{
    /// <summary>
    /// A shared instance, so consumers can reference the converter without declaring it as a resource.
    /// </summary>
    public static CultureToFlagImageSourceConverter Instance { get; } = new();

    /// <summary>
    /// Resolution options applied to every conversion. Defaults to <see cref="CultureFlagOptions.Default"/>.
    /// </summary>
    /// <remarks>
    /// Set this on a converter declared as a XAML resource to pin the answers that matter to your
    /// application, rather than inheriting whatever CLDR currently says.
    /// </remarks>
    public CultureFlagOptions Options { get; set; } = CultureFlagOptions.Default;

    /// <inheritdoc />
    /// <remarks>The converter parameter selects the <see cref="FlagAspectRatio"/>.</remarks>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var target = value switch
        {
            CultureInfo cultureInfo => cultureInfo,
            string name => TryCreate(name),
            _ => null,
        };

        return target.GetFlagImage(CountryIdToFlagImageSourceConverter.ParseAspectRatio(parameter), Options);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always, because a flag maps to many cultures.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException($"{nameof(CultureToFlagImageSourceConverter)} is a one-way converter.");

    // Builds a culture from a name. A stale or user-supplied name is a display concern, not grounds
    // for throwing out of a binding, so an unknown one simply has no flag.
    private static CultureInfo? TryCreate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
