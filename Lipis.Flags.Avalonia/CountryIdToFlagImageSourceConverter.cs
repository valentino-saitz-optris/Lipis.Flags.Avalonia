using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Platform;
using EnumsNET;

namespace Lipis.Flags.Avalonia;

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
            var path = $"avares://Lipis.Flags.Avalonia/Assets/{aspectRatioFolder}/{countryId.ToLowerInvariant()}.svg";
            var uri = new Uri(path, UriKind.Absolute);

            // Without this, an unrecognised code yields a Uri pointing at nothing, and the asset
            // loader raises FileNotFoundException rather than returning null, so a typo in a
            // binding takes the application down instead of leaving the image empty.
            return AssetLoader.Exists(uri) ? uri : null;
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