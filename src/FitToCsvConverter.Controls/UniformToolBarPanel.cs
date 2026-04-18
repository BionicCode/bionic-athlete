namespace FitToCsvConverter.Controls;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BionicCode.Utilities.Net;
using global::System.Windows.Data;

public class UniformToolBarPanel : VirtualizingPanel
{
    protected UniformToolBarLayoutResult CurrentLayoutBrief { get; set; }

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

    #region UniformSize
    /// <summary>
    ///     The key needed set a read-only property.
    /// </summary>
    protected static readonly DependencyProperty UniformSizeProperty =
            DependencyProperty.Register(
                    "UniformSize",
                    typeof(Size),
                    typeof(UniformToolBarPanel),
                    new FrameworkPropertyMetadata(new Size(double.NaN, double.NaN), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    /// <summary>
    /// The uniform item size as provided by <see cref="ItemHeight"/> and <see cref="ItemWidth"/>.
    /// </summary>
    public Size UniformSize
    {
        get => (Size)GetValue(UniformSizeProperty);

        internal set => SetValue(UniformSizeProperty, value);
    }
    #endregion UniformSize

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

    #region OverflowChanged event

    public static readonly RoutedEvent LayoutChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(LayoutChanged),
        RoutingStrategy.Bubble,
        typeof(EventHandler<LayoutChangedRoutedEventArgs>),
        typeof(UniformToolBarPanel));

    public event EventHandler<LayoutChangedRoutedEventArgs> LayoutChanged
    {
        add => AddHandler(LayoutChangedEvent, value);
        remove => RemoveHandler(LayoutChangedEvent, value);
    }

    #endregion OverflowChanged event

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

    private bool IsUniformSizeFromUser => !double.IsNaN(ItemWidth) && !double.IsNaN(ItemHeight);

    protected virtual void OnLayoutChanged(UniformToolBarLayoutResult layoutPlan) => RaiseEvent(new LayoutChangedRoutedEventArgs(LayoutChangedEvent, this, layoutPlan));

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "args is a framework callback parameter with non-null contract semantics")]
    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        base.OnItemsChanged(sender, args);

        if (!IsUniformSizeFromUser)
        {
            // User uses "Auto" or double.NaN for ItemWidth/ItemHeight:
            // UniformSize must be recalculated dynamically based on the size of the children, so we need to invalidate measure whenever the items change in any way.
            InvalidateMeasure();

            return;
        }

        if (!CurrentLayoutBrief.HasOverflowItems)
        {
            // No overflow, so we can just invalidate measure to re-layout the children with the new total items count.
            InvalidateMeasure();
            return;
        }

        if (!IsMeasureValid)
        {
            // If measure is not valid, it means a new layout pass will be triggered soon, so we can skip handling the item change here to avoid redundant measure/arrange passes.
            return;
        }

        switch (args.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                int addIndex = ItemContainerGenerator.IndexFromGeneratorPosition(args.Position);
                if (addIndex >= CurrentLayoutBrief.VisibleCount)
                {
                    CurrentLayoutBrief = new UniformToolBarLayoutResult
                    {
                        UniformSize = UniformSize,
                        VisibleCount = CurrentLayoutBrief.VisibleCount,
                        OverflowCount = CurrentLayoutBrief.OverflowCount + 1
                    };
                    OnLayoutChanged(CurrentLayoutBrief);
                }
                else
                {
                    InvalidateMeasure();
                }

                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                int removeIndex = ItemContainerGenerator.IndexFromGeneratorPosition(args.OldPosition);
                if (removeIndex >= CurrentLayoutBrief.VisibleCount)
                {
                    CurrentLayoutBrief = new UniformToolBarLayoutResult
                    {
                        UniformSize = UniformSize,
                        VisibleCount = CurrentLayoutBrief.VisibleCount,
                        OverflowCount = CurrentLayoutBrief.OverflowCount - 1
                    };
                    OnLayoutChanged(CurrentLayoutBrief);
                }
                else
                {
                    InvalidateMeasure();
                }

                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                int changeIndex = ItemContainerGenerator.IndexFromGeneratorPosition(args.Position);
                if (changeIndex >= CurrentLayoutBrief.VisibleCount)
                {
                    CurrentLayoutBrief = new UniformToolBarLayoutResult
                    {
                        UniformSize = UniformSize,
                        VisibleCount = CurrentLayoutBrief.VisibleCount,
                        OverflowCount = CurrentLayoutBrief.OverflowCount
                    };
                    OnLayoutChanged(CurrentLayoutBrief);
                }
                else
                {
                    InvalidateMeasure();
                }

                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                int oldIndex = ItemContainerGenerator.IndexFromGeneratorPosition(args.OldPosition);
                int newIndex = ItemContainerGenerator.IndexFromGeneratorPosition(args.Position);
                if (oldIndex >= CurrentLayoutBrief.VisibleCount
                    && newIndex >= CurrentLayoutBrief.VisibleCount)
                {
                    CurrentLayoutBrief = new UniformToolBarLayoutResult
                    {
                        UniformSize = UniformSize,
                        VisibleCount = CurrentLayoutBrief.VisibleCount,
                        OverflowCount = CurrentLayoutBrief.OverflowCount
                    };
                    OnLayoutChanged(CurrentLayoutBrief);
                }
                else
                {
                    InvalidateMeasure();
                }

                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
            default:
                InvalidateMeasure();
                break;
        }
    }

    #region Layout
    protected virtual IList<ContainerInfo> GenerateChildren(bool isAddGeneratedItemsToPanelRequested)
    {
        if (ItemContainerGenerator is null)
        {
            return [];
        }

        RemoveInternalChildRange(0, InternalChildren.Count);

        using IDisposable generatorScop = ItemContainerGenerator.StartAt(ItemContainerGenerator.GeneratorPositionFromIndex(0), GeneratorDirection.Forward, allowStartAtRealizedItem: true);
        ReadOnlyCollection<object> items = (ItemContainerGenerator as ItemContainerGenerator)?.Items ?? [];
        var generatedItems = new List<ContainerInfo>();
        for (int index = 0; index < items.Count; index++)
        {
            if (ItemContainerGenerator.GenerateNext(out bool isNewlyRealized) is not UIElement child)
            {
                continue;
            }

            if (isNewlyRealized)
            {
                ItemContainerGenerator.PrepareItemContainer(child);
            }

            generatedItems.Add(new ContainerInfo(index, items[index], child));

            if (isAddGeneratedItemsToPanelRequested)
            {
                AddInternalChild(child);
            }
        }

        return generatedItems;
    }

    private bool MeasureItems(
        Size constraint,
        IList<ContainerInfo> generatedItems,
        out UniformToolBarLayoutResult layoutPlan,
        out Size newPanelSize)
    {
        Debug.Assert(InternalChildren.Count == 0, "MeasureItems should be called with an empty InternalChildren collection");

        layoutPlan = UniformToolBarLayoutResult.Empty;
        newPanelSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        if (generatedItems.Count == 0)
        {
            return false;
        }

        double maxWidth = 0.0;
        double maxHeight = 0.0;
        if (!IsUniformSizeFromUser)
        {
            for (int index = generatedItems.Count - 1; index >= 0; index--)
            {
                ContainerInfo containerInfo = generatedItems[index];
                if (containerInfo.Container is not FrameworkElement childContainer)
                {
                    generatedItems.RemoveAt(index);
                    continue;
                }

                // First measure pass: allow child to provide its natural desired finalDesiredPanelSize
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

        double itemWidth = maxWidth;
        double itemHeight = maxHeight;
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
        var newUniformChildSize = new Size(itemWidth, itemHeight);
        int overflowStartIndex = -1;
        bool hasOverflowItems = OverflowMode is OverflowMode.Always;
        foreach (ContainerInfo containerInfo in generatedItems)
        {
            UIElement childContainer = containerInfo.Container;

            // If the total length of the children exceeds the available length in the orientation direction,
            // or 'OverflowMode.Always' is configured
            // we can stop measuring and directly mark all remaining containers as overflow items.
            if (!hasOverflowItems)
            {
                // Second measure pass: force child to apply the required finalDesiredPanelSize
                childContainer.Measure(newUniformChildSize);

                double tempCurrentHorizontalLength = currentHorizontalLength;
                double tempCurrentVerticalLength = currentVerticalLength;
                if (isOrientationHorizontal)
                {
                    tempCurrentHorizontalLength += newUniformChildSize.Width;
                }
                else
                {
                    tempCurrentVerticalLength += newUniformChildSize.Height;
                }

                if (OverflowMode is OverflowMode.AsNeeded
                    && (DoubleUtil.GreaterThan(tempCurrentHorizontalLength, maxHorizontalLength)
                        || DoubleUtil.GreaterThan(tempCurrentVerticalLength, maxVerticalLength)))
                {
                    // Flag to stop measuring further items
                    hasOverflowItems = true;
                }
                else
                {
                    currentHorizontalLength = tempCurrentHorizontalLength;
                    currentVerticalLength = tempCurrentVerticalLength;
                    AddInternalChild(childContainer);
                }
            }

            if (hasOverflowItems)
            {
                overflowStartIndex = containerInfo.ItemIndex;

                break;
            }
        }

        layoutPlan = new UniformToolBarLayoutResult(newUniformChildSize,
            overflowStartIndex != -1
                ? overflowStartIndex
                : generatedItems.Count,
            overflowStartIndex != -1
                ? generatedItems.Count - overflowStartIndex
                : 0);
        newPanelSize = new Size(currentHorizontalLength, currentVerticalLength);

        return true;
    }

    /// <summary>
    /// Measure the content and store the desired finalDesiredPanelSize of the content
    /// </summary>
    /// <param name="availableSize"></param>
    /// <returns></returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        OnLayoutChanged(CurrentLayoutBrief.GetInvalidatedResult());

        IList<ContainerInfo> generatedContainerInfo = GenerateChildren(isAddGeneratedItemsToPanelRequested: false);

        if (MeasureItems(availableSize, generatedContainerInfo, out UniformToolBarLayoutResult layoutPlan, out Size newPanelSize))
        {
            UniformSize = layoutPlan.UniformSize;
            CurrentLayoutBrief = layoutPlan;
            OnLayoutChanged(layoutPlan);

            return newPanelSize;
        }

        return base.MeasureOverride(availableSize);
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

        Rect layoutSlot = new(UniformSize);
        bool isOrientationHorizontal = Orientation is Orientation.Horizontal;
        Vector offset = isOrientationHorizontal
            ? new Vector(layoutSlot.Width, 0)
            : new Vector(0, layoutSlot.Height);
        layoutSlot.Offset(-offset);

        foreach (UIElement itemContainer in InternalChildren)
        {
            layoutSlot.Offset(offset);
            itemContainer.Arrange(layoutSlot);
        }

        var finalRenderedPanelSize = new Size(layoutSlot.Right, layoutSlot.Bottom);
        return finalRenderedPanelSize;
    }

    #endregion
}
