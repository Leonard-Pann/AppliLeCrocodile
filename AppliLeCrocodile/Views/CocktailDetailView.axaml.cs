using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AppliLeCrocodile.Views;

public partial class CocktailDetailView : UserControl
{
    public CocktailDetailView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs eventArgs)
    {
        base.OnLoaded(eventArgs);
        BackButton.Focus();
    }
}
