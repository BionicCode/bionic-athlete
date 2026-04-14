namespace FitToCsvConverter.Controls;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BionicCode.Utilities.Net;
using global::System.Windows.Data;

public class UniformToolBarPanel : Panel
{
    private Size _measuredUniformSize;
    private Size _measuredRequiredPanelSize;

    #region IsOverflowItem
    /// <summary>
    ///     The key needed set a read-only property.
    /// Attached property to indicate if the item is placed in the overflow panel
    /// </summary>
    protected static readonly DependencyPropertyKey IsOverflowItemPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                    "IsOverflowItem",
                    typeof(bool),
                    typeof(UniformToolBarPanel),
                    new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

    /// <summary>
    ///     The DependencyProperty for the IsOverflowItem property.
    ///     Flags:              None
    ///     Default Value:      false
    /// </summary>
    public static readonly DependencyProperty IsOverflowItemProperty = IsOverflowItemPropertyKey.DependencyProperty;

    /// <summary>
    /// Writes the attached property IsOverflowItem to the given element.
    /// </summary>
    /// <param name="element">The element to which to write the attached property.</param>
    /// <param name="value">The property value to set</param>
    internal static void SetIsOverflowItem(DependencyObject element, object value)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(element);

        element.SetValue(IsOverflowItemPropertyKey, value);
    }

    /// <summary>
    /// Reads the attached property IsOverflowItem from the given element.
    /// </summary>
    /// <param name="element">The element from which to read the attached property.</param>
    /// <returns>The property's value.</returns>
    public static bool GetIsOverflowItem(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(IsOverflowItemProperty);
    }

    #endregion IsOverflowItem 

    #region Orientation
    /// <summary>
    /// Gets or sets the orientation of the <see cref="UniformToolBarPanel"/>. 
    /// This determines whether the toolbar items are arranged horizontally or vertically.
    /// </summary>
    /// <value>The orientation of the <see cref="UniformToolBarPanel"/>. The default is <see cref="Orientation.Horizontal"/>.</value>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// Gets or sets the orientation of the <see cref="UniformToolBarPanel"/>. 
    /// This determines whether the toolbar items are arranged horizontally or vertically.
    /// </summary>
    /// <value>The orientation of the <see cref="UniformToolBarPanel"/>. The default is <see cref="Orientation.Horizontal"/>.</value>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(UniformToolBarPanel),
        new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsParentMeasure));
    #endregion Orientation

    #region ItemHeight
    [TypeConverter(typeof(LengthConverter))]
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
        nameof(ItemHeight),
        typeof(double),
        typeof(UniformToolBarPanel),
        new FrameworkPropertyMetadata(
            48d,
            FrameworkPropertyMetadataOptions.AffectsMeasure),
        new ValidateValueCallback(IsWidthHeightValid));
    #endregion ItemHeight

    #region ItemWidth
    [TypeConverter(typeof(LengthConverter))]
    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
        nameof(ItemWidth),
        typeof(double),
        typeof(UniformToolBarPanel),
        new FrameworkPropertyMetadata(
            48d,
            FrameworkPropertyMetadataOptions.AffectsMeasure),
        new ValidateValueCallback(IsWidthHeightValid));
    #endregion ItemWidth 

    #region OverflowMode
    /// <summary>
    /// Gets or sets the overflow mode of the <see cref="UniformToolBar"/>. 
    /// This determines how the toolbar items behave when there is not enough space.
    /// </summary>
    /// <value>The overflow mode of the <see cref="UniformToolBar"/>. The default is <see cref="OverflowMode.AsNeeded"/>.</value>
    public OverflowMode OverflowMode
    {
        get => (OverflowMode)GetValue(OverflowModeProperty);
        set => SetValue(OverflowModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the overflow mode of the <see cref="UniformToolBar"/>. 
    /// This determines how the toolbar items behave when there is not enough space.
    /// </summary>
    /// <value>The overflow mode of the <see cref="UniformToolBarPanel"/>. The default is <see cref="OverflowMode.AsNeeded"/>.</value>
    public static readonly DependencyProperty OverflowModeProperty = DependencyProperty.Register(
        nameof(OverflowMode),
        typeof(OverflowMode),
        typeof(UniformToolBarPanel),
        new FrameworkPropertyMetadata(OverflowMode.AsNeeded, FrameworkPropertyMetadataOptions.AffectsMeasure));
    #endregion OverflowMode

    #region OverflowDetected event

    public static readonly RoutedEvent OverflowDetectedEvent = EventManager.RegisterRoutedEvent(
        nameof(OverflowDetected),
        RoutingStrategy.Bubble,
        typeof(EventHandler<OverflowDetectedRoutedEventArgs>),
        typeof(UniformToolBarPanel));

    public event EventHandler<OverflowDetectedRoutedEventArgs> OverflowDetected
    {
        add => AddHandler(OverflowDetectedEvent, value);
        remove => RemoveHandler(OverflowDetectedEvent, value);
    }

    #endregion OverflowDetected event
    /// <summary>
    ///     Instantiates a new instance of this class.
    /// </summary>
    public UniformToolBarPanel() : base()
    { }

    //private static void OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //{
    //    var uniformToolBarItem = (UniformToolBarPanel)d;
    //    //uniformToolBarItem.InvalidateMeasure();
    //}

    //private static void OnHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //{
    //    var uniformToolBarItem = (UniformToolBarPanel)d;
    //    //uniformToolBarItem.InvalidateMeasure();
    //}

    private void OnLoaded(object sender, RoutedEventArgs e)
    {

        /*Truth table for BaseValueSource
           ┌───────────────────┬───────────────┬───────────────────────┬──────────────────────────┐
           │ BaseValueSource   │ Has modifiers │ HasDefaultValue(...)  │ HasNonDefaultValue(...)  │
           ├───────────────────┼───────────────┼───────────────────────┼──────────────────────────┤
           │ Default           │ No            │ true                  │ false                    │ <=== Only in this case we want to set the binding, 
           │ Default           │ Yes           │ false                 │ true                     │      otherwise we might be overwriting a user set or inherited value
           │ Local             │ No            │ false                 │ true                     │
           │ Local             │ Yes           │ false                 │ true                     │
           │ Style             │ No            │ false                 │ true                     │
           │ Style             │ Yes           │ false                 │ true                     │
           │ Inherited         │ No            │ false                 │ true                     │
           │ Inherited         │ Yes           │ false                 │ true                     │
           └───────────────────┴───────────────┴───────────────────────┴──────────────────────────┘

        Since we don't want to override any expressions like Binding etc. (original ToolBarPanel overrides user expression) 
        we must add an extra check using ReadLocalValue to determine if the value is actually coming from a default value or not. 
        */
        ValueSource source = DependencyPropertyHelper.GetValueSource(this, OrientationProperty);
        bool isDefaultSource = source.BaseValueSource is BaseValueSource.Default;
        bool isOrientationDefaultValue = isDefaultSource
            && ReadLocalValue(OrientationProperty) == DependencyProperty.UnsetValue;

        DependencyObject parent = VisualTreeHelper.GetParent(this);
        while (parent is not null and not (ToolBar or UniformToolBar))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        if (parent is not ToolBar and not UniformToolBar)
        {
            return;
        }

        if (isOrientationDefaultValue)
        {
            var binding = new Binding
            {
                Source = parent,
                Path = new PropertyPath(ToolBar.OrientationProperty)
            };
            _ = SetBinding(OrientationProperty, binding);
        }

        source = DependencyPropertyHelper.GetValueSource(this, ItemHeightProperty);
        isDefaultSource = source.BaseValueSource is BaseValueSource.Default;
        bool isItemHeightDefaultValue = isDefaultSource
            && ReadLocalValue(ItemHeightProperty) == DependencyProperty.UnsetValue;

        var uniformToolBar = parent as UniformToolBar;
        if (uniformToolBar is not null && isItemHeightDefaultValue)
        {
            var binding = new Binding
            {
                Source = uniformToolBar,
                Path = new PropertyPath(UniformToolBar.ItemHeightProperty)
            };
            _ = SetBinding(ItemHeightProperty, binding);
            //BindingOperations.GetBindingExpression(this, ItemHeightProperty)?.UpdateTarget();
        }

        source = DependencyPropertyHelper.GetValueSource(this, ItemWidthProperty);
        isDefaultSource = source.BaseValueSource is BaseValueSource.Default;
        bool isItemWidthDefaultValue = isDefaultSource
            && ReadLocalValue(ItemWidthProperty) == DependencyProperty.UnsetValue;

        if (isItemWidthDefaultValue && uniformToolBar is not null)
        {
            var binding = new Binding
            {
                Source = uniformToolBar,
                Path = new PropertyPath(UniformToolBar.ItemWidthProperty)
            };
            _ = SetBinding(ItemWidthProperty, binding);
        }

        source = DependencyPropertyHelper.GetValueSource(this, OverflowModeProperty);
        isDefaultSource = source.BaseValueSource is BaseValueSource.Default;
        bool isOverflowModeDefaultValue = isDefaultSource
            && ReadLocalValue(OverflowModeProperty) == DependencyProperty.UnsetValue;

        if (isOverflowModeDefaultValue && uniformToolBar is not null)
        {
            var binding = new Binding
            {
                Source = uniformToolBar,
                Path = new PropertyPath(UniformToolBar.OverflowModeProperty)
            };
            _ = SetBinding(OverflowModeProperty, binding);
        }

        //InvalidateMeasure();
    }

    private static bool IsWidthHeightValid(object value)
    {
        double v = (double)value;
        return double.IsNaN(v) || (v >= 0.0d && !double.IsPositiveInfinity(v));
    }

    #region Layout

    private bool MeasureItems(Size constraint, out ReadOnlyCollection<(int itemIndex, object item)> overflowItems, out Size newUniformChildSize, out Size newPanelSize)
    {
        newPanelSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
        var overflowItemsList = new List<(int itemIndex, object item)>();
        overflowItems = new ReadOnlyCollection<(int itemIndex, object item)>(overflowItemsList);
        //double maxWidth = 0.0;
        //double maxHeight = 0.0;
        bool hasOverflowItems = false;

        if (InternalChildren.Count == 0)
        {
            return false;
        }

        //_isModifyingChildLayout = true;

        ItemContainerGenerator? generator = null;
        if (IsItemsHost)
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            generator = itemsControl.ItemContainerGenerator;
        }

        //List<ContentControl> measuredChildContainers = [];
        //Size oldMeasuredUniformSize = _measuredUniformSize;
        //Size oldMeasuredRequiredPanelSize = _measuredRequiredPanelSize;
        //if (double.IsNaN(ItemWidth)
        //    || double.IsNaN(ItemHeight))
        //{
        //    foreach (ContentControl childContainer in InternalChildren.OfType<ContentControl>())
        //    {
        //        //if (childContainer is UniformToolBarItem uniformToolBarItem)
        //        //{
        //        //    uniformToolBarItem.PrepareForMeasure();
        //        //}
        //        //else
        //        //{
        //        //    childContainer.MinWidth = 0;
        //        //    childContainer.MaxWidth = double.PositiveInfinity;
        //        //    childContainer.MinHeight = 0;
        //        //    childContainer.MaxHeight = double.PositiveInfinity;
        //        //    childContainer.Width = double.NaN;
        //        //    childContainer.Height = double.NaN;
        //        //}

        //        // First measure pass: allow child to provide its natural desired finalDesiredPanelSize
        //        childContainer.Measure(constraint);
        //        Size childDesiredSize = childContainer.DesiredSize;
        //        maxWidth = Math.Max(maxWidth, childDesiredSize.Width);
        //        maxHeight = Math.Max(maxHeight, childDesiredSize.Height);
        //        measuredChildContainers.Add(childContainer);
        //    }
        //}

        //// Enforce ItemWidth if not set to "Auto" (NaN)
        //if (!double.IsNaN(ItemWidth))
        //{
        //    maxWidth = ItemWidth;
        //}

        //// Enforce ItemHeight if not set to "Auto" (NaN)
        //if (!double.IsNaN(ItemHeight))
        //{
        //    maxHeight = ItemHeight;
        //}

        //newUniformChildSize = new Size(maxWidth, maxHeight);
        //if (newUniformChildSize == oldMeasuredUniformSize)
        //{
        //    // If the uniform child finalDesiredPanelSize hasn't changed since the last measure, we can skip re-measuring the children
        //    // because their desired finalDesiredPanelSize will be the same as before and thus the same items will be in the overflow.
        //    newPanelSize = oldMeasuredRequiredPanelSize;
        //    newUniformChildSize = oldMeasuredUniformSize;
        //    //_isModifyingChildLayout = false;
        //    return true;
        //}
        double itemWidth = double.IsNaN(ItemWidth)
            ? 0.0
            : ItemWidth;
        double itemHeight = double.IsNaN(ItemHeight)
            ? 0.0
            : ItemHeight;
        bool isOrientationHorizontal = Orientation is Orientation.Horizontal;
        double maxHorizontalLength = isOrientationHorizontal
            ? constraint.Width
            : double.PositiveInfinity;
        double maxVerticalLength = !isOrientationHorizontal
            ? constraint.Height
            : double.PositiveInfinity;
        double currentHorizontalLength = isOrientationHorizontal
            ? 0.0
            : itemWidth;
        double currentVerticalLength = isOrientationHorizontal
            ? itemHeight
            : 0.0;
        newUniformChildSize = new Size(itemWidth, itemHeight);
        foreach (FrameworkElement childContainer in InternalChildren.OfType<FrameworkElement>())
        {
            SetIsOverflowItem(childContainer, BooleanBoxes.FalseBox);

            // If the total length of the children exceeds the available length in the orientation direction,
            // we can stop measuring and directly mark all remaining containers as overflow items.
            if (!hasOverflowItems)
            {
                // Second measure pass: force child to apply the required finalDesiredPanelSize
                childContainer.Measure(newUniformChildSize);

                if (isOrientationHorizontal)
                {
                    currentHorizontalLength += newUniformChildSize.Width;
                }
                else
                {
                    currentVerticalLength += newUniformChildSize.Height;
                }

                if (OverflowMode is OverflowMode.AsNeeded
                    && (DoubleUtil.GreaterThan(currentHorizontalLength, maxHorizontalLength)
                        || DoubleUtil.GreaterThan(currentVerticalLength, maxVerticalLength)))
                {
                    // Flag to stop measuring further items
                    hasOverflowItems = true;
                }
            }

            if (hasOverflowItems
                || OverflowMode is OverflowMode.Always)
            {
                SetIsOverflowItem(childContainer, BooleanBoxes.TrueBox);
                if (generator is not null)
                {
                    int itemIndex = generator.IndexFromContainer(childContainer);
                    if (itemIndex >= 0)
                    {
                        object item = generator.ItemFromContainer(childContainer);
                        overflowItemsList.Add((itemIndex, item));
                    }
                }

                continue;
            }

            //childContainer.SetCurrentValue(WidthProperty, newUniformChildSize.Width);
            //childContainer.SetCurrentValue(HeightProperty, newUniformChildSize.Height);
            //Debug.WriteLine($"Measuring item at index {generator?.IndexFromContainer(childContainer) ?? -1} with finalDesiredPanelSize '{newUniformChildSize}' and forced to Size: '{new Size(childContainer.Width, childContainer.Height)}'");
        }

        newPanelSize = new Size(currentHorizontalLength, currentVerticalLength);
        //_isModifyingChildLayout = false;

        return true;
    }

    /// <summary>
    /// Measure the content and store the desired finalDesiredPanelSize of the content
    /// </summary>
    /// <param name="availableSize"></param>
    /// <returns></returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (InternalChildren.Count == 0)
        {
            return base.MeasureOverride(availableSize);
        }

        //if (availableSize == new Size(double.PositiveInfinity, double.PositiveInfinity))
        //{
        //    return base.MeasureOverride(availableSize);
        //}

        if (MeasureItems(availableSize, out ReadOnlyCollection<(int itemIndex, object item)> overflowItems, out Size newUniformChildSize, out Size newPanelSize))
        {
            _measuredUniformSize = newUniformChildSize;
            _measuredRequiredPanelSize = newPanelSize;
            if (overflowItems.Count > 0)
            {
                RaiseEvent(new OverflowDetectedRoutedEventArgs(OverflowDetectedEvent, this, overflowItems));
            }

            return newPanelSize;
        }

        return availableSize;
    }

    /// <summary>
    /// Content arrangement.
    /// </summary>
    /// <param name="finalSize">Arrange finalDesiredPanelSize</param>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count == 0)
        {
            return base.ArrangeOverride(finalSize);
        }

        Rect layoutSlot = new(_measuredUniformSize);
        bool isOrientationHorizontal = Orientation is Orientation.Horizontal;
        Vector offset = isOrientationHorizontal
            ? new Vector(layoutSlot.Width, 0)
            : new Vector(0, layoutSlot.Height);
        layoutSlot.Offset(-offset);

        //
        // Arrange and Position Children.
        //
        foreach (UIElement itemContainer in InternalChildren)
        {
            layoutSlot.Offset(offset);
            itemContainer.Arrange(layoutSlot);
        }

        var finalDesiredPanelSize = new Size(Math.Max(_measuredUniformSize.Width, layoutSlot.Right), Math.Max(_measuredUniformSize.Height, layoutSlot.Bottom));
        return finalDesiredPanelSize;
    }

    #endregion
}

public class OverflowDetectedRoutedEventArgs : RoutedEventArgs
{
    public ReadOnlyCollection<(int itemIndex, object item)> OverflowItems { get; }
    public OverflowDetectedRoutedEventArgs(RoutedEvent routedEvent, object source, ReadOnlyCollection<(int itemIndex, object item)> overflowItems) : base(routedEvent, source) => OverflowItems = overflowItems;
}