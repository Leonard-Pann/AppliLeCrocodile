using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AppliLeCrocodile.ViewModels;
using AppliLeCrocodile.Views;

namespace AppliLeCrocodile;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        MainViewModel mainViewModel = CompositionRoot.CreateMainViewModel();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => CreateMainView(mainViewModel);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = CreateMainView(mainViewModel);
        }

        mainViewModel.InitializeCommand.Execute(null);
        base.OnFrameworkInitializationCompleted();
    }

    private static MainView CreateMainView(MainViewModel mainViewModel)
    {
        return new MainView
        {
            DataContext = mainViewModel
        };
    }
}
