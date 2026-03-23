namespace FitToCsvConverter.Main;

#region Info
// //  
// BionicCode.BionicSwipePageFrame
#endregion

using System.Windows;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "<Pending>")]
public delegate void ValueChangedRoutedEventHandler<TValue>(object sender, ValueChangedRoutedEventArgs<TValue> e);

public class ValueChangedRoutedEventArgs<TValue> : RoutedEventArgs
{
    public TValue OldValue { get; }
    public TValue NewValue { get; }

    public ValueChangedRoutedEventArgs(RoutedEvent routedEvent, object sender, TValue oldValue, TValue newValue) : base(routedEvent, sender)
    {
        this.OldValue = oldValue;
        this.NewValue = newValue;
    }
}