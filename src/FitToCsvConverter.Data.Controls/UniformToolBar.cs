namespace FitToCsvConverter.Controls;

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using BionicCode.Utilities.Net;

public class UniformToolBar : HeaderedItemsControl
{
    private readonly ToolBarPanel? _itemsHost;
    private Size _currentUniformSize;
    private readonly ToolBarOverflowPanel? _toolBarOverflowPanel;
    private readonly Dictionary<FrameworkElement, Size> _originalDesiredSizes = [];
    private readonly LayoutPlan _plan;
    private Size _lastMeasureConstraint;

    public static ComponentResourceKey ToolBarPanelTemplateName { get; } = new ComponentResourceKey(typeof(UniformToolBar), "PART_UniformToolBarPanel");
    public static ComponentResourceKey ToolBarOverflowPanelTemplateName { get; } = new ComponentResourceKey(typeof(UniformToolBar), "PART_UniformToolBarOverflowPanel");
    public static ComponentResourceKey UniformToolBarMainPanelStyleKey { get; } = new ComponentResourceKey(typeof(UniformToolBar), "UniformToolBarMainPanelStyleKey");
    public static ComponentResourceKey UniformToolBarOverflowPanelStyleKey { get; } = new ComponentResourceKey(typeof(UniformToolBar), "UniformToolBarOverflowPanelStyleKey");

    #region Dependency properties

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
        typeof(UniformToolBar),
        new PropertyMetadata(24d, OnItemHeightChanged));
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
        typeof(UniformToolBar),
        new PropertyMetadata(24d, OnItemWidthChanged));
    #endregion ItemWidth 

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
    ///     Default Value:      false
    /// </summary>
    public static readonly DependencyProperty HasOverflowItemsProperty =
            HasOverflowItemsPropertyKey.DependencyProperty;

    /// <summary>
    /// Whether we have overflow items
    /// </summary>
    public bool HasOverflowItems => (bool)GetValue(HasOverflowItemsProperty);
    #endregion HasOverflowItems

    #region MainPanel
    /// <summary>
    /// Gets or sets the main <see cref="Panel"/> of the <see cref="UniformToolBar"/>. 
    /// This is the panel that contains the toolbar items and is responsible for arranging them.
    /// </summary>
    /// <value>The main <see cref="Panel"/> of the <see cref="UniformToolBar"/>. The default is a <see cref="UniformToolBarPanel"/>.</value>
    public Panel MainPanel
    {
        get => (Panel)GetValue(MainPanelProperty);
        set => SetValue(MainPanelProperty, value);
    }

    /// <summary>
    /// Gets or sets the main <see cref="Panel"/> of the <see cref="UniformToolBar"/>. 
    /// This is the panel that contains the toolbar items and is responsible for arranging them.
    /// </summary>
    /// <value>The main <see cref="Panel"/> of the <see cref="UniformToolBar"/>. The default is a <see cref="UniformToolBarPanel"/>.</value>
    public static readonly DependencyProperty MainPanelProperty = DependencyProperty.Register(
        nameof(MainPanel),
        typeof(Panel),
        typeof(UniformToolBar),
        new PropertyMetadata(new UniformToolBarPanel()));
    #endregion MainPanel

    #region OverflowPanel
    /// <summary>
    /// Gets or sets the main <see cref="Panel"/> of the <see cref="UniformToolBar"/>. 
    /// This is the panel that contains the toolbar items and is responsible for arranging them.
    /// </summary>
    /// <value>The main <see cref="Panel"/> of the <see cref="UniformToolBar"/>. The default is a <see cref="UniformToolBarPanel"/>.</value>
    public Panel OverflowPanel
    {
        get => (Panel)GetValue(OverflowPanelProperty);
        set => SetValue(OverflowPanelProperty, value);
    }

    /// <summary>
    /// Gets or sets the main <see cref="Panel"/> of the <see cref="UniformToolBar"/>. 
    /// This is the panel that contains the toolbar items and is responsible for arranging them.
    /// </summary>
    /// <value>The overflow <see cref="Panel"/> of the <see cref="UniformToolBar"/>. The default is a <see cref="UniformToolBarOverflowPanel"/>.</value>
    public static readonly DependencyProperty OverflowPanelProperty = DependencyProperty.Register(
        nameof(OverflowPanel),
        typeof(Panel),
        typeof(UniformToolBar),
        new PropertyMetadata(new UniformToolBarOverflowPanel()));
    #endregion OverflowPanel

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
        new PropertyMetadata(BooleanBoxes.FalseBox));
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

    #region IsOverflowItem
    /// <summary>
    ///     The key needed set a read-only property.
    /// Attached property to indicate if the item is placed in the overflow panel
    /// </summary>
    protected static readonly DependencyPropertyKey IsOverflowItemPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                    "IsOverflowItem",
                    typeof(bool),
                    typeof(UniformToolBar),
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

    #endregion Attached properties

    /// <summary>
    /// Gets reference to ToolBar's ToolBarOverflowPanel element.
    /// </summary>
    internal ToolBarOverflowPanel? ToolBarOverflowPanel => _toolBarOverflowPanel;

    private static void OnItemHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var uniformToolBar = (UniformToolBar)d;
        uniformToolBar._currentUniformSize = new Size(uniformToolBar._currentUniformSize.Width, (double)e.NewValue);
        _ = uniformToolBar.Dispatcher.InvokeAsync(uniformToolBar.ApplyUniformSizing, DispatcherPriority.Render);
    }

    private static void OnItemWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var uniformToolBar = (UniformToolBar)d;
        uniformToolBar._currentUniformSize = new Size((double)e.NewValue, uniformToolBar._currentUniformSize.Height);
        _ = uniformToolBar.Dispatcher.InvokeAsync(uniformToolBar.ApplyUniformSizing, DispatcherPriority.Render);
    }

    public UniformToolBar()
    {
        _currentUniformSize = Size.Empty;
        Loaded += OnLoaded;
        _currentUniformSize = new Size(ItemWidth, ItemHeight);
        AddHandler(ToolBarButton.UniformToolBarItemSizeChangedEvent, new EventHandler<UniformToolBarItemSizeChangedEventArgs>(OnUniformToolBarItemSizeChanged!));
    }

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

                if (e.NewItems is not null
                    && e.NewItems.Count > 0)
                {
                    RegisterNewItems(e.NewItems);
                }

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

    private void OnUniformToolBarItemSizeChanged(object sender, UniformToolBarItemSizeChangedEventArgs e) => _ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);

    public override void OnApplyTemplate() => base.OnApplyTemplate();//_itemsHost = GetTemplateChild(ToolBarPanelTemplateName.) as ToolBarPanel//    ?? // Only thrown if Microsoft .NET source have drastically changed the template for ToolBar, which is unlikely.//       // We throw so we can update the code to match the new template.//       throw new InvalidOperationException("PART_ToolBarPanel not found in official .NET template.");//DependencyObject? panel = GetTemplateChild(ToolBarOverflowPanelTemplateName.ResourceId as string);//if (panel is not null and not System.Windows.Controls.Primitives.ToolBarOverflowPanel)//{//    throw new NotSupportedException("The template part named PART_ToolBarOverflowPanel must be of type ToolBarOverflowPanel.");//}//_toolBarOverflowPanel = panel as ToolBarOverflowPanel;

    protected override Size MeasureOverride(Size constraint)
    {
        _originalDesiredSizes.Clear();
        _ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);
        return base.MeasureOverride(constraint);
    }

    protected override void OnChildDesiredSizeChanged(UIElement child)
    {
        base.OnChildDesiredSizeChanged(child);

        _ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);
    }

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

    private void OnLoaded(object sender, RoutedEventArgs e) => _ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);

    private void ApplyUniformSizing()
    {
        //IEnumerable<FrameworkElement> frameworkElementsOfHost = _itemsHost.Children
        //    .OfType<FrameworkElement>()
        //    .Where(element => element is not Separator);

        bool hasChanges = HasContentChildSizeChanged(out List<FrameworkElement>? targetElements);
        if (hasChanges)
        {
            foreach (FrameworkElement element in targetElements)
            {
                element.MinWidth = _currentUniformSize.Width;
                element.MaxWidth = _currentUniformSize.Width;
                element.MinHeight = _currentUniformSize.Height;
                element.MaxHeight = _currentUniformSize.Height;
            }
        }
    }

    private bool HasContentChildSizeChanged(out List<FrameworkElement> targetElements)
    {
        targetElements = [];

        if (_itemsHost is null
            || _itemsHost.Children.Count == 0)
        {
            return false;
        }

        IEnumerable<FrameworkElement> frameworkElementsOfHost = _itemsHost.Children.OfType<FrameworkElement>();
        double maxWidth = 0;
        double maxHeight = 0;
        List<FrameworkElement> candidates = [];
        foreach (FrameworkElement frameworkElement in frameworkElementsOfHost)
        {
            if (frameworkElement is Separator)
            {
                continue;
            }

            if (!_originalDesiredSizes.TryGetValue(frameworkElement, out Size originalDesiredSize))
            {
                originalDesiredSize = frameworkElement.DesiredSize;
                _originalDesiredSizes[frameworkElement] = originalDesiredSize;
            }

            maxHeight = Math.Max(maxHeight, originalDesiredSize.Height);
            maxWidth = Math.Max(maxWidth, originalDesiredSize.Width);
            candidates.Add(frameworkElement);
        }

        bool hasSizeChanged = maxWidth != _currentUniformSize.Width
            || maxHeight != _currentUniformSize.Height;
        if (hasSizeChanged)
        {
            double newWith = double.IsNaN(ItemWidth)
                ? maxWidth
                : Math.Max(maxWidth, ItemWidth);
            double newHeight = double.IsNaN(ItemHeight)
                ? maxHeight
                : Math.Max(maxHeight, ItemHeight);
            _currentUniformSize = new Size(newWith, newHeight);

            // Perform a second pass over collected candidates to filter out any elements
            // that may already have the new uniform size, so we don't unnecessarily update them
            // and cause extra layout passes.
            foreach (FrameworkElement element in candidates)
            {
                if (element.RenderSize != _currentUniformSize)
                {
                    targetElements.Add(element);
                }
            }
        }

        return hasSizeChanged;
    }

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
        public IReadOnlyList<UIElement> VisibleChildren { get; init; }
        public IReadOnlyList<Rect> Slots { get; init; }
    }
}
