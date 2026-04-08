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
    private readonly Dictionary<FrameworkElement, Size> _originalDesiredSizes = [];
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
        _itemsHost = GetTemplateChild("PART_ToolBarPanel") as ToolBarPanel
            ?? // Only thrown if Microsoft .NET source have drastically changed the template for ToolBar, which is unlikely.
               // We throw so we can update the code to match the new template.
               throw new InvalidOperationException("PART_ToolBarPanel not found in official .NET template.");
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