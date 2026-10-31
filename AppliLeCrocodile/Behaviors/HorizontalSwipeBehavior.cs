using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AppliLeCrocodile.Behaviors;

public sealed class HorizontalSwipeBehavior : AvaloniaObject
{
    private const double DefaultThreshold = 48d;
    private const double DirectionRatio = 1.15d;
    private const double DirectionDetectionDistance = 12d;

    public static readonly AttachedProperty<ICommand?> NextCommandProperty =
        AvaloniaProperty.RegisterAttached<HorizontalSwipeBehavior, InputElement, ICommand?>("NextCommand");

    public static readonly AttachedProperty<ICommand?> PreviousCommandProperty =
        AvaloniaProperty.RegisterAttached<HorizontalSwipeBehavior, InputElement, ICommand?>("PreviousCommand");

    public static readonly AttachedProperty<double> ThresholdProperty =
        AvaloniaProperty.RegisterAttached<HorizontalSwipeBehavior, InputElement, double>("Threshold", DefaultThreshold);

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<HorizontalSwipeBehavior, InputElement, bool>("IsEnabled", true);

    private static readonly ConditionalWeakTable<InputElement, SwipeState> States =
        new ConditionalWeakTable<InputElement, SwipeState>();

    static HorizontalSwipeBehavior()
    {
        NextCommandProperty.Changed.AddClassHandler<InputElement>(OnCommandChanged);
        PreviousCommandProperty.Changed.AddClassHandler<InputElement>(OnCommandChanged);
        IsEnabledProperty.Changed.AddClassHandler<InputElement>(OnIsEnabledChanged);
    }

    public static ICommand? GetNextCommand(InputElement element)
    {
        return element.GetValue(NextCommandProperty);
    }

    public static void SetNextCommand(InputElement element, ICommand? value)
    {
        element.SetValue(NextCommandProperty, value);
    }

    public static ICommand? GetPreviousCommand(InputElement element)
    {
        return element.GetValue(PreviousCommandProperty);
    }

    public static void SetPreviousCommand(InputElement element, ICommand? value)
    {
        element.SetValue(PreviousCommandProperty, value);
    }

    public static double GetThreshold(InputElement element)
    {
        return element.GetValue(ThresholdProperty);
    }

    public static void SetThreshold(InputElement element, double value)
    {
        element.SetValue(ThresholdProperty, value);
    }

    public static bool GetIsEnabled(InputElement element)
    {
        return element.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(InputElement element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    private static void OnCommandChanged(InputElement element, AvaloniaPropertyChangedEventArgs change)
    {
        EnsureHandlersAttached(element);
    }

    private static void OnIsEnabledChanged(InputElement element, AvaloniaPropertyChangedEventArgs change)
    {
        EnsureHandlersAttached(element);

        if (change.NewValue is false && States.TryGetValue(element, out SwipeState? state))
        {
            state.Pointer?.Capture(null);
            state.Reset();
        }
    }

    private static void EnsureHandlersAttached(InputElement element)
    {
        if (States.TryGetValue(element, out SwipeState? existingState))
        {
            return;
        }

        SwipeState state = new SwipeState();
        States.Add(element, state);
        element.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, true);
        element.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel, true);
        element.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel, true);
        element.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Tunnel, true);
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is not InputElement element || !States.TryGetValue(element, out SwipeState? state))
        {
            return;
        }

        if (!GetIsEnabled(element))
        {
            state.Reset();
            return;
        }

        if (eventArgs.Source is Visual visual && visual.FindAncestorOfType<TextBox>(includeSelf: true) is not null)
        {
            state.Reset();
            return;
        }

        PointerPoint pointerPoint = eventArgs.GetCurrentPoint(element);
        if (eventArgs.Pointer.Type == PointerType.Mouse && !pointerPoint.Properties.IsLeftButtonPressed)
        {
            return;
        }

        state.Pointer = eventArgs.Pointer;
        state.StartPosition = pointerPoint.Position;
        state.CurrentPosition = pointerPoint.Position;
        state.IsVerticalGesture = false;
        state.HasCapturedPointer = false;
    }

    private static void OnPointerMoved(object? sender, PointerEventArgs eventArgs)
    {
        if (sender is not InputElement element || !States.TryGetValue(element, out SwipeState? state))
        {
            return;
        }

        if (!GetIsEnabled(element))
        {
            state.Reset();
            return;
        }

        if (state.Pointer != eventArgs.Pointer)
        {
            return;
        }

        state.CurrentPosition = eventArgs.GetPosition(element);
        Vector delta = state.CurrentPosition - state.StartPosition;

        if (Math.Abs(delta.Y) >= DirectionDetectionDistance && Math.Abs(delta.Y) > Math.Abs(delta.X))
        {
            state.IsVerticalGesture = true;
            return;
        }

        if (state.IsVerticalGesture)
        {
            return;
        }

        bool isHorizontalGesture = Math.Abs(delta.X) >= DirectionDetectionDistance
            && Math.Abs(delta.X) > Math.Abs(delta.Y) * DirectionRatio;

        if (isHorizontalGesture && !state.HasCapturedPointer)
        {
            eventArgs.Pointer.Capture(element);
            state.HasCapturedPointer = true;
        }
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs eventArgs)
    {
        if (sender is not InputElement element || !States.TryGetValue(element, out SwipeState? state))
        {
            return;
        }

        if (!GetIsEnabled(element))
        {
            state.Reset();
            return;
        }

        if (state.Pointer != eventArgs.Pointer)
        {
            return;
        }

        Point endPosition = eventArgs.GetPosition(element);
        Vector delta = endPosition - state.StartPosition;
        bool isVerticalGesture = state.IsVerticalGesture;
        bool hasCapturedPointer = state.HasCapturedPointer;
        state.Reset();

        if (hasCapturedPointer)
        {
            eventArgs.Pointer.Capture(null);
        }

        double threshold = Math.Max(1d, GetThreshold(element));
        bool isHorizontalSwipe = !isVerticalGesture
            && Math.Abs(delta.X) >= threshold
            && Math.Abs(delta.X) > Math.Abs(delta.Y) * DirectionRatio;

        if (!isHorizontalSwipe)
        {
            return;
        }

        ICommand? command = delta.X < 0d ? GetNextCommand(element) : GetPreviousCommand(element);
        if (command?.CanExecute(null) != true)
        {
            return;
        }

        command.Execute(null);
        eventArgs.Handled = true;
    }

    private static void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs eventArgs)
    {
        if (sender is InputElement element && States.TryGetValue(element, out SwipeState? state))
        {
            state.Reset();
        }
    }

    private sealed class SwipeState
    {
        public IPointer? Pointer { get; set; }

        public Point StartPosition { get; set; }

        public Point CurrentPosition { get; set; }

        public bool IsVerticalGesture { get; set; }

        public bool HasCapturedPointer { get; set; }

        public void Reset()
        {
            Pointer = null;
            StartPosition = default;
            CurrentPosition = default;
            IsVerticalGesture = false;
            HasCapturedPointer = false;
        }
    }
}
