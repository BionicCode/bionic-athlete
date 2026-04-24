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
    private Storyboard? _mouseOverStoryboard;
    private Storyboard? _revertMouseOverStoryboard;
    private double _buttonMouseHoverScaleFactor;

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

    private void UpdateSizeRelatedResources()
    {
        BuildMouseOverStoryboard();
        BuildMouseOverRevertStoryboard();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentHost = GetTemplateChild("PART_ContentPresenterViewBox") as FrameworkElement;
        _ = _contentHost?.RenderTransform = new ScaleTransform(1, 1);
        _ = (_contentHost?.RenderTransformOrigin = new Point(0.5, 0.5));

        _label = GetTemplateChild("PART_Label") as TextBlock;
        _ = _label?.RenderTransform = new ScaleTransform(0, 0);
        _ = (_label?.RenderTransformOrigin = new Point(0.5, 0.5));
        _labelBorder = GetTemplateChild("PART_LabelBorder") as Border;
        _ = _labelBorder?.RenderTransform = new ScaleTransform(1, 1);
        _ = (_labelBorder?.RenderTransformOrigin = new Point(0.5, 0.5));

        _buttonMouseHoverScaleFactor = TryFindResource("ButtonMouseHoverScaleFactor") is double scaleFactor
            ? scaleFactor
            : 0.75;

    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        if (_contentHost is not null
            && _label is not null
            && _labelBorder is not null)
        {
            if (_mouseOverStoryboard is null)
            {
                BuildMouseOverStoryboard();
            }

            _mouseOverStoryboard?.Begin();
        }

        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if (_contentHost is not null
            && _label is not null
            && _labelBorder is not null)
        {
            if (_revertMouseOverStoryboard is null)
            {
                BuildMouseOverRevertStoryboard();
            }

            _revertMouseOverStoryboard?.Begin();
        }

        base.OnMouseLeave(e);
    }

    private void BuildMouseOverStoryboard()
    {
        if (_contentHost is not null
            && _label is not null
            && _labelBorder is not null)
        {
            _mouseOverStoryboard = new Storyboard() { FillBehavior = FillBehavior.HoldEnd };

            var opacityAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(opacityAnimation, _labelBorder);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            _mouseOverStoryboard.Children.Add(opacityAnimation);

            var borderHorizontalGrowAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(borderHorizontalGrowAnimation, _labelBorder);
            Storyboard.SetTargetProperty(borderHorizontalGrowAnimation, new PropertyPath("RenderTransform.ScaleX"));
            _mouseOverStoryboard.Children.Add(borderHorizontalGrowAnimation);

            var horizontalFontSizeAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(horizontalFontSizeAnimation, _label);
            Storyboard.SetTargetProperty(horizontalFontSizeAnimation, new PropertyPath("RenderTransform.ScaleX"));
            _mouseOverStoryboard.Children.Add(horizontalFontSizeAnimation);

            var verticalFontSizeAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(verticalFontSizeAnimation, _label);
            Storyboard.SetTargetProperty(verticalFontSizeAnimation, new PropertyPath("RenderTransform.ScaleY"));
            _mouseOverStoryboard.Children.Add(verticalFontSizeAnimation);

            // Shrink to 75 % of original height to create some visual interest and to help indicate that the button is being hovered over
            var contentHeightAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = _buttonMouseHoverScaleFactor,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(contentHeightAnimation, _contentHost);
            Storyboard.SetTargetProperty(contentHeightAnimation, new PropertyPath("RenderTransform.ScaleY"));
            _mouseOverStoryboard.Children.Add(contentHeightAnimation);

            var contentWidthAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.75,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(contentWidthAnimation, _contentHost);
            Storyboard.SetTargetProperty(contentWidthAnimation, new PropertyPath("RenderTransform.ScaleX"));
            _mouseOverStoryboard.Children.Add(contentWidthAnimation);
        }
    }

    private void BuildMouseOverRevertStoryboard()
    {
        if (_contentHost is not null
            && _label is not null
            && _labelBorder is not null)
        {
            _revertMouseOverStoryboard = new Storyboard() { FillBehavior = FillBehavior.HoldEnd };

            var opacityAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(opacityAnimation, _labelBorder);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            _revertMouseOverStoryboard.Children.Add(opacityAnimation);

            var borderHorizontalShrinkAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(borderHorizontalShrinkAnimation, _labelBorder);
            Storyboard.SetTargetProperty(borderHorizontalShrinkAnimation, new PropertyPath("RenderTransform.ScaleX"));
            _revertMouseOverStoryboard.Children.Add(borderHorizontalShrinkAnimation);

            var horizontalFontSizeAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(horizontalFontSizeAnimation, _label);
            Storyboard.SetTargetProperty(horizontalFontSizeAnimation, new PropertyPath("RenderTransform.ScaleX"));
            _revertMouseOverStoryboard.Children.Add(horizontalFontSizeAnimation);

            var verticalFontSizeAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(verticalFontSizeAnimation, _label);
            Storyboard.SetTargetProperty(verticalFontSizeAnimation, new PropertyPath("RenderTransform.ScaleY"));
            _revertMouseOverStoryboard.Children.Add(verticalFontSizeAnimation);

            var contentHeightAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(contentHeightAnimation, _contentHost);
            Storyboard.SetTargetProperty(contentHeightAnimation, new PropertyPath("RenderTransform.ScaleY"));
            _revertMouseOverStoryboard.Children.Add(contentHeightAnimation);

            var contentWidthAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            };
            Storyboard.SetTarget(contentWidthAnimation, _contentHost);
            Storyboard.SetTargetProperty(contentWidthAnimation, new PropertyPath("RenderTransform.ScaleX"));
            _revertMouseOverStoryboard.Children.Add(contentWidthAnimation);

            Grid.SetRowSpan(_contentHost, 2);
        }
    }
}
