
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