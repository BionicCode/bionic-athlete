namespace BionicCode.Utilities.Net;

/// <summary>
/// PropertyChanged event handler that supports standard property changed signature events with additional old value and new value parameters.
/// </summary>
/// <typeparam name="TValue"></typeparam>
[Obsolete("This delegate is deprecated. Use 'EventHandler<PropertyValueChangedArgs<TValue>>' instead.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "Adds clarity")]
public delegate void PropertyValueChangedEventHandler<TValue>(
  object sender,
  PropertyValueChangedArgs<TValue> propertyChangedArgs);