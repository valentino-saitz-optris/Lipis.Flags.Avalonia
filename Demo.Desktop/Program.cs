using Avalonia;
#if DEBUG
using Keincheck.Avalonia;
#endif

namespace Demo.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Avalonia configuration; do not remove, as the visual designer uses this too.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

#if DEBUG
        // Keincheck drives the sample UI without a human at the screen. Debug-only, matching the
        // Debug-conditional package reference in Demo.Desktop.csproj.
        builder = builder.UseMcpClient(options => options.AppId = "flags-demo");
#endif

        return builder;
    }
}
