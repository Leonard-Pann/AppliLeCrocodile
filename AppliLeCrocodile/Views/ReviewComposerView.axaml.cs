using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AppliLeCrocodile.ViewModels;

namespace AppliLeCrocodile.Views;

public partial class ReviewComposerView : UserControl
{
    private ReviewComposerViewModel? viewModel;

    public ReviewComposerView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs eventArgs)
    {
        base.OnLoaded(eventArgs);
        viewModel = DataContext as ReviewComposerViewModel;
        if (viewModel is not null)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        FocusFirstField();
    }

    protected override void OnUnloaded(RoutedEventArgs eventArgs)
    {
        if (viewModel is not null)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            viewModel = null;
        }

        base.OnUnloaded(eventArgs);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(ReviewComposerViewModel.IsLoading))
        {
            FocusFirstField();
        }
    }

    private void FocusFirstField()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsEffectivelyVisible || viewModel?.IsLoading != false)
            {
                return;
            }

            if (UserNameInput.IsEffectivelyVisible)
            {
                UserNameInput.Focus();
            }
            else
            {
                StarChoices.GetVisualDescendants().OfType<Button>().FirstOrDefault()?.Focus();
            }
        });
    }
}
