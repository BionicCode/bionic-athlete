namespace BionicCode.Utilities.Net;

using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

public sealed class PopupService : DependencyObject
{
    private static readonly ConditionalWeakTable<Popup, IInputElement?> s_focusedElementMap = [];

    public static bool GetIsMouseCaptureManagementEnabled(DependencyObject attachingElement) => (bool)(attachingElement?.GetValue(IsMouseCaptureManagementEnabledProperty) ?? BooleanBoxes.FalseBox);

    public static void SetIsMouseCaptureManagementEnabled(DependencyObject attachingElement, bool value) => attachingElement?.SetValue(IsMouseCaptureManagementEnabledProperty, value);

    public static readonly DependencyProperty IsMouseCaptureManagementEnabledProperty = DependencyProperty.RegisterAttached(
        "IsMouseCaptureManagementEnabled",
        typeof(bool),
        typeof(PopupService),
        new PropertyMetadata(BooleanBoxes.FalseBox, OnIsMouseCaptureManagementEnabledChanged));

    private static void OnIsMouseCaptureManagementEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ArgumentExceptionAdvanced.ThrowIfNotAssignableTo<Popup>(d, "Attaching element must be of type Popup.", nameof(d));

        var popup = (Popup)d;
        bool isEnabled = (bool)e.NewValue;
        if (isEnabled)
        {
            popup.Opened += OnPopupOpened;
            popup.Closed += OnPopupClosed;

        }
        else
        {
            popup.Opened -= OnPopupOpened;
            popup.Closed -= OnPopupClosed;
        }
    }

    private static void OnPopupClosed(object? sender, EventArgs e)
    {
        if (sender is not Popup popup)
        {
            // Is not expected to happen, since the attached property is only registered for Popup,
            // but we check it anyway to be safe.
            throw new InvalidOperationException($"The type '{sender?.GetType().FullName ?? "null"}' is not supported. The event source must be convertible to the type '{typeof(Popup).FullName}'.");
        }

        Mouse.RemovePreviewMouseDownOutsideCapturedElementHandler(
            popup,
            OnPreviewMouseDownOutsideCapturedElement);
    }

    private static void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is not Popup popup)
        {
            // Is not expected to happen, since the attached property is only registered for Popup,
            // but we check it anyway to be safe.
            throw new InvalidOperationException($"The type '{sender?.GetType().FullName ?? "null"}' is not supported. The event source must be convertible to the type '{typeof(Popup).FullName}'.");
        }

        IInputElement? focusableElement = s_focusedElementMap.GetValue(popup, FindFirstFocusableChildElement);
        if (focusableElement is null)
        {
            return;
        }

        _ = popup.Dispatcher.InvokeAsync(() => CaptureChild(focusableElement),
            DispatcherPriority.Input);

        Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(
            popup,
            OnPreviewMouseDownOutsideCapturedElement);
    }

    private static void CaptureChild(IInputElement focusableElement)
    {
        _ = Keyboard.Focus(focusableElement);
        _ = focusableElement.Focus();
        _ = Mouse.Capture(focusableElement, CaptureMode.SubTree);
    }

    private static void OnPreviewMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Popup popup)
        {
            // Is not expected to happen, since the attached property is only registered for Popup,
            // but we check it anyway to be safe.
            throw new InvalidOperationException($"The type '{sender?.GetType().FullName ?? "null"}' is not supported. The event source must be convertible to the type '{typeof(Popup).FullName}'.");
        }

        popup.IsOpen = false;

        if (s_focusedElementMap.TryGetValue(popup, out IInputElement? focusableElement)
            && focusableElement is not null)
        {
            focusableElement.ReleaseMouseCapture();
        }
    }

    private static IInputElement? FindFirstFocusableChildElement(Popup popup) => popup.Child.EnumerateVisualChildElements<DependencyObject>()
        .OfType<IInputElement>()
        .FirstOrDefault(inputElement => inputElement.Focusable);
}