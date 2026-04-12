namespace FitToCsvConverter.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

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

        _contentHost = GetTemplateChild("PART_ContentPresenterViewBox") as FrameworkElement;
        _label = GetTemplateChild("PART_Label") as TextBlock;
        _labelBorder = GetTemplateChild("PART_LabelBorder") as Border;
    }

    protected override Size MeasureOverride(Size constraint)
    {
        Size calculatedSize = base.MeasureOverride(constraint);

        _ = DesiredSize;

        return DesiredSize;
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
