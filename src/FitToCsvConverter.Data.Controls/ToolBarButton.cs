namespace FitToCsvConverter.Controls;

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BionicCode.Utilities.Net;
using global::System.Diagnostics;
using global::System.Windows.Data;

/// <summary>
/// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
///
/// Step 1a) Using this custom control in a XAML file that exists in the current project.
/// Add this XmlNamespace attribute to the root element of the markup file where it is 
/// to be used:
///
///     xmlns:MyNamespace="clr-namespace:FitToCsvConverter.Controls"
///
///
/// Step 1b) Using this custom control in a XAML file that exists in a different project.
/// Add this XmlNamespace attribute to the root element of the markup file where it is 
/// to be used:
///
///     xmlns:MyNamespace="clr-namespace:FitToCsvConverter.Controls;assembly=FitToCsvConverter.Controls"
///
/// You will also need to add a project reference from the project where the XAML file lives
/// to this project and Rebuild to avoid compilation errors:
///
///     Right click on the target project in the Solution Explorer and
///     "Add Reference"->"Projects"->[Browse to and select this project]
///
///
/// Step 2)
/// Go ahead and use your control in the XAML file.
///
///     <MyNamespace:ToolBarButton/>
///
/// </summary>
public class ToolBarButton : Button
{
    private FrameworkElement? _contentHost;
    private TextBlock? _label;
    private Border? _labelBorder;
    private Storyboard? _mouseOverForegroundStoryboard;
    private Storyboard? _revertForegroundStoryboard;
    private readonly double _oldContentHeight;

    public string LabelText { get => (string)GetValue(LabelTextProperty); set => SetValue(LabelTextProperty, value); }

    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(
            nameof(LabelText),
            typeof(string),
            typeof(ToolBarButton),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    public double LabelFontSize { get => (double)GetValue(LabelFontSizeProperty); set => SetValue(LabelFontSizeProperty, value); }

    public static readonly DependencyProperty LabelFontSizeProperty =
        DependencyProperty.Register(
            nameof(LabelFontSize),
            typeof(double),
            typeof(ToolBarButton),
            new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    //public delegate void UniformToolBarItemSizeChangedEventHandler(object sender, UniformToolBarItemSizeChangedEventArgs e);
    public static readonly RoutedEvent UniformToolBarItemSizeChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(UniformToolBarItemSizeChanged),
        RoutingStrategy.Bubble,
        typeof(EventHandler<UniformToolBarItemSizeChangedEventArgs>),
        typeof(ToolBarButton));

    public event EventHandler<UniformToolBarItemSizeChangedEventArgs> UniformToolBarItemSizeChanged
    {
        add => AddHandler(UniformToolBarItemSizeChangedEvent, value);
        remove => RemoveHandler(UniformToolBarItemSizeChangedEvent, value);
    }

    static ToolBarButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarButton), new FrameworkPropertyMetadata(typeof(ToolBarButton)));

    public ToolBarButton()
    {
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => UpdateSizeRelatedResources();

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSizeRelatedResources();
        RaiseEvent(new UniformToolBarItemSizeChangedEventArgs(UniformToolBarItemSizeChangedEvent, this, e));
    }

    //protected override void OnChildDesiredSizeChanged(UIElement child)
    //{
    //    ArgumentNullExceptionAdvanced.ThrowIfNull(child);

    //    base.OnChildDesiredSizeChanged(child);
    //    UpdateSizeRelatedResources();
    //    RaiseEvent(new UniformToolBarItemSizeChangedEventArgs(UniformToolBarItemSizeChangedEvent, this, true, true, child.RenderSize, child.RenderSize));
    //}

    private void UpdateSizeRelatedResources()
    {
        BuildMouseOverStoryboard();
        BuildMouseOverRevertStoryboard();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentHost = GetTemplateChild("PART_ContentPresenter") as FrameworkElement;
        _label = GetTemplateChild("PART_Label") as TextBlock;
        _labelBorder = GetTemplateChild("PART_LabelBorder") as Border;
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        if (_contentHost is not null
            && _label is not null
            && _labelBorder is not null)
        {
            if (_mouseOverForegroundStoryboard is null)
            {
                BuildMouseOverStoryboard();
            }

            _mouseOverForegroundStoryboard?.Begin();
        }

        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if (_contentHost is not null
            && _label is not null
            && _labelBorder is not null)
        {
            if (_revertForegroundStoryboard is null)
            {
                BuildMouseOverRevertStoryboard();
            }

            _revertForegroundStoryboard?.Begin();
        }

        base.OnMouseLeave(e);
    }

    private void BuildMouseOverStoryboard()
    {
        if (_contentHost is not null
            && _label is not null
            && _labelBorder is not null)
        {
            _mouseOverForegroundStoryboard = new Storyboard() { FillBehavior = FillBehavior.HoldEnd };
            var colorAnimation = new ColorAnimation
            {
                To = (Color)FindResource("MouseOverTextColor"),
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(colorAnimation, this);
            Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Button.Foreground).(SolidColorBrush.Color)"));
            _mouseOverForegroundStoryboard.Children.Add(colorAnimation);

            var opacityAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(opacityAnimation, _labelBorder);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            _mouseOverForegroundStoryboard.Children.Add(opacityAnimation);

            var borderHorizontalGrowAnimation = new DoubleAnimation
            {
                From = 0,
                To = _labelBorder.ActualWidth,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(borderHorizontalGrowAnimation, _labelBorder);
            Storyboard.SetTargetProperty(borderHorizontalGrowAnimation, new PropertyPath(WidthProperty));
            _mouseOverForegroundStoryboard.Children.Add(borderHorizontalGrowAnimation);

            var fontSizeAnimation = new DoubleAnimation
            {
                To = LabelFontSize,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(fontSizeAnimation, _label);
            Storyboard.SetTargetProperty(fontSizeAnimation, new PropertyPath(FontSizeProperty));
            _mouseOverForegroundStoryboard.Children.Add(fontSizeAnimation);

            // Shrink to 75 % of original height to create some visual interest and to help indicate that the button is being hovered over
            double newContentHeight = _contentHost.ActualHeight * 0.75;
            var heightAnimation = new DoubleAnimation
            {
                From = _contentHost.ActualHeight,
                To = newContentHeight,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(heightAnimation, _contentHost);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(FrameworkElement.HeightProperty));
            _mouseOverForegroundStoryboard.Children.Add(heightAnimation);
        }
    }

    private void BuildMouseOverRevertStoryboard()
    {
        if (_contentHost is not null
            && _label is not null
            && _labelBorder is not null)
        {
            _revertForegroundStoryboard = new Storyboard() { FillBehavior = FillBehavior.HoldEnd };
            var colorAnimation = new ColorAnimation
            {
                To = (Color)FindResource("TextColor"),
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(colorAnimation, this);
            Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Button.Foreground).(SolidColorBrush.Color)"));
            _revertForegroundStoryboard.Children.Add(colorAnimation);

            var opacityAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(opacityAnimation, _labelBorder);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            _revertForegroundStoryboard.Children.Add(opacityAnimation);

            var borderHorizontalShrinkAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(borderHorizontalShrinkAnimation, _labelBorder);
            Storyboard.SetTargetProperty(borderHorizontalShrinkAnimation, new PropertyPath(WidthProperty));
            _revertForegroundStoryboard.Children.Add(borderHorizontalShrinkAnimation);

            var fontSizeAnimation = new DoubleAnimation
            {
                To = 0.001,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(fontSizeAnimation, _label);
            Storyboard.SetTargetProperty(fontSizeAnimation, new PropertyPath(FontSizeProperty));
            _revertForegroundStoryboard.Children.Add(fontSizeAnimation);

            var heightAnimation = new DoubleAnimation
            {
                To = _contentHost.ActualHeight,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(heightAnimation, _contentHost);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(FrameworkElement.HeightProperty));
            _revertForegroundStoryboard.Children.Add(heightAnimation);
        }
    }
}

public class UniformToolBar : ToolBar
{
    private ToolBarPanel? _itemsHost;
    private Size _currentUniformSize;
    private ToolBarOverflowPanel? _toolBarOverflowPanel;
    private readonly Dictionary<FrameworkElement, Size> _originalDesiredSizes = [];

    private const string ToolBarPanelTemplateName = "PART_ToolBarPanel";
    private const string ToolBarOverflowPanelTemplateName = "PART_ToolBarOverflowPanel";

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

    #region IsOverflowItem
    /// <summary>
    ///     The key needed set a read-only property.
    /// Attached property to indicate if the item is placed in the overflow panel
    /// </summary>
    internal static readonly DependencyPropertyKey IsOverflowItemPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                    "IsOverflowItem",
                    typeof(bool),
                    typeof(ToolBar),
                    new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

    /// <summary>
    ///     The DependencyProperty for the IsOverflowItem property.
    ///     Flags:              None
    ///     Default Value:      false
    /// </summary>
    public static new readonly DependencyProperty IsOverflowItemProperty = IsOverflowItemPropertyKey.DependencyProperty;

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
    public static new bool GetIsOverflowItem(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(IsOverflowItemProperty);
    }

    #endregion
    #region HasOverflowItems

    /// <summary>
    ///     The key needed set a read-only property.
    /// </summary>
    internal static readonly DependencyPropertyKey HasOverflowItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                    "HasOverflowItems",
                    typeof(bool),
                    typeof(ToolBar),
                    new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

    /// <summary>
    ///     The DependencyProperty for the HasOverflowItems property.
    ///     Flags:              None
    ///     Default Value:      false
    /// </summary>
    public static new readonly DependencyProperty HasOverflowItemsProperty =
            HasOverflowItemsPropertyKey.DependencyProperty;

    /// <summary>
    /// Whether we have overflow items
    /// </summary>
    public new bool HasOverflowItems => (bool)GetValue(HasOverflowItemsProperty);
    #endregion HasOverflowItems

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

    private void OnUniformToolBarItemSizeChanged(object sender, UniformToolBarItemSizeChangedEventArgs e) => _ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _itemsHost = GetTemplateChild(ToolBarPanelTemplateName) as ToolBarPanel
            ?? // Only thrown if Microsoft .NET source have drastically changed the template for ToolBar, which is unlikely.
               // We throw so we can update the code to match the new template.
               throw new InvalidOperationException("PART_ToolBarPanel not found in official .NET template.");
        DependencyObject? panel = GetTemplateChild(ToolBarOverflowPanelTemplateName);
        if (panel is not null and not System.Windows.Controls.Primitives.ToolBarOverflowPanel)
        {
            throw new NotSupportedException("The template part named PART_ToolBarOverflowPanel must be of type ToolBarOverflowPanel.");
        }

        _toolBarOverflowPanel = panel as ToolBarOverflowPanel;
    }

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

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        if (e is null)
        {
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                _ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);
                break;
            case NotifyCollectionChangedAction.Replace:
                PurgeTable(e.OldItems);
                _ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);
                break;
            case NotifyCollectionChangedAction.Remove:
                PurgeTable(e.OldItems);
                _ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);
                break;
            case NotifyCollectionChangedAction.Reset:
                _originalDesiredSizes.Clear();
                _ = Dispatcher.InvokeAsync(ApplyUniformSizing, DispatcherPriority.Render);
                break;
            case NotifyCollectionChangedAction.Move:
            default:
                return;
        }
    }

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
}

public class UniformToolBarPanel : ToolBarPanel
{
    private readonly LayoutPlan _plan;
    private Size _lastMeasureConstraint;
    /// <summary>
    ///     Instantiates a new instance of this class.
    /// </summary>
    public UniformToolBarPanel() : base()
    {
        _plan = new LayoutPlan();
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

    private bool MeasureGeneratedItems(bool asNeededPass, Size constraint, bool horizontal, double maxExtent, ref Size panelDesiredSize, out double overflowExtent)
    {
        ToolBarOverflowPanel overflowPanel = ToolBarOverflowPanel;
        bool sendToOverflow = false; // Becomes true when the first AsNeeded does not fit
        bool hasOverflowItems = false;
        bool overflowNeedsInvalidation = false;
        overflowExtent = 0.0;
        UIElementCollection children = InternalChildren;
        int childrenCount = children.Count;
        int childrenIndex = 0;

        for (int i = 0; i < _generatedItemsCollection.Count; i++)
        {
            UIElement child = _generatedItemsCollection[i];
            OverflowMode overflowMode = System.Windows.Controls.ToolBar.GetOverflowMode(child);
            bool asNeededMode = overflowMode == OverflowMode.AsNeeded;

            // MeasureGeneratedItems is called twice to do a complete measure.
            // The first pass measures Always and Never items -- items that
            // are guaranteed to be or not to be in the overflow menu.
            // The second pass measures AsNeeded items and determines whether
            // there is enough room for them in the main bar or if they should
            // be placed in the overflow menu.
            // Check here whether the overflow mode matches a mode we should be
            // examining in this pass.
            if (asNeededMode == asNeededPass)
            {
                DependencyObject visualParent = VisualTreeHelper.GetParent(child);

                // In non-Always overflow modes, measure for main bar placement.
                if ((overflowMode != OverflowMode.Always) && !sendToOverflow)
                {
                    // Children may change their size depending on whether they are in the overflow
                    // menu or not. Ensure that when we measure, we are using the main bar size.
                    // If the item goes to overflow, this property will be updated later in this loop
                    // when it is removed from the visual tree.
                    UniformToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);
                    child.Measure(constraint);
                    Size childDesiredSize = child.DesiredSize;

                    // If the child is an AsNeeded, check if it fits. If it doesn't then
                    // this child and all subsequent AsNeeded children should be sent
                    // to the overflow menu.
                    if (asNeededMode)
                    {
                        double newExtent;
                        if (horizontal)
                        {
                            newExtent = childDesiredSize.Width + panelDesiredSize.Width;
                        }
                        else
                        {
                            newExtent = childDesiredSize.Height + panelDesiredSize.Height;
                        }

                        if (DoubleUtil.GreaterThan(newExtent, maxExtent))
                        {
                            // It doesn't fit, send to overflow
                            sendToOverflow = true;
                        }
                    }

                    // The child has been validated as belonging in the main bar.
                    // Update the panel desired size dimensions, and ensure the child
                    // is in the main bar's visual tree.
                    if (!sendToOverflow)
                    {
                        if (horizontal)
                        {
                            panelDesiredSize.Width += childDesiredSize.Width;
                            panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, childDesiredSize.Height);
                        }
                        else
                        {
                            panelDesiredSize.Width = Math.Max(panelDesiredSize.Width, childDesiredSize.Width);
                            panelDesiredSize.Height += childDesiredSize.Height;
                        }

                        if (visualParent != this)
                        {
                            if ((visualParent == overflowPanel) && (overflowPanel != null))
                            {
                                overflowPanel.Children.Remove(child);
                            }

                            if (childrenIndex < childrenCount)
                            {
                                children.Insert(childrenIndex, child);
                            }
                            else
                            {
                                _ = children.Add(child);
                            }

                            childrenCount++;
                        }

                        Debug.Assert(children[childrenIndex] == child, "InternalChildren is out of sync with _generatedItemsCollection.");
                        childrenIndex++;
                    }
                }

                // The child should go to the overflow menu
                if ((overflowMode == OverflowMode.Always) || sendToOverflow)
                {
                    hasOverflowItems = true;

                    // If a child is in the overflow menu, we don't want to keep measuring.
                    // However, we need to calculate the MaxLength as well as set the desired height
                    // correctly. Thus, we will use the DesiredSize of the child. There is a problem
                    // that can occur if the child changes size while in the overflow menu and
                    // was recently displayed. It will be measure clean, yet its DesiredSize
                    // will not be accurate for the MaxLength calculation.
                    if (child.IsMeasureValid)
                    {
                        // Set this temporarily in case the size is different while in the overflow area
                        UniformToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);
                        child.Measure(constraint);
                    }

                    // Even when in the overflow, we need two pieces of information:
                    // 1. We need to continue to track the maximum size of the non-logical direction
                    //    (i.e. height in horizontal bars). This way, ToolBars with everything in
                    //    the overflow will still have some height.
                    // 2. We want to track how much of the space we saved by placing the child in
                    //    the overflow menu. This is used to calculate MinLength and MaxLength.
                    Size childDesiredSize = child.DesiredSize;
                    if (horizontal)
                    {
                        overflowExtent += childDesiredSize.Width;
                        panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, childDesiredSize.Height);
                    }
                    else
                    {
                        overflowExtent += childDesiredSize.Height;
                        panelDesiredSize.Width = Math.Max(panelDesiredSize.Width, childDesiredSize.Width);
                    }

                    // Set the flag to indicate that the child is in the overflow menu
                    UniformToolBar.SetIsOverflowItem(child, BooleanBoxes.TrueBox);

                    // If the child is in this panel's visual tree, remove it.
                    if (visualParent == this)
                    {
                        Debug.Assert(children[childrenIndex] == child, "InternalChildren is out of sync with _generatedItemsCollection.");
                        children.Remove(child);
                        childrenCount--;
                        overflowNeedsInvalidation = true;
                    }
                    // If the child isnt connected to the visual tree, notify the overflow panel to pick it up.
                    else if (visualParent == null)
                    {
                        overflowNeedsInvalidation = true;
                    }
                }
            }
            else
            {
                // We are not measure this child in this pass. Update the index into the
                // visual children collection.
                if ((childrenIndex < childrenCount) && (children[childrenIndex] == child))
                {
                    childrenIndex++;
                }
            }
        }

        // A child was added to the overflow panel, but since we don't add it
        // to the overflow panel's visual collection until that panel's measure
        // pass, we need to mark it as measure dirty.
        if (overflowNeedsInvalidation && (overflowPanel != null))
        {
            overflowPanel.InvalidateMeasure();
        }

        return hasOverflowItems;
    }

    /// <summary>
    /// Measure the content and store the desired size of the content
    /// </summary>
    /// <param name="constraint"></param>
    /// <returns></returns>
    protected override Size MeasureOverride(Size constraint)
    {
        var stackDesiredSize = new Size();

        if (IsItemsHost)
        {
            Size layoutSlotSize = constraint;
            double maxExtent;
            bool horizontal = Orientation == Orientation.Horizontal;

            if (horizontal)
            {
                layoutSlotSize.Width = double.PositiveInfinity;
                maxExtent = constraint.Width;
            }
            else
            {
                layoutSlotSize.Height = double.PositiveInfinity;
                maxExtent = constraint.Height;
            }

            // This first call will measure all of the non-AsNeeded elements (i.e. we know
            // whether they're going into the overflow or not.
            // overflowExtent will be the size of the Always elements, which is not actually
            // needed for subsequent calculations.
            bool hasAlwaysOverflowItems = MeasureGeneratedItems(/* asNeeded = */ false, layoutSlotSize, horizontal, maxExtent, ref stackDesiredSize, out _);

            // At this point, the desired size is the minimum size of the ToolBar.
            MinLength = horizontal ? stackDesiredSize.Width : stackDesiredSize.Height;

            // This second call will measure all of the AsNeeded elements and place
            // them in the appropriate location.
            bool hasAsNeededOverflowItems = MeasureGeneratedItems(/* asNeeded = */ true, layoutSlotSize, horizontal, maxExtent, ref stackDesiredSize, out double overflowExtent);

            // At this point, the desired size is complete. The desired size plus overflowExtent
            // is the maximum size of the ToolBar.
            MaxLength = (horizontal ? stackDesiredSize.Width : stackDesiredSize.Height) + overflowExtent;

            UniformToolBar? toolbar = ToolBar;
            toolbar?.SetValue(UniformToolBar.HasOverflowItemsPropertyKey, hasAlwaysOverflowItems || hasAsNeededOverflowItems);
        }
        else
        {
            stackDesiredSize = base.MeasureOverride(constraint);
        }

        return stackDesiredSize;
    }

    /// <summary>
    /// Content arrangement.
    /// </summary>
    /// <param name="arrangeSize">Arrange size</param>
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        UIElementCollection children = InternalChildren;
        bool fHorizontal = Orientation == Orientation.Horizontal;
        var rcChild = new Rect(arrangeSize);
        double previousChildSize = 0.0d;

        //
        // Arrange and Position Children.
        //
        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = (UIElement)children[i];

            if (fHorizontal)
            {
                rcChild.X += previousChildSize;
                previousChildSize = child.DesiredSize.Width;
                rcChild.Width = previousChildSize;
                rcChild.Height = Math.Max(arrangeSize.Height, child.DesiredSize.Height);
            }
            else
            {
                rcChild.Y += previousChildSize;
                previousChildSize = child.DesiredSize.Height;
                rcChild.Height = previousChildSize;
                rcChild.Width = Math.Max(arrangeSize.Width, child.DesiredSize.Width);
            }

            child.Arrange(rcChild);
        }

        return arrangeSize;
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
        
        if (TemplatedParent is ToolBar && isOrientationDefaultValue)
        {
            var binding = new Binding
            {
                RelativeSource = RelativeSource.TemplatedParent,
                Path = new PropertyPath(System.Windows.Controls.ToolBar.OrientationProperty)
            };
            _ = SetBinding(OrientationProperty, binding);
        }
    }

    #endregion

    #region Item Generation

    //internal override void GenerateChildren()
    //{
    //    base.GenerateChildren();

    //    // This could re-enter InternalChildren, but after base.GenerateChildren, the collection
    //    // should be fully instantiated and ready to go.
    //    UIElementCollection children = InternalChildren;
    //    if (_generatedItemsCollection == null)
    //    {
    //        _generatedItemsCollection = [with(children.Count)];
    //    }
    //    else
    //    {
    //        _generatedItemsCollection.Clear();
    //    }

    //    ToolBarOverflowPanel overflowPanel = ToolBarOverflowPanel;
    //    if (overflowPanel != null)
    //    {
    //        overflowPanel.Children.Clear();
    //        overflowPanel.InvalidateMeasure();
    //    }

    //    int childrenCount = children.Count;
    //    for (int i = 0; i < childrenCount; i++)
    //    {
    //        UIElement child = children[i];

    //        // Reset the overflow decision. This will be re-evaluated on the next measure
    //        // by ToolBarPanel.MeasureOverride.
    //        ToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);

    //        _generatedItemsCollection.Add(child);
    //    }
    //}

    //// This method returns a bool to indicate if or not the panel layout is affected by this collection change
    //internal override bool OnItemsChangedInternal(object sender, ItemsChangedEventArgs args)
    //{
    //    switch (args.Action)
    //    {
    //        case NotifyCollectionChangedAction.Add:
    //            AddChildren(args.Position, args.ItemCount);
    //            break;
    //        case NotifyCollectionChangedAction.Remove:
    //            RemoveChildren(args.Position, args.ItemUICount);
    //            break;
    //        case NotifyCollectionChangedAction.Replace:
    //            ReplaceChildren(args.Position, args.ItemCount, args.ItemUICount);
    //            break;
    //        case NotifyCollectionChangedAction.Move:
    //            MoveChildren(args.OldPosition, args.Position, args.ItemUICount);
    //            break;

    //        case NotifyCollectionChangedAction.Reset:
    //            base.OnItemsChangedInternal(sender, args);
    //            break;
    //    }

    //    return true;
    //}

    //private void AddChildren(GeneratorPosition pos, int itemCount)
    //{
    //    var generator = (IItemContainerGenerator)Generator;
    //    using (generator.StartAt(pos, GeneratorDirection.Forward))
    //    {
    //        for (int i = 0; i < itemCount; i++)
    //        {
    //            if (generator.GenerateNext() is UIElement e)
    //            {
    //                _generatedItemsCollection.Insert(pos.Index + 1 + i, e);
    //                generator.PrepareItemContainer(e);
    //            }
    //            else
    //            {
    //                var icg = Generator as ItemContainerGenerator;
    //                icg?.Verify();
    //            }
    //        }
    //    }
    //}

    //private void RemoveChild(UIElement child)
    //{
    //    DependencyObject visualParent = VisualTreeHelper.GetParent(child);

    //    if (visualParent == this)
    //    {
    //        InternalChildren.RemoveInternal(child);
    //    }
    //    else
    //    {
    //        ToolBarOverflowPanel overflowPanel = ToolBarOverflowPanel;
    //        if ((visualParent == overflowPanel) && (overflowPanel != null))
    //        {
    //            overflowPanel.Children.Remove(child);
    //        }
    //    }
    //}

    //private void RemoveChildren(GeneratorPosition pos, int containerCount)
    //{
    //    for (int i = 0; i < containerCount; i++)
    //    {
    //        RemoveChild(_generatedItemsCollection[pos.Index + i]);
    //    }

    //    _generatedItemsCollection.RemoveRange(pos.Index, containerCount);
    //}

    //private void ReplaceChildren(GeneratorPosition pos, int itemCount, int containerCount)
    //{
    //    Debug.Assert(itemCount == containerCount, "ToolBarPanel expects Replace to affect only realized containers");

    //    var generator = (IItemContainerGenerator)Generator;
    //    using (generator.StartAt(pos, GeneratorDirection.Forward, true))
    //    {
    //        for (int i = 0; i < itemCount; i++)
    //        {
    //            var e = generator.GenerateNext(out bool isNewlyRealized) as UIElement;

    //            Debug.Assert(e != null && !isNewlyRealized, "ToolBarPanel expects Replace to affect only realized containers");
    //            if (e != null && !isNewlyRealized)
    //            {
    //                RemoveChild(_generatedItemsCollection[pos.Index + i]);
    //                _generatedItemsCollection[pos.Index + i] = e;
    //                generator.PrepareItemContainer(e);
    //            }
    //            else
    //            {
    //                var icg = Generator as ItemContainerGenerator;
    //                icg?.Verify();
    //            }
    //        }
    //    }
    //}

    //private void MoveChildren(GeneratorPosition fromPos, GeneratorPosition toPos, int containerCount)
    //{
    //    if (fromPos == toPos)
    //    {
    //        return;
    //    }

    //    var generator = (IItemContainerGenerator)Generator;
    //    int toIndex = generator.IndexFromGeneratorPosition(toPos);

    //    var elements = new UIElement[containerCount];

    //    for (int i = 0; i < containerCount; i++)
    //    {
    //        UIElement child = _generatedItemsCollection[fromPos.Index + i];
    //        RemoveChild(child);
    //        elements[i] = child;
    //    }

    //    _generatedItemsCollection.RemoveRange(fromPos.Index, containerCount);

    //    for (int i = 0; i < containerCount; i++)
    //    {
    //        _generatedItemsCollection.Insert(toIndex + i, elements[i]);
    //    }
    //}

    #endregion

    #region Helpers

    private UniformToolBar? ToolBar => TemplatedParent as UniformToolBar;

    private ToolBarOverflowPanel? ToolBarOverflowPanel => ToolBar?.ToolBarOverflowPanel;

    // Accessed by ToolBarOverflowPanel
    //internal List<UIElement> GeneratedItemsCollection => _generatedItemsCollection;

    #endregion

    #region Data

    //private List<UIElement> _generatedItemsCollection;

    #endregion

    private sealed class LayoutPlan
    {
        public Size UniformItemSize { get; init; }
        public int VisibleCount { get; init; }
        public double LogicalMainLength { get; init; }
        public IReadOnlyList<UIElement> VisibleChildren { get; init; }
        public IReadOnlyList<Rect> Slots { get; init; }
    }
}

/// <summary>
/// Dependency properties of type bool are boxed when stored in the property system. 
/// This class provides boxed values for <see langword="true"/> and <see langword="false"/> to avoid unnecessary allocations.
/// </summary>
internal static class BooleanBoxes
{
    internal static readonly object TrueBox = true;
    internal static readonly object FalseBox = false;

    internal static object Box(bool value) => value ? TrueBox : FalseBox;

    internal static object? Box(bool? value) => value switch
    {
        true => TrueBox,
        false => FalseBox,
        null => null,
    };
}

/// <summary>
/// DoubleUtil uses fixed eps to provide fuzzy comparison functionality for doubles.
/// Note that FP noise is a big problem and using any of these compare 
/// methods is not a complete solution, but rather the way to reduce 
/// the probability of repeating unnecessary work.
/// </summary>
internal static class DoubleUtil
{
    // Const values come from sdk\inc\crt\float.h
    internal const double DBL_EPSILON = 2.2204460492503131e-016; /* smallest such that 1.0+DBL_EPSILON != 1.0 */
    internal const float FLT_MIN = 1.175494351e-38F; /* Number close to zero, where float.MinValue is -float.MaxValue */

    /// <summary>
    /// AreClose - Returns whether or not two doubles are "close".  That is, whether or 
    /// not they are within epsilon of each other.  Note that this epsilon is proportional
    /// to the numbers themselves to that AreClose survives scalar multiplication.
    /// There are plenty of ways for this to return false even for numbers which
    /// are theoretically identical, so no code calling this should fail to work if this 
    /// returns false.  This is important enough to repeat:
    /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
    /// used for optimizations *only*.
    /// </summary>
    /// <returns>
    /// bool - the result of the AreClose comparison.
    /// </returns>
    /// <param name="value1"> The first double to compare. </param>
    /// <param name="value2"> The second double to compare. </param>
    public static bool AreClose(double value1, double value2)
    {
        //in case they are Infinities (then epsilon check does not work)
        if (value1 == value2)
        {
            return true;
        }

        // This computes (|value1-value2| / (|value1| + |value2| + 10.0)) < DBL_EPSILON
        double eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DBL_EPSILON;
        double delta = value1 - value2;
        return (-eps < delta) && (eps > delta);
    }

    /// <summary>
    /// LessThan - Returns whether or not the first double is less than the second double.
    /// That is, whether or not the first is strictly less than *and* not within epsilon of
    /// the other number.  Note that this epsilon is proportional to the numbers themselves
    /// to that AreClose survives scalar multiplication.  Note,
    /// There are plenty of ways for this to return false even for numbers which
    /// are theoretically identical, so no code calling this should fail to work if this 
    /// returns false.  This is important enough to repeat:
    /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
    /// used for optimizations *only*.
    /// </summary>
    /// <returns>
    /// bool - the result of the LessThan comparison.
    /// </returns>
    /// <param name="value1"> The first double to compare. </param>
    /// <param name="value2"> The second double to compare. </param>
    public static bool LessThan(double value1, double value2) => (value1 < value2) && !AreClose(value1, value2);

    /// <summary>
    /// GreaterThan - Returns whether or not the first double is greater than the second double.
    /// That is, whether or not the first is strictly greater than *and* not within epsilon of
    /// the other number.  Note that this epsilon is proportional to the numbers themselves
    /// to that AreClose survives scalar multiplication.  Note,
    /// There are plenty of ways for this to return false even for numbers which
    /// are theoretically identical, so no code calling this should fail to work if this 
    /// returns false.  This is important enough to repeat:
    /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
    /// used for optimizations *only*.
    /// </summary>
    /// <returns>
    /// bool - the result of the GreaterThan comparision.
    /// </returns>
    /// <param name="value1"> The first double to compare. </param>
    /// <param name="value2"> The second double to compare. </param>
    public static bool GreaterThan(double value1, double value2) => (value1 > value2) && !AreClose(value1, value2);

    /// <summary>
    /// GreaterThanZero - Returns whether or not the value is greater than zero
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool GreaterThanZero(double value) => value >= 10.0 * DBL_EPSILON;

    /// <summary>
    /// LessThanOrClose - Returns whether or not the first double is less than or close to
    /// the second double.  That is, whether or not the first is strictly less than or within
    /// epsilon of the other number.  Note that this epsilon is proportional to the numbers 
    /// themselves to that AreClose survives scalar multiplication.  Note,
    /// There are plenty of ways for this to return false even for numbers which
    /// are theoretically identical, so no code calling this should fail to work if this 
    /// returns false.  This is important enough to repeat:
    /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
    /// used for optimizations *only*.
    /// </summary>
    /// <returns>
    /// bool - the result of the LessThanOrClose comparision.
    /// </returns>
    /// <param name="value1"> The first double to compare. </param>
    /// <param name="value2"> The second double to compare. </param>
    public static bool LessThanOrClose(double value1, double value2) => (value1 < value2) || AreClose(value1, value2);

    /// <summary>
    /// GreaterThanOrClose - Returns whether or not the first double is greater than or close to
    /// the second double.  That is, whether or not the first is strictly greater than or within
    /// epsilon of the other number.  Note that this epsilon is proportional to the numbers 
    /// themselves to that AreClose survives scalar multiplication.  Note,
    /// There are plenty of ways for this to return false even for numbers which
    /// are theoretically identical, so no code calling this should fail to work if this 
    /// returns false.  This is important enough to repeat:
    /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
    /// used for optimizations *only*.
    /// </summary>
    /// <returns>
    /// bool - the result of the GreaterThanOrClose comparision.
    /// </returns>
    /// <param name="value1"> The first double to compare. </param>
    /// <param name="value2"> The second double to compare. </param>
    public static bool GreaterThanOrClose(double value1, double value2) => (value1 > value2) || AreClose(value1, value2);

    /// <summary>
    /// IsOne - Returns whether or not the double is "close" to 1.  Same as AreClose(double, 1),
    /// but this is faster.
    /// </summary>
    /// <returns>
    /// bool - the result of the AreClose comparision.
    /// </returns>
    /// <param name="value"> The double to compare to 1. </param>
    public static bool IsOne(double value) => Math.Abs(value - 1.0) < 10.0 * DBL_EPSILON;

    /// <summary>
    /// IsZero - Returns whether or not the double is "close" to 0.  Same as AreClose(double, 0),
    /// but this is faster.
    /// </summary>
    /// <returns>
    /// bool - the result of the AreClose comparision.
    /// </returns>
    /// <param name="value"> The double to compare to 0. </param>
    public static bool IsZero(double value) => Math.Abs(value) < 10.0 * DBL_EPSILON;

    // The Point, Size, Rect and Matrix class have moved to WinCorLib.  However, we provide
    // internal AreClose methods for our own use here.

    /// <summary>
    /// Compares two points for fuzzy equality.  This function
    /// helps compensate for the fact that double values can 
    /// acquire error when operated upon
    /// </summary>
    /// <param name='point1'>The first point to compare</param>
    /// <param name='point2'>The second point to compare</param>
    /// <returns>Whether or not the two points are equal</returns>
    public static bool AreClose(Point point1, Point point2) => DoubleUtil.AreClose(point1.X, point2.X)
        && DoubleUtil.AreClose(point1.Y, point2.Y);

    /// <summary>
    /// Compares two Size instances for fuzzy equality.  This function
    /// helps compensate for the fact that double values can 
    /// acquire error when operated upon
    /// </summary>
    /// <param name='size1'>The first size to compare</param>
    /// <param name='size2'>The second size to compare</param>
    /// <returns>Whether or not the two Size instances are equal</returns>
    public static bool AreClose(Size size1, Size size2) => DoubleUtil.AreClose(size1.Width, size2.Width)
        && DoubleUtil.AreClose(size1.Height, size2.Height);

    /// <summary>
    /// Compares two Vector instances for fuzzy equality.  This function
    /// helps compensate for the fact that double values can 
    /// acquire error when operated upon
    /// </summary>
    /// <param name='vector1'>The first Vector to compare</param>
    /// <param name='vector2'>The second Vector to compare</param>
    /// <returns>Whether or not the two Vector instances are equal</returns>
    public static bool AreClose(System.Windows.Vector vector1, System.Windows.Vector vector2) => DoubleUtil.AreClose(vector1.X, vector2.X)
        && DoubleUtil.AreClose(vector1.Y, vector2.Y);

    /// <summary>
    /// Compares two rectangles for fuzzy equality.  This function
    /// helps compensate for the fact that double values can 
    /// acquire error when operated upon
    /// </summary>
    /// <param name='rect1'>The first rectangle to compare</param>
    /// <param name='rect2'>The second rectangle to compare</param>
    /// <returns>Whether or not the two rectangles are equal</returns>
    public static bool AreClose(Rect rect1, Rect rect2)
    {
        // If they're both empty, don't bother with the double logic.
        if (rect1.IsEmpty)
        {
            return rect2.IsEmpty;
        }

        // At this point, rect1 isn't empty, so the first thing we can test is
        // rect2.IsEmpty, followed by property-wise compares.

        return (!rect2.IsEmpty) &&
            DoubleUtil.AreClose(rect1.X, rect2.X) &&
            DoubleUtil.AreClose(rect1.Y, rect2.Y) &&
            DoubleUtil.AreClose(rect1.Height, rect2.Height) &&
            DoubleUtil.AreClose(rect1.Width, rect2.Width);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public static bool IsBetweenZeroAndOne(double val) => GreaterThanOrClose(val, 0) && LessThanOrClose(val, 1);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public static int DoubleToInt(double val) => (0 < val) ? (int)(val + 0.5) : (int)(val - 0.5);

    /// <summary>
    /// rectHasNaN - this returns true if this rect has X, Y , Height or Width as NaN.
    /// </summary>
    /// <param name='r'>The rectangle to test</param>
    /// <returns>returns whether the Rect has NaN</returns>        
    public static bool RectHasNaN(Rect r)
    {
        if (double.IsNaN(r.X)
             || double.IsNaN(r.Y)
             || double.IsNaN(r.Height)
             || double.IsNaN(r.Width))
        {
            return true;
        }

        return false;
    }
}