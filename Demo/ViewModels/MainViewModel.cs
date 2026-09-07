using System.Reflection;
using Avalonia;

namespace Demo.ViewModels;

public sealed class MainViewModel
{
    public GalleryViewModel Gallery { get; } = new();

    public CultureViewModel Cultures { get; } = new();

    public UsageViewModel Usage { get; } = new();

    /// <summary>
    /// The Avalonia build actually loaded in this process.
    /// </summary>
    /// <remarks>
    /// Read from the loaded assembly rather than written down: the library declares an Avalonia 12
    /// floor, which resolves to whatever 12.x the consumer's graph settles on.
    /// </remarks>
    public string AvaloniaVersionLabel { get; } = $"Avalonia {ReadAvaloniaVersion()}";

    private static string ReadAvaloniaVersion()
    {
        var assembly = typeof(Application).Assembly;

        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        // Informational versions often carry a "+<commit>" build metadata suffix. It is noise here.
        if (informational is { Length: > 0 })
        {
            var plus = informational.IndexOf('+');

            return plus < 0 ? informational : informational[..plus];
        }

        return assembly.GetName().Version?.ToString() ?? "unknown";
    }
}
