using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
        AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        CommentInput.GotFocus += (_, _) =>
        {
            if (viewModel is not null)
            {
                viewModel.IsEditingComment = true;
                Dispatcher.UIThread.Post(() =>
                {
                    if (viewModel?.IsEditingComment == true)
                    {
                        FocusedCommentInput.CaretIndex = FocusedCommentInput.Text?.Length ?? 0;
                        FocusedCommentInput.Focus();
                    }
                });
            }
        };
        FinishEditingButton.Click += (_, _) => FinishEditing();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (viewModel?.IsEditingComment != true || eventArgs.Source is not Visual source)
        {
            return;
        }

        if (source.FindAncestorOfType<TextBox>(includeSelf: true) == FocusedCommentInput
            || source.FindAncestorOfType<Button>(includeSelf: true) == FinishEditingButton)
        {
            return;
        }

        FinishEditing();
        eventArgs.Handled = true;
    }

    private void FinishEditing()
    {
        if (viewModel is not null)
        {
            viewModel.IsEditingComment = false;
            StarChoices.GetVisualDescendants().OfType<Button>().FirstOrDefault()?.Focus();
        }
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
