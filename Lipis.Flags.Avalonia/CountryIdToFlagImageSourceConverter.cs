using System.Globalization;
using Avalonia.Data.Converters;

namespace Lipis.Flags.Avalonia;

public sealed class CountryIdToFlagImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => FlagAssets.GetImage(value as string, ParseAspectRatio(parameter));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Reads the aspect ratio from a converter parameter, accepting the enum or the strings
    /// "4x3", "1x1", "FourByThree" and "OneByOne".
    /// </summary>
    internal static FlagAspectRatio ParseAspectRatio(object? parameter) => parameter switch
    {
        FlagAspectRatio aspectRatio => aspectRatio,
        string text when text.Equals("1x1", StringComparison.OrdinalIgnoreCase)
                      || text.Equals(nameof(FlagAspectRatio.OneByOne), StringComparison.OrdinalIgnoreCase)
            => FlagAspectRatio.OneByOne,
        _ => FlagAspectRatio.FourByThree,
    };
}
