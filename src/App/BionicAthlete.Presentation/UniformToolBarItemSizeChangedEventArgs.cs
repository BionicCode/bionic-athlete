namespace BionicAthlete.Presentation;

using System.Windows;
using BionicCode.Utilities.Net;

public class UniformToolBarItemSizeChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the UniformToolBarItemSizeChangedEventArgs class with the specified routed event,
    /// event source, and size change event data.
    /// </summary>
    /// <param name="routedEvent">The routed event identifier for this event instance.</param>
    /// <param name="source">The object that raised the event.</param>
    /// <param name="sizeChangedEventArgs">The original event data for the most recent size changed event as raised by the <see cref="FrameworkElement.SizeChanged"/> event.</param>
    public UniformToolBarItemSizeChangedEventArgs(RoutedEvent routedEvent, object source, SizeChangedEventArgs sizeChangedEventArgs) : base(routedEvent, source)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(sizeChangedEventArgs);

        WidthChanged = sizeChangedEventArgs.WidthChanged;
        HeightChanged = sizeChangedEventArgs.HeightChanged;
        NewSize = sizeChangedEventArgs.NewSize;
        PreviousSize = sizeChangedEventArgs.PreviousSize;
    }

    public UniformToolBarItemSizeChangedEventArgs(RoutedEvent routedEvent,
        object source,
        bool widthChanged,
        bool heightChanged,
        Size newSize,
        Size previousSize) : base(routedEvent, source)
    {
        WidthChanged = widthChanged;
        HeightChanged = heightChanged;
        NewSize = newSize;
        PreviousSize = previousSize;
    }

    public bool WidthChanged { get; }
    public bool HeightChanged { get; }
    public Size NewSize { get; }
    public Size PreviousSize { get; }
}