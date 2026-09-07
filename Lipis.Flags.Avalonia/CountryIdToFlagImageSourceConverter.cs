using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Platform;

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

    /// <summary>
    /// Gets the folder name corresponding to the aspect ratio. (e.g., "4x3" or "1x1")
    /// </summary>
    private static string GetAspectRatioFolder(object? parameter) => parameter switch
    {
        FlagAspectRatio.OneByOne => "1x1",
        _ => "4x3",
    };
}
