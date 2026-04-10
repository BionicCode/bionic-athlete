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
            double.NaN,
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
            double.NaN,
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
    public UniformToolBarPanel() : base() => Loaded += OnLoaded;

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
    }

    private static bool IsWidthHeightValid(object value)
    {
        double v = (double)value;
        return double.IsNaN(v) || (v >= 0.0d && !double.IsPositiveInfinity(v));
    }

    #region Layout

    internal double MinLength
    {
        get;
        private set;
    }

    internal double MaxLength
    {
        get;
        private set;
    }

    private bool MeasureItems(Size constraint, out ReadOnlyCollection<(int itemIndex, object item)> overflowItems, out Size newPanelSize)
    {
        newPanelSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
        var overflowItemsList = new List<(int itemIndex, object item)>();
        overflowItems = new ReadOnlyCollection<(int itemIndex, object item)>(overflowItemsList);
        double maxWidth = 0.0;
        double maxHeight = 0.0;
        bool hasOverflowItems = false;

        if (InternalChildren.Count == 0)
        {
            return false;
        }

        ItemContainerGenerator? generator = null;
        if (IsItemsHost)
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            generator = itemsControl.ItemContainerGenerator;
        }

        if (double.IsNaN(ItemWidth)
            && double.IsNaN(ItemHeight))
        {
            foreach (UIElement childContainer in InternalChildren)
            {
                // First measure pass: allow child to provide its natural desired size
                childContainer.Measure(constraint);
                Size childDesiredSize = childContainer.DesiredSize;
                maxWidth = Math.Max(maxWidth, childDesiredSize.Width);
                maxHeight = Math.Max(maxHeight, childDesiredSize.Height);
            }
        }

        // Enforce ItemWidth if not set to "Auto" (NaN)
        if (!double.IsNaN(ItemWidth))
        {
            maxWidth = ItemWidth;
        }

        // Enforce ItemHeight if not set to "Auto" (NaN)
        if (!double.IsNaN(ItemHeight))
        {
            maxHeight = ItemHeight;
        }

        double maxHorizontalLength = Orientation == Orientation.Horizontal ? constraint.Width : double.PositiveInfinity;
        double maxVerticalLength = Orientation == Orientation.Vertical ? constraint.Height : double.PositiveInfinity;
        double currentHorizontalLength = 0.0;
        double currentVerticalLength = 0.0;
        var requiredChildSize = new Size(maxWidth, maxHeight);
        foreach (UIElement childContainer in InternalChildren)
        {
            // If the total length of the children exceeds the available length in the orientation direction,
            // we can stop measuring and directly mark all remaining containers as overflow items.
            if (!hasOverflowItems)
            {
                // Second measure pass: force child to apply the required size
                childContainer.Measure(requiredChildSize);
                currentHorizontalLength += requiredChildSize.Width;
                currentVerticalLength += requiredChildSize.Height;

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
            }
        }

        newPanelSize = new Size(currentHorizontalLength, currentVerticalLength);

        return true;
    }

    //private bool MeasureGeneratedItems(bool asNeededPass, Size constraint, bool horizontal, double maxExtent, ref Size panelDesiredSize, out double overflowExtent)
    //{
    //    bool sendToOverflow = false; // Becomes true when the first AsNeeded does not fit
    //    bool hasOverflowItems = false;
    //    bool overflowNeedsInvalidation = false;
    //    overflowExtent = 0.0;
    //    UIElementCollection children = InternalChildren;
    //    int childrenCount = children.Count;
    //    int childrenIndex = 0;

    //    for (int i = 0; i < _generatedItemsCollection.Count; i++)
    //    {
    //        UIElement child = _generatedItemsCollection[i];
    //        OverflowMode overflowMode = System.Windows.Controls.ToolBar.GetOverflowMode(child);
    //        bool asNeededMode = overflowMode == OverflowMode.AsNeeded;

    //        // MeasureGeneratedItems is called twice to do a complete measure.
    //        // The first pass measures Always and Never items -- items that
    //        // are guaranteed to be or not to be in the overflow menu.
    //        // The second pass measures AsNeeded items and determines whether
    //        // there is enough room for them in the main bar or if they should
    //        // be placed in the overflow menu.
    //        // Check here whether the overflow mode matches a mode we should be
    //        // examining in this pass.
    //        if (asNeededMode == asNeededPass)
    //        {
    //            DependencyObject visualParent = VisualTreeHelper.GetParent(child);

    //            // In non-Always overflow modes, measure for main bar placement.
    //            if ((overflowMode != OverflowMode.Always) && !sendToOverflow)
    //            {
    //                // Children may change their size depending on whether they are in the overflow
    //                // menu or not. Ensure that when we measure, we are using the main bar size.
    //                // If the item goes to overflow, this property will be updated later in this loop
    //                // when it is removed from the visual tree.
    //                UniformToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);
    //                child.Measure(constraint);
    //                Size childDesiredSize = child.DesiredSize;

    //                // If the child is an AsNeeded, check if it fits. If it doesn't then
    //                // this child and all subsequent AsNeeded children should be sent
    //                // to the overflow menu.
    //                if (asNeededMode)
    //                {
    //                    double newExtent;
    //                    if (horizontal)
    //                    {
    //                        newExtent = childDesiredSize.Width + panelDesiredSize.Width;
    //                    }
    //                    else
    //                    {
    //                        newExtent = childDesiredSize.Height + panelDesiredSize.Height;
    //                    }

    //                    if (DoubleUtil.GreaterThan(newExtent, maxExtent))
    //                    {
    //                        // It doesn't fit, send to overflow
    //                        sendToOverflow = true;
    //                    }
    //                }

    //                // The child has been validated as belonging in the main bar.
    //                // Update the panel desired size dimensions, and ensure the child
    //                // is in the main bar's visual tree.
    //                if (!sendToOverflow)
    //                {
    //                    if (horizontal)
    //                    {
    //                        panelDesiredSize.Width += childDesiredSize.Width;
    //                        panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, childDesiredSize.Height);
    //                    }
    //                    else
    //                    {
    //                        panelDesiredSize.Width = Math.Max(panelDesiredSize.Width, childDesiredSize.Width);
    //                        panelDesiredSize.Height += childDesiredSize.Height;
    //                    }

    //                    if (visualParent != this)
    //                    {
    //                        if ((visualParent == overflowPanel) && (overflowPanel != null))
    //                        {
    //                            overflowPanel.Children.Remove(child);
    //                        }

    //                        if (childrenIndex < childrenCount)
    //                        {
    //                            children.Insert(childrenIndex, child);
    //                        }
    //                        else
    //                        {
    //                            _ = children.Add(child);
    //                        }

    //                        childrenCount++;
    //                    }

    //                    Debug.Assert(children[childrenIndex] == child, "InternalChildren is out of sync with _generatedItemsCollection.");
    //                    childrenIndex++;
    //                }
    //            }

    //            // The child should go to the overflow menu
    //            if ((overflowMode == OverflowMode.Always) || sendToOverflow)
    //            {
    //                hasOverflowItems = true;

    //                // If a child is in the overflow menu, we don't want to keep measuring.
    //                // However, we need to calculate the MaxLength as well as set the desired height
    //                // correctly. Thus, we will use the DesiredSize of the child. There is a problem
    //                // that can occur if the child changes size while in the overflow menu and
    //                // was recently displayed. It will be measure clean, yet its DesiredSize
    //                // will not be accurate for the MaxLength calculation.
    //                if (child.IsMeasureValid)
    //                {
    //                    // Set this temporarily in case the size is different while in the overflow area
    //                    UniformToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);
    //                    child.Measure(constraint);
    //                }

    //                // Even when in the overflow, we need two pieces of information:
    //                // 1. We need to continue to track the maximum size of the non-logical direction
    //                //    (i.e. height in horizontal bars). This way, ToolBars with everything in
    //                //    the overflow will still have some height.
    //                // 2. We want to track how much of the space we saved by placing the child in
    //                //    the overflow menu. This is used to calculate MinLength and MaxLength.
    //                Size childDesiredSize = child.DesiredSize;
    //                if (horizontal)
    //                {
    //                    overflowExtent += childDesiredSize.Width;
    //                    panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, childDesiredSize.Height);
    //                }
    //                else
    //                {
    //                    overflowExtent += childDesiredSize.Height;
    //                    panelDesiredSize.Width = Math.Max(panelDesiredSize.Width, childDesiredSize.Width);
    //                }

    //                // Set the flag to indicate that the child is in the overflow menu
    //                UniformToolBar.SetIsOverflowItem(child, BooleanBoxes.TrueBox);

    //                // If the child is in this panel's visual tree, remove it.
    //                if (visualParent == this)
    //                {
    //                    Debug.Assert(children[childrenIndex] == child, "InternalChildren is out of sync with _generatedItemsCollection.");
    //                    children.Remove(child);
    //                    childrenCount--;
    //                    overflowNeedsInvalidation = true;
    //                }
    //                // If the child isnt connected to the visual tree, notify the overflow panel to pick it up.
    //                else if (visualParent == null)
    //                {
    //                    overflowNeedsInvalidation = true;
    //                }
    //            }
    //        }
    //        else
    //        {
    //            // We are not measure this child in this pass. Update the index into the
    //            // visual children collection.
    //            if ((childrenIndex < childrenCount) && (children[childrenIndex] == child))
    //            {
    //                childrenIndex++;
    //            }
    //        }
    //    }

    //    // A child was added to the overflow panel, but since we don't add it
    //    // to the overflow panel's visual collection until that panel's measure
    //    // pass, we need to mark it as measure dirty.
    //    if (overflowNeedsInvalidation && (overflowPanel != null))
    //    {
    //        overflowPanel.InvalidateMeasure();
    //    }

    //    return hasOverflowItems;
    //}

    /// <summary>
    /// Measure the content and store the desired size of the content
    /// </summary>
    /// <param name="availableSize"></param>
    /// <returns></returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (InternalChildren.Count == 0)
        {
            return base.MeasureOverride(availableSize);
        }

        if (availableSize == new Size(double.PositiveInfinity, double.PositiveInfinity))
        {
            return base.MeasureOverride(availableSize);
            double desiredPanelWidth = 0;
            double desiredPanelHeight = 0;
            double maxWidth = 0;
            double maxHeight = 0;
            // If the available size is infinite in both dimensions, we can skip the overflow logic and just measure the children with their natural desired size.
            foreach (UIElement childContainer in InternalChildren)
            {
                childContainer.Measure(availableSize);
                Size childDesiredSize = childContainer.DesiredSize;
                desiredPanelWidth += childDesiredSize.Width;
                desiredPanelHeight += childDesiredSize.Height;
                maxWidth = Math.Max(maxWidth, childDesiredSize.Width);
                maxHeight = Math.Max(maxHeight, childDesiredSize.Height);
            }

            _measuredUniformSize = new Size(double.IsNaN(ItemWidth) ? maxWidth : ItemWidth, double.IsNaN(ItemHeight) ? maxHeight : ItemHeight);
            return new Size(desiredPanelWidth, desiredPanelHeight);
        }

        if (MeasureItems(availableSize, out ReadOnlyCollection<(int itemIndex, object item)> overflowItems, out Size newPanelSize))
        {
            if (overflowItems.Count > 0)
            {
                RaiseEvent(new OverflowDetectedRoutedEventArgs(OverflowDetectedEvent, this, overflowItems));
            }

            return newPanelSize;
        }

        //if (IsItemsHost)
        //{
        //    Size layoutSlotSize = availableSize;
        //    double maxExtent;
        //    bool horizontal = Orientation == Orientation.Horizontal;

        //    if (horizontal)
        //    {
        //        layoutSlotSize.Width = double.PositiveInfinity;
        //        maxExtent = availableSize.Width;
        //    }
        //    else
        //    {
        //        layoutSlotSize.Height = double.PositiveInfinity;
        //        maxExtent = availableSize.Height;
        //    }

        //    // This first call will measure all of the non-AsNeeded elements (i.e. we know
        //    // whether they're going into the overflow or not.
        //    // overflowExtent will be the size of the Always elements, which is not actually
        //    // needed for subsequent calculations.
        //    bool hasAlwaysOverflowItems = MeasureGeneratedItems(/* asNeeded = */ false, layoutSlotSize, horizontal, maxExtent, ref stackDesiredSize, out _);

        //    // At this point, the desired size is the minimum size of the ToolBar.
        //    MinLength = horizontal ? stackDesiredSize.Width : stackDesiredSize.Height;

        //    // This second call will measure all of the AsNeeded elements and place
        //    // them in the appropriate location.
        //    bool hasAsNeededOverflowItems = MeasureGeneratedItems(/* asNeeded = */ true, layoutSlotSize, horizontal, maxExtent, ref stackDesiredSize, out double overflowExtent);

        //    // At this point, the desired size is complete. The desired size plus overflowExtent
        //    // is the maximum size of the ToolBar.
        //    MaxLength = (horizontal ? stackDesiredSize.Width : stackDesiredSize.Height) + overflowExtent;

        //    UniformToolBar? toolbar = ToolBar;
        //    toolbar?.SetValue(UniformToolBar.HasOverflowItemsPropertyKey, hasAlwaysOverflowItems || hasAsNeededOverflowItems);
        //}
        //else
        //{
        //    stackDesiredSize = base.MeasureOverride(availableSize);
        //}

        return availableSize;
    }

    /// <summary>
    /// Content arrangement.
    /// </summary>
    /// <param name="finalSize">Arrange size</param>
    protected override Size ArrangeOverride(Size finalSize)
    {
        bool isOrientationHorizontal = Orientation is Orientation.Horizontal;
        Rect layoutSlot = new(_measuredUniformSize);

        //
        // Arrange and Position Children.
        //
        foreach (UIElement itemContainer in InternalChildren)
        {
            itemContainer.Arrange(layoutSlot);

            if (isOrientationHorizontal)
            {
                layoutSlot.Offset(_measuredUniformSize.Width, 0);
            }
            else
            {
                layoutSlot.Offset(0, _measuredUniformSize.Height);
            }
        }

        return new Size(Math.Max(_measuredUniformSize.Width, layoutSlot.Right), Math.Max(_measuredUniformSize.Height, layoutSlot.Bottom));
    }

    /// <summary>
    /// ToolBarPanel sets bindings a on its Orientation property to its TemplatedParent if the
    /// property is not already set.
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

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

        //if (TemplatedParent is ToolBar && isOrientationDefaultValue)
        //{
        //    var binding = new Binding
        //    {
        //        RelativeSource = RelativeSource.TemplatedParent,
        //        Path = new PropertyPath(System.Windows.Controls.ToolBar.OrientationProperty)
        //    };
        //    _ = SetBinding(OrientationProperty, binding);
        //}
    }

    #endregion
}

public class OverflowDetectedRoutedEventArgs : RoutedEventArgs
{
    public ReadOnlyCollection<(int itemIndex, object item)> OverflowItems { get; }
    public OverflowDetectedRoutedEventArgs(RoutedEvent routedEvent, object source, ReadOnlyCollection<(int itemIndex, object item)> overflowItems) : base(routedEvent, source) => OverflowItems = overflowItems;
}