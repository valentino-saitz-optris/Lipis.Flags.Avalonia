using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Demo.Views;

public partial class CultureView : UserControl
{
    public CultureView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
