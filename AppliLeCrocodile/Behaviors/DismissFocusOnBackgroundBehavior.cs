using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AppliLeCrocodile.Behaviors;

public sealed class DismissFocusOnBackgroundBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<DismissFocusOnBackgroundBehavior, InputElement, bool>("IsEnabled");

    static DismissFocusOnBackgroundBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<InputElement>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(InputElement element)
    {
        return element.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(InputElement element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(InputElement element, AvaloniaPropertyChangedEventArgs change)
    {
        bool wasEnabled = change.OldValue is true;
        bool isEnabled = change.NewValue is true;

        if (wasEnabled == isEnabled)
        {
            return;
        }

        if (isEnabled)
        {
            element.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, true);
        }
        else
        {
            element.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is not InputElement element)
        {
            return;
        }

        if (eventArgs.Source is Visual sourceVisual
            && sourceVisual.FindAncestorOfType<TextBox>(includeSelf: true) is not null)
        {
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(element);
        topLevel?.FocusManager?.Focus(null);
    }
}
