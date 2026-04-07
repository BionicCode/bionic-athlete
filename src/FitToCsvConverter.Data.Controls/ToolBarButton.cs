namespace FitToCsvConverter.Controls;

using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
            if (frameworkElement is not Separator)
            {
                if (!_originalDesiredSizes.TryGetValue(frameworkElement, out Size originalDesiredSize))
                {
                    originalDesiredSize = frameworkElement.DesiredSize;
                    _originalDesiredSizes[frameworkElement] = originalDesiredSize;
                }

                maxHeight = Math.Max(maxHeight, originalDesiredSize.Height);
                maxWidth = Math.Max(maxWidth, originalDesiredSize.Width);
                candidates.Add(frameworkElement);
            }
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