using System.Globalization;
using Avalonia.Data.Converters;
using EnumsNET;

namespace Avalonia.Lipis.Flags;

public sealed class CountryIdToFlagImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var countryId = value as string;

        if (countryId == null)
            return null;

        var aspectRatioFolder = GetAspectRatioFolder(parameter);
        try
        {
            var path = $"avares://Avalonia.Lipis.Flags/Assets/{aspectRatioFolder}/{countryId.ToLower()}.svg";
            return new Uri(path, UriKind.Absolute);
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private string GetAspectRatioFolder(object? parameter)
    {
        var aspectRatio = FlagAspectRatio.FourByThree;
        if (parameter is FlagAspectRatio ratio)
        {
            aspectRatio = ratio;
        }

        var str = aspectRatio.AsString(EnumFormat.Description);
        if (str == null)
            str = "4x3";
        return str;
    }
}