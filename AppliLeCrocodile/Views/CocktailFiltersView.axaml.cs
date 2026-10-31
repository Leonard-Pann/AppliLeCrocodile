using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AppliLeCrocodile.Views;

public partial class CocktailFiltersView : UserControl
{
    public CocktailFiltersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs eventArgs)
    {
        base.OnLoaded(eventArgs);
        FocusCloseButton();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != IsVisibleProperty)
        {
            return;
        }

        if (IsVisible)
        {
            FocusCloseButton();
        }
        else
        {
            IngredientPicker?.Flyout?.Hide();
        }
    }

    private void FocusCloseButton()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (IsEffectivelyVisible)
            {
                CloseFiltersButton?.Focus();
            }
        });
    }
}
