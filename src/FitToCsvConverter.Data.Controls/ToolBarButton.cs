namespace FitToCsvConverter.Controls;

using System.Collections;
using System.Collections.Specialized;
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
    private Storyboard? _mouseOverForegroundStoryboard;
    private Storyboard? _revertForegroundStoryboard;
    private double _oldContentHeight;

    public string LabelText { get => (string)GetValue(LabelTextProperty); set => SetValue(LabelTextProperty, value); }

    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(
            nameof(LabelText),
            typeof(string),
            typeof(ToolBarButton),
            new PropertyMetadata(string.Empty));
    public double LabelFontSize { get => (double)GetValue(LabelFontSizeProperty); set => SetValue(LabelFontSizeProperty, value); }

    public static readonly DependencyProperty LabelFontSizeProperty =
        DependencyProperty.Register(
            nameof(LabelFontSize),
            typeof(double),
            typeof(ToolBarButton),
            new PropertyMetadata(14.0));

    static ToolBarButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarButton), new FrameworkPropertyMetadata(typeof(ToolBarButton)));

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentHost = GetTemplateChild("PART_ContentPresenter") as FrameworkElement;
        _label = GetTemplateChild("PART_Label") as TextBlock;
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);

        if (_contentHost is not null
            && _label is not null)
        {
            if (_mouseOverForegroundStoryboard is null)
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
                    To = LabelFontSize,
                    Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                    AutoReverse = false,
                };
                Storyboard.SetTarget(opacityAnimation, _label);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(FontSizeProperty));
                _mouseOverForegroundStoryboard.Children.Add(opacityAnimation);
            }

            _mouseOverForegroundStoryboard.Begin();
            _oldContentHeight = _contentHost.ActualHeight;
            double newContentHeight = _contentHost.ActualHeight - _label.ActualHeight;
            _contentHost.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation
            {
                From = _contentHost.ActualHeight,
                To = newContentHeight,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            });
            Grid.SetRowSpan(_contentHost, 1);
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if (_contentHost is not null)
        {
            if (_revertForegroundStoryboard is null)
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
                    To = 0.001,
                    Duration = TimeSpan.FromMilliseconds(100),
                    AutoReverse = false,
                };
                Storyboard.SetTarget(opacityAnimation, _label);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(FontSizeProperty));
                _revertForegroundStoryboard.Children.Add(opacityAnimation);
            }

            _revertForegroundStoryboard.Begin();

            _contentHost.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation
            {
                To = _oldContentHeight,
                Duration = (Duration)FindResource("MouseOverAnimationDuration"),
                AutoReverse = false,
            });

            Grid.SetRowSpan(_contentHost, 2);
        }

        base.OnMouseLeave(e);
    }
}

public class UniformToolBar : ToolBar
{
    private ToolBarPanel? _itemsHost;
    private Size _currentUniformSize;
    private readonly Dictionary<FrameworkElement, Size> _originalDesiredSizes = [];

    public UniformToolBar()
    {
        _currentUniformSize = Size.Empty;
        Loaded += OnLoaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _itemsHost = GetTemplateChild("PART_ToolBarPanel") as ToolBarPanel
            ?? // Only thrown if Microsoft .NET source have drastically changed the template for ToolBar, which is unlikely.
               // We throw so we can update the code to match the new template.
               throw new InvalidOperationException("PART_ToolBarPanel not found in official .NET template.");
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
            _currentUniformSize = new Size(maxWidth, maxHeight);

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