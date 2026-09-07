using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Demo.ViewModels;

namespace Demo;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new Window
                {
                    Title = "Lipis.Flags.Avalonia - Gallery",
                    Width = 1180,
                    Height = 840,
                    MinWidth = 900,
                    MinHeight = 560,
                    Content = new MainView { DataContext = new MainViewModel() },
                };
                break;

            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView { DataContext = new MainViewModel() };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
