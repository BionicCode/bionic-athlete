namespace BionicAthlete.Presentation;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

public class AdvancedProgressBar : ProgressBar
{
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(new CornerRadius(4), FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double ProgressPercentageSize
    {
        get => (double)GetValue(ProgressPercentageSizeProperty);
        set => SetValue(ProgressPercentageSizeProperty, value);
    }

    public static readonly DependencyProperty ProgressPercentageSizeProperty =
        DependencyProperty.Register(
            nameof(ProgressPercentageSize),
            typeof(double),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double ProgressMessageSize
    {
        get => (double)GetValue(ProgressMessageSizeProperty);
        set => SetValue(ProgressMessageSizeProperty, value);
    }

    public static readonly DependencyProperty ProgressMessageSizeProperty =
        DependencyProperty.Register(
            nameof(ProgressMessageSize),
            typeof(double),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double TitleSize
    {
        get => (double)GetValue(TitleSizeProperty);
        set => SetValue(TitleSizeProperty, value);
    }

    public static readonly DependencyProperty TitleSizeProperty =
        DependencyProperty.Register(
            nameof(TitleSize),
            typeof(double),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public Brush ProgressPercentageBrush
    {
        get => (Brush)GetValue(ProgressPercentageBrushProperty);
        set => SetValue(ProgressPercentageBrushProperty, value);
    }

    public static readonly DependencyProperty ProgressPercentageBrushProperty =
        DependencyProperty.Register(
            nameof(ProgressPercentageBrush),
            typeof(Brush),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(Brushes.Black));

    public Brush ProgressMessageBrush
    {
        get => (Brush)GetValue(ProgressMessageBrushProperty);
        set => SetValue(ProgressMessageBrushProperty, value);
    }

    public static readonly DependencyProperty ProgressMessageBrushProperty =
        DependencyProperty.Register(
            nameof(ProgressMessageBrush),
            typeof(Brush),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(Brushes.Black));

    public Brush TitleBrush
    {
        get => (Brush)GetValue(TitleBrushProperty);
        set => SetValue(TitleBrushProperty, value);
    }

    public static readonly DependencyProperty TitleBrushProperty =
        DependencyProperty.Register(
            nameof(TitleBrush),
            typeof(Brush),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(Brushes.Black));

    public double ProgressPercentage
    {
        get => (double)GetValue(ProgressPercentageProperty);
        set => SetValue(ProgressPercentageProperty, value);
    }

    public static readonly DependencyProperty ProgressPercentageProperty =
        DependencyProperty.Register(
            nameof(ProgressPercentage),
            typeof(double),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(0.0));

    public Thickness ProgressPercentageMargin
    {
        get => (Thickness)GetValue(ProgressPercentageMarginProperty);
        set => SetValue(ProgressPercentageMarginProperty, value);
    }

    public static readonly DependencyProperty ProgressPercentageMarginProperty =
        DependencyProperty.Register(
            nameof(ProgressPercentageMargin),
            typeof(Thickness),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(new Thickness(4)));

    public ProgressTextAlignment ProgressPercentageAlignment
    {
        get => (ProgressTextAlignment)GetValue(ProgressPercentageAlignmentProperty);
        set => SetValue(ProgressPercentageAlignmentProperty, value);
    }

    public static readonly DependencyProperty ProgressPercentageAlignmentProperty =
        DependencyProperty.Register(
            nameof(ProgressPercentageAlignment),
            typeof(ProgressTextAlignment),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(ProgressTextAlignment.Center));

    public string Title
    {
        get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(AdvancedProgressBar),
        new PropertyMetadata(string.Empty));

    public string ProgressMessage
    {
        get => (string)GetValue(ProgressMessageProperty); set => SetValue(ProgressMessageProperty, value);
    }

    public static readonly DependencyProperty ProgressMessageProperty = DependencyProperty.Register(
        nameof(ProgressMessage),
        typeof(string),
        typeof(AdvancedProgressBar),
        new PropertyMetadata(string.Empty));

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateProgressPercentageProperty();
    }

    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        UpdateProgressPercentageProperty();
    }

    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        UpdateProgressPercentageProperty();
    }

    protected virtual void UpdateProgressPercentageProperty() => ProgressPercentage = Maximum != 0 && Maximum > Minimum
        ? (Value - Minimum) / (Maximum - Minimum) * 100
        : 0;
}

public class SizeToRectConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is Size hostRenderSize
        ? new Rect(hostRenderSize)
        : Rect.Empty;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

[ValueConversion(typeof(CornerRadius), typeof(double))]
public class CornerRadiusToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is CornerRadius cornerRadius
        ? cornerRadius.TopLeft
        : 0.0;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is double doubleValue
        ? new CornerRadius(doubleValue)
        : new CornerRadius(0);
}