namespace FitToCsvConverter.Controls;

using System.Windows;

public class OverflowChangedRoutedEventArgs : RoutedEventArgs
{
    public OverflowChangedRoutedEventArgs(RoutedEvent routedEvent, object source, UniformToolBarLayoutResult layoutResult) : base(routedEvent, source) => OverflowItems = overflowItems;

    public object OverflowItems { get; }
}