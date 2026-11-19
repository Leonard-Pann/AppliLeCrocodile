
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AppliLeCrocodile.ViewModels;

namespace AppliLeCrocodile.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        UpdateButton.Click += async (_, _) =>
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is not null)
            {
                await topLevel.Launcher.LaunchUriAsync(new Uri("https://github.com/Leonard-Pann/AppliLeCrocodile/releases"));
            }
        };
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs eventArgs)
    {
        base.OnAttachedToVisualTree(eventArgs);
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
        {
            topLevel.BackRequested += OnBackRequested;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs eventArgs)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
        {
            topLevel.BackRequested -= OnBackRequested;
        }
        base.OnDetachedFromVisualTree(eventArgs);
    }

    private void OnBackRequested(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is MainViewModel { IsUpdateAvailable: true } main)
        {
            main.DismissUpdateCommand.Execute(null);
            eventArgs.Handled = true;
            return;
        }

        CocktailMenuPageViewModel? page = (DataContext as MainViewModel)?.CocktailPage;
        if (page?.SelectedCocktail is CocktailDetailViewModel detail)
        {
            detail.CloseCommand.Execute(null);
            eventArgs.Handled = true;
        }
        else if (page?.IsIngredientPickerOpen == true)
        {
            page.CloseIngredientPickerCommand.Execute(null);
            eventArgs.Handled = true;
        }
        else if (page?.IsFilterOpen == true)
        {
            page.CloseFiltersCommand.Execute(null);
            eventArgs.Handled = true;
        }
    }
}
