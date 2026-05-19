namespace BionicAthlete.Presentation;

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BionicCode.Utilities.Net;

public class UniformToolBar : HeaderedItemsControl
{
    //private ItemsControl? _mainItemsHost;
    //private ItemsControl? _overflowPanelHost;
    private Size _toolStripSize;
    private readonly ToolBarOverflowPanel? _toolBarOverflowPanel;
    private readonly Dictionary<FrameworkElement, Size> _originalDesiredSizes = [];
    //private readonly LayoutPlan _plan;
    //private Size _lastMeasureConstraint;
    //private Panel? _mainPanel;

    public static ComponentResourceKey ToolBarPanelTemplateName { get; } = new ComponentResourceKey(typeof(UniformToolBar), "PART_UniformToolBarPanel");
    public static ComponentResourceKey ToolBarOverflowPanelTemplateName { get; } = new ComponentResourceKey(typeof(UniformToolBar), "PART_UniformToolBarOverflowPanel");
    public static ComponentResourceKey UniformToolBarMainPanelStyleKey { get; } = new ComponentResourceKey(typeof(UniformToolBar), "UniformToolBarMainPanelStyleKey");
    public static ComponentResourceKey UniformToolBarOverflowPanelStyleKey { get; } = new ComponentResourceKey(typeof(UniformToolBar), "UniformToolBarOverflowPanelStyleKey");

    #region Dependency properties

    #region ItemHeight
    [TypeConverter(typeof(LengthConverter))]
    /// <summary>
    /// The uniform item height. If not set, the height of the tallest item will be used as the uniform height.
    /// </summary>
    /// <value>The uniform item height. The default is <see cref="double.NaN"/>, which means the height of the tallest item will be used.</value>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    /// <summary>
    /// The uniform item height. If not set, the height of the tallest item will be used as the uniform height.
    /// </summary>
    /// <value>The uniform item height. The default is <see cref="double.NaN"/>, which means the height of the tallest item will be used.</value>
    public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
        nameof(ItemHeight),
        typeof(double),
        typeof(UniformToolBar),
        new FrameworkPropertyMetadata(
            double.NaN,
            FrameworkPropertyMetadataOptions.AffectsMeasure, OnItemHeightChanged),
        new ValidateValueCallback(IsWidthHeightValid));
    #endregion ItemHeight

    #region ItemWidth
    [TypeConverter(typeof(LengthConverter))]
    /// <summary>
    /// The uniform item width. If not set, the width of the widest item will be used as the uniform width.
    /// </summary>
    /// <value>The uniform item width. The default is <see cref="double.NaN"/>, which means the width of the widest item will be used.</value>
    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    /// <summary>
    /// The uniform item width. If not set, the width of the widest item will be used as the uniform width.
    /// </summary>
    /// <value>The uniform item width. The default is <see cref="double.NaN"/>, which means the width of the widest item will be used.</value>
    public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
        nameof(ItemWidth),
        typeof(double),
        typeof(UniformToolBar),
        new FrameworkPropertyMetadata(
            double.NaN,
            FrameworkPropertyMetadataOptions.AffectsMeasure, OnItemWidthChanged),
        new ValidateValueCallback(IsWidthHeightValid));
    #endregion ItemWidth 

    #region UniformSize

    /// <summary>
    ///     The key needed set a read-only property.
    /// </summary>
    public static readonly DependencyProperty UniformSizeProperty =
            DependencyProperty.Register(
                    "UniformSize",
                    typeof(Size),
                    typeof(UniformToolBar),
                    new FrameworkPropertyMetadata(new Size(double.NaN, double.NaN)));

    /// <summary>
    /// The uniform item newUniformSize as provided by <see cref="ItemHeight"/> and <see cref="ItemWidth"/>.
    /// </summary>
    public Size UniformSize
    {
        get => (Size)GetValue(UniformSizeProperty);

        internal set => SetValue(UniformSizeProperty, value);
    }
    #endregion UniformSize

    #region HasOverflowItems

    /// <summary>
    ///     The key needed set a read-only property.
    /// </summary>
    protected static readonly DependencyPropertyKey HasOverflowItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                    "HasOverflowItems",
                    typeof(bool),
                    typeof(UniformToolBar),
                    new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

    /// <summary>
    ///     The DependencyProperty for the HasOverflowItems property.
    ///     Flags:              None
    ///     Default Name:      false
    /// </summary>
    public static readonly DependencyProperty HasOverflowItemsProperty =
            HasOverflowItemsPropertyKey.DependencyProperty;

    /// <summary>
    /// Whether we have overflow items
    /// </summary>
    public bool HasOverflowItems
    {
        get => (bool)GetValue(HasOverflowItemsProperty);

        protected set => SetValue(HasOverflowItemsPropertyKey, BooleanBoxes.Box(value));
    }
    #endregion HasOverflowItems

    #region Orientation
    /// <summary>
    /// Gets or sets the orientation of the <see cref="UniformToolBar"/>. 
    /// This determines whether the toolbar items are arranged horizontally or vertically.
    /// </summary>
    /// <value>The orientation of the <see cref="UniformToolBar"/>. The default is <see cref="Orientation.Horizontal"/>.</value>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// Gets or sets the orientation of the <see cref="UniformToolBar"/>. 
    /// This determines whether the toolbar items are arranged horizontally or vertically.
    /// </summary>
    /// <value>The orientation of the <see cref="UniformToolBar"/>. The default is <see cref="Orientation.Horizontal"/>.</value>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(UniformToolBar),
        new PropertyMetadata(Orientation.Horizontal));
    #endregion Orientation

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
    /// <value>The overflow mode of the <see cref="UniformToolBar"/>. The default is <see cref="OverflowMode.AsNeeded"/>.</value>
    public static readonly DependencyProperty OverflowModeProperty = DependencyProperty.Register(
        nameof(OverflowMode),
        typeof(OverflowMode),
        typeof(UniformToolBar),
        new PropertyMetadata(OverflowMode.AsNeeded));
    #endregion OverflowMode

    #region IsOverflowOpen
    /// <summary>
    /// Gets or sets a value indicating whether the overflow panel of the <see cref="UniformToolBar"/> is open.
    /// </summary>
    /// <value><see langword="true"/> if the overflow panel is open; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
    public bool IsOverflowOpen
    {
        get => (bool)GetValue(IsOverflowOpenProperty);
        set => SetValue(IsOverflowOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the overflow panel of the <see cref="UniformToolBar"/> is open.
    /// </summary>
    /// <value><see langword="true"/> if the overflow panel is open; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
    public static readonly DependencyProperty IsOverflowOpenProperty = DependencyProperty.Register(
        nameof(IsOverflowOpen),
        typeof(bool),
        typeof(UniformToolBar),
        new PropertyMetadata(BooleanBoxes.FalseBox, OnIsOverflowOpenChanged));
    #endregion IsOverflowOpen

    #region VisibleItems
    /// <summary>
    /// Gets or sets the collection of visible items in the <see cref="UniformToolBar"/>.
    /// </summary>
    /// <remarks>The default collection is an empty <see cref="ObservableCollection{T}"/>. It's highly recommended to call <see cref="Collection{T}.Clear"/> instead of reassigning new collection instances. Performance can be improved by reusing the existing collection.
    /// </remarks>
    /// <value>The collection of visible items. The default is an empty collection.</value>
    public IList VisibleItems
    {
        get => (IList)GetValue(VisibleItemsProperty);
        protected set => SetValue(VisibleItemsPropertyKey, value);
    }

    /// <summary>
    /// Identifies the read-only dependency property key for the collection of items that are visible in the <see cref="UniformToolBar"/>.
    /// </summary>
    /// <remarks>This key is used to set the value of the <see cref="VisibleItems"/> dependency property internally.
    /// External code should use the corresponding dependency property for read-only access to the visible items
    /// collection.
    /// <para/>The default collection is an empty <see cref="ObservableCollection{T}"/>. It's highly recommended to call <see cref="Collection{T}.Clear"/> instead of reassigning new collection instances. Performance can be improved by reusing the existing collection.
    /// </remarks>
    protected static readonly DependencyPropertyKey VisibleItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(VisibleItems),
        typeof(IList),
        typeof(UniformToolBar),
        new PropertyMetadata(new ObservableCollection<object>()));

    /// <summary>
    /// Gets or sets the collection of visible items in the <see cref="UniformToolBar"/>.
    /// </summary>
    /// <value>The collection of visible items. The default is an empty collection.</value>
    public static readonly DependencyProperty VisibleItemsProperty = VisibleItemsPropertyKey.DependencyProperty;
    #endregion VisibleItems

    #region OverflowItems
    /// <summary>
    /// Gets or sets the collection of visible items in the <see cref="UniformToolBar"/>.
    /// </summary>
    /// <remarks>The default collection is an empty <see cref="ObservableCollection{T}"/>. It's highly recommended to call <see cref="Collection{T}.Clear"/> instead of reassigning new collection instances. Performance can be improved by reusing the existing collection.
    /// </remarks>
    /// <value>The collection of visible items. The default is an empty collection.</value>
    public IList OverflowItems
    {
        get => (IList)GetValue(OverflowItemsProperty);
        protected set => SetValue(OverflowItemsPropertyKey, value);
    }

    /// <summary>
    /// Identifies the read-only dependency property key for the collection of items that overflow the visible area of
    /// the <see cref="UniformToolBar"/>.
    /// </summary>
    /// <remarks>This key is used to set the value of the <see cref="OverflowItems"/> dependency property internally.
    /// External code should use the corresponding dependency property for read-only access to the overflow items
    /// collection.
    /// <para/>The default collection is an empty <see cref="ObservableCollection{T}"/>. It's highly recommended to call <see cref="Collection{T}.Clear"/> instead of reassigning new collection instances. Performance can be improved by reusing the existing collection.
    /// </remarks>
    protected static readonly DependencyPropertyKey OverflowItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(OverflowItems),
        typeof(IList),
        typeof(UniformToolBar),
        new PropertyMetadata(new ObservableCollection<object>()));

    /// <summary>
    /// Gets or sets the collection of overflow items in the <see cref="UniformToolBar"/>.
    /// </summary>
    /// <remarks>The default collection is an empty <see cref="ObservableCollection{T}"/>. It's highly recommended to call <see cref="Collection{T}.Clear"/> instead of reassigning new collection instances. Performance can be improved by reusing the existing collection.
    /// </remarks>
    /// <value>The collection of overflow items. The default is an empty collection.</value>
    public static readonly DependencyProperty OverflowItemsProperty = OverflowItemsPropertyKey.DependencyProperty;
    #endregion OverflowItems

    #endregion Dependency properties

    #region Attached properties

    #endregion Attached properties

    private static bool IsWidthHeightValid(object value)
    {
        double v = (double)value;
        return double.IsNaN(v) || (v >= 0.0d && !double.IsPositiveInfinity(v));
    }

    private static void OnItemWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UniformToolBar)d).OnItemWidthChanged((double)e.OldValue, (double)e.NewValue);

    private static void OnItemHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UniformToolBar)d).OnItemHeightChanged((double)e.OldValue, (double)e.NewValue);

    private static void OnIsOverflowOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UniformToolBar)d).OnIsOverflowOpenChanged((bool)e.OldValue, (bool)e.NewValue);
    protected virtual void OnIsOverflowOpenChanged(bool oldValue, bool newValue)
    {
    }

    public UniformToolBar()
    {
        _toolStripSize = Size.Empty;
        //Loaded += OnLoaded;
        //_currentUniformSize = new Size(ItemWidth, ItemHeight);
        //AddHandler(ToolBarButton.UniformToolBarItemSizeChangedEvent, new EventHandler<UniformToolBarItemSizeChangedEventArgs>(OnUniformToolBarItemSizeChanged!));
        AddHandler(UniformToolBarPanel.LayoutChangedEvent, new EventHandler<LayoutChangedRoutedEventArgs>(OnOverflowChanged));
    }

    protected virtual void OnItemWidthChanged(double oldWidth, double newWidth) => SetCurrentValue(UniformSizeProperty, new Size(newWidth, ItemHeight));

    protected virtual void OnItemHeightChanged(double oldHeight, double newHeight) => SetCurrentValue(UniformSizeProperty, new Size(ItemWidth, newHeight));

    private void OnOverflowChanged(object? sender, LayoutChangedRoutedEventArgs e) => UpdateOverflowItems(e.LayoutResult);

    private void UpdateOverflowItems(UniformToolBarLayoutResult layoutResult)
    {
        OverflowItems.Clear();

        if (!layoutResult.IsValid)
        {
            return;
        }

        SetCurrentValue(UniformSizeProperty, layoutResult.UniformSize);
        HasOverflowItems = layoutResult.HasOverflowItems;

        if (!HasOverflowItems)
        {
            return;
        }

        for (int index = layoutResult.VisibleCount; index < Items.Count; index++)
        {
            object item = Items[index];
            _ = OverflowItems.Add(item);
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) => base.OnRenderSizeChanged(sizeInfo);//InvalidateMeasure();

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);

        VisibleItems.Clear();
        OverflowItems.Clear();

        RegisterNewItems(newValue);
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(e);

        base.OnItemsChanged(e);
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null
                    && e.NewItems.Count > 0)
                {
                    RegisterNewItems(e.NewItems);
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null
                    && e.OldItems.Count > 0)
                {
                    UnregisterOldItems(e.OldItems);
                }

                break;
            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems is not null
                    && e.OldItems.Count > 0)
                {
                    UnregisterOldItems(e.OldItems);
                }

                if (e.NewItems is not null
                    && e.NewItems.Count > 0)
                {
                    RegisterNewItems(e.NewItems);
                }

                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                VisibleItems.Clear();
                OverflowItems.Clear();

                RegisterNewItems(Items);

                break;
        }
    }

    private void UnregisterOldItems(IEnumerable oldItems)
    {
        foreach (object item in oldItems)
        {
            VisibleItems.Remove(item);
            OverflowItems.Remove(item);
        }
    }

    private void RegisterNewItems(IEnumerable newItems)
    {
        if (newItems is null)
        {
            return;
        }

        foreach (object item in newItems)
        {
            _ = VisibleItems.Add(item);
        }
    }

    //public override void OnApplyTemplate()
    //{
    //    base.OnApplyTemplate();
    //    _mainItemsHost = GetTemplateChild("MainPanelHost") as ItemsControl;

    //    if (TryFindVisualChild(this, "OverflowPanelHost", out ItemsControl overflowPanelHost))
    //    {
    //        _overflowPanelHost = overflowPanelHost;
    //    }
    //}

    private bool TryFindVisualChild<TChild>(DependencyObject parent, string name, [NotNullWhen(true)] out TChild? child) where TChild : DependencyObject
    {
        child = null;
        if (parent is null)
        {
            return false;
        }

        if (typeof(TChild) != typeof(Popup)
            && parent is Popup popup)
        {
            return TryFindVisualChild(popup.Child, name, out child);
        }

        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            DependencyObject currentChild = VisualTreeHelper.GetChild(parent, index);
            if (currentChild is TChild foundChild)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (currentChild is FrameworkElement frameworkElement
                        && frameworkElement.Name.Equals(name, StringComparison.Ordinal))
                    {
                        child = foundChild;
                        return true;
                    }

                    continue;
                }

                child = foundChild;
                return true;
            }

            if (TryFindVisualChild(currentChild, name, out child))
            {
                return true;
            }
        }

        return false;
    }

    //protected override Size MeasureOverride(Size constraint)
    //{
    //    Size desiredSize = base.MeasureOverride(constraint);

    //    _mainItemsHost.Measure(constraint);

    //    _ = Orientation == Orientation.Horizontal
    //        ? _mainItemsHost.DesiredSize.Width
    //        : _mainItemsHost.DesiredSize.Height;

    //    double currentHorizontalLength = 0.0;
    //    double currentVerticalLength = 0.0;
    //    bool isOrientationHorizontal = Orientation is Orientation.Horizontal;
    //    double maxWidth = 0.0;
    //    double maxHeight = 0.0;
    //    bool hasOverflowingItems = false;

    //    List<UIElement> measuredChildContainers = [];
    //    bool isCalculateUniformSizeRequired = double.IsNaN(ItemWidth)
    //        || double.IsNaN(ItemHeight);
    //    if (isCalculateUniformSizeRequired)
    //    {
    //        for (int index = 0; index < Items.Count; index++)
    //        {
    //            if (_mainItemsHost.ItemContainerGenerator.ContainerFromIndex(index) is not UIElement childContainer)
    //            {
    //                continue;
    //            }

    //            measuredChildContainers.Add(childContainer);

    //            // First measure pass: allow child to provide its natural desired finalDesiredPanelSize
    //            childContainer.Measure(constraint);
    //            Size childDesiredSize = childContainer.DesiredSize;
    //            maxWidth = Math.Max(maxWidth, childDesiredSize.Width);
    //            maxHeight = Math.Max(maxHeight, childDesiredSize.Height);
    //        }

    //        // Enforce ItemWidth if not set to "Auto" (NaN)
    //        double itemWidth = double.IsNaN(ItemWidth)
    //            ? maxWidth
    //            : ItemWidth;

    //        // Enforce ItemHeight if not set to "Auto" (NaN)
    //        double itemHeight = double.IsNaN(ItemHeight)
    //            ? maxHeight
    //            : ItemHeight;

    //        var newUniformSize = new Size(itemWidth, itemHeight);
    //        if (UniformSize != newUniformSize)
    //        {
    //            SetCurrentValue(UniformSizeProperty, newUniformSize);
    //        }
    //    }

    //    VisibleItems.Clear();
    //    OverflowItems.Clear();
    //    for (int index = 0; index < Items.Count; index++)
    //    {
    //        UIElement childContainer;
    //        if (measuredChildContainers.Count > 0)
    //        {
    //            childContainer = measuredChildContainers[index];
    //        }
    //        else if (ItemContainerGenerator.ContainerFromIndex(index) is UIElement container)
    //        {
    //            childContainer = container;
    //        }
    //        else
    //        {
    //            continue;
    //        }

    //        if (hasOverflowingItems)
    //        {
    //            _ = OverflowItems.Add(Items[index]);

    //            continue;
    //        }

    //        Size desiredToolbarItemSize = childContainer.DesiredSize;
    //        if (isOrientationHorizontal)
    //        {
    //            currentHorizontalLength += desiredToolbarItemSize.Width;
    //            currentVerticalLength = Math.Max(currentVerticalLength, desiredToolbarItemSize.Height);
    //        }
    //        else
    //        {
    //            currentVerticalLength += desiredSize.Height;
    //            currentHorizontalLength = Math.Max(currentHorizontalLength, desiredSize.Width);
    //        }

    //        if (DoubleUtil.GreaterThan(currentHorizontalLength, _mainItemsHost.DesiredSize.Width)
    //            || DoubleUtil.GreaterThan(currentVerticalLength, _mainItemsHost.DesiredSize.Height))
    //        {
    //            hasOverflowingItems = true;
    //            _ = OverflowItems.Add(Items[index]);
    //        }
    //        else
    //        {
    //            _ = VisibleItems.Add(Items[index]);
    //        }
    //    }

    //    return desiredSize;
    //}

    //protected override void OnChildDesiredSizeChanged(UIElement child) => base.OnChildDesiredSizeChanged(child);//_ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);

    //protected override void OnMouseEnter(MouseEventArgs e)
    //{
    //    base.OnMouseEnter(e);
    //    _ = Items.Add(new ToolBarButton { LabelText = "New Button with extra long label to stretch all existing items" });
    //}

    //protected override void OnMouseLeave(MouseEventArgs e)
    //{
    //    base.OnMouseLeave(e);
    //    Items.RemoveAt(Items.Count - 1);
    //}

    //private void OnLoaded(object sender, RoutedEventArgs e) { }
    //if (TryFindVisualChild(_mainItemsHost, out ItemsPresenter? itemsPresenter))
    //{
    //    if (TryFindVisualChild(itemsPresenter, out Panel? mainPanel))
    //    {
    //        _mainPanel = mainPanel;
    //    }
    //}

    //InvalidateMeasure();//_mainPanel?.InvalidateMeasure();//foreach (object? item in Items)//{//    _ = VisibleItems.Add(item);//}//_ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);

    //private void ApplyUniformSizing()
    //{
    //    //IEnumerable<FrameworkElement> frameworkElementsOfHost = _itemsHost.Children
    //    //    .OfType<FrameworkElement>()
    //    //    .Where(element => element is not Separator);

    //    bool hasChanges = HasContentChildSizeChanged(out List<FrameworkElement>? targetElements);
    //    if (hasChanges)
    //    {
    //        foreach (FrameworkElement element in targetElements)
    //        {
    //            element.MinWidth = _currentUniformSize.Width;
    //            element.MaxWidth = _currentUniformSize.Width;
    //            element.MinHeight = _currentUniformSize.Height;
    //            element.MaxHeight = _currentUniformSize.Height;
    //        }
    //    }
    //}

    //private bool HasContentChildSizeChanged(out List<FrameworkElement> targetElements)
    //{
    //    targetElements = [];

    //    if (_itemsHost is null
    //        || _itemsHost.Children.Count == 0)
    //    {
    //        return false;
    //    }

    //    IEnumerable<FrameworkElement> frameworkElementsOfHost = _itemsHost.Children.OfType<FrameworkElement>();
    //    double maxWidth = 0;
    //    double maxHeight = 0;
    //    List<FrameworkElement> candidates = [];
    //    foreach (FrameworkElement frameworkElement in frameworkElementsOfHost)
    //    {
    //        if (frameworkElement is Separator)
    //        {
    //            continue;
    //        }

    //        if (!_originalDesiredSizes.TryGetValue(frameworkElement, out Size originalDesiredSize))
    //        {
    //            originalDesiredSize = frameworkElement.DesiredSize;
    //            _originalDesiredSizes[frameworkElement] = originalDesiredSize;
    //        }

    //        maxHeight = Math.Max(maxHeight, originalDesiredSize.Height);
    //        maxWidth = Math.Max(maxWidth, originalDesiredSize.Width);
    //        candidates.Add(frameworkElement);
    //    }

    //    bool hasSizeChanged = maxWidth != _currentUniformSize.Width
    //        || maxHeight != _currentUniformSize.Height;
    //    if (hasSizeChanged)
    //    {
    //        double newWith = double.IsNaN(ItemWidth)
    //            ? maxWidth
    //            : Math.Max(maxWidth, ItemWidth);
    //        double newHeight = double.IsNaN(ItemHeight)
    //            ? maxHeight
    //            : Math.Max(maxHeight, ItemHeight);
    //        _currentUniformSize = new Size(newWith, newHeight);

    //        // Perform a second pass over collected candidates to filter out any elements
    //        // that may already have the new uniform newUniformSize, so we don't unnecessarily update them
    //        // and cause extra layout passes.
    //        foreach (FrameworkElement element in candidates)
    //        {
    //            if (element.RenderSize != _currentUniformSize)
    //            {
    //                targetElements.Add(element);
    //            }
    //        }
    //    }

    //    return hasSizeChanged;
    //}

    private void PurgeTable(IList? items)
    {
        if (items is null
            || items.Count == 0)
        {
            return;
        }

        foreach (object oldItem in items)
        {
            if (oldItem is FrameworkElement frameworkElement)
            {
                _ = _originalDesiredSizes.Remove(frameworkElement);
            }
        }
    }

    protected sealed class LayoutPlan
    {
        public Size UniformItemSize { get; init; }
        public int VisibleCount { get; init; }
        public double LogicalMainLength { get; init; }
        public required IReadOnlyList<UIElement> VisibleChildren { get; init; }
        public required IReadOnlyList<Rect> Slots { get; init; }
    }
}
