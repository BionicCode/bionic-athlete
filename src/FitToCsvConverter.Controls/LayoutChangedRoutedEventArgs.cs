namespace FitToCsvConverter.Controls;

using System.Windows;

public class LayoutChangedRoutedEventArgs : RoutedEventArgs
{
    public LayoutChangedRoutedEventArgs(RoutedEvent routedEvent, object source, UniformToolBarLayoutResult layoutResult) : base(routedEvent, source) => LayoutResult = layoutResult;

    public UniformToolBarLayoutResult LayoutResult { get; }
}