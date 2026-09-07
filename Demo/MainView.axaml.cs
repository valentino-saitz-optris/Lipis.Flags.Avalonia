using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Demo;

public partial class MainView : UserControl
{
    public MainView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Forces a theme variant, so the "works in light and dark" claim can be checked without
    /// going out to the operating system's settings and back.
    /// </summary>
    private void OnThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Application.Current is not { } application || sender is not ComboBox selector)
        {
            return;
        }

        application.RequestedThemeVariant = selector.SelectedIndex switch
        {
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
