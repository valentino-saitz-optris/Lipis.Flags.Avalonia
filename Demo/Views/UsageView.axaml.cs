using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Demo.Views;

public partial class UsageView : UserControl
{
    public UsageView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
