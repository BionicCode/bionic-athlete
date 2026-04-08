namespace FitToCsvConverter.Controls;

using System.Windows;
using System.Windows.Controls;
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

    public double ProgressTextSize
    {
        get => (double)GetValue(ProgressTextSizeProperty);
        set => SetValue(ProgressTextSizeProperty, value);
    }

    public static readonly DependencyProperty ProgressTextSizeProperty =
        DependencyProperty.Register(
            nameof(ProgressTextSize),
            typeof(double),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public Brush ProgressTextBrush
    {
        get => (Brush)GetValue(ProgressTextBrushProperty);
        set => SetValue(ProgressTextBrushProperty, value);
    }

    public static readonly DependencyProperty ProgressTextBrushProperty =
        DependencyProperty.Register(
            nameof(ProgressTextBrush),
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

    public Thickness ProgressTextMargin
    {
        get => (Thickness)GetValue(ProgressTextMarginProperty);
        set => SetValue(ProgressTextMarginProperty, value);
    }

    public static readonly DependencyProperty ProgressTextMarginProperty =
        DependencyProperty.Register(
            nameof(ProgressTextMargin),
            typeof(Thickness),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(new Thickness(4)));

    public ProgressTextAlignment ProgressTextAlignment
    {
        get => (ProgressTextAlignment)GetValue(ProgressTextAlignmentProperty);
        set => SetValue(ProgressTextAlignmentProperty, value);
    }

    public static readonly DependencyProperty ProgressTextAlignmentProperty =
        DependencyProperty.Register(
            nameof(ProgressTextAlignment),
            typeof(ProgressTextAlignment),
            typeof(AdvancedProgressBar),
            new FrameworkPropertyMetadata(ProgressTextAlignment.Center));

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        if (oldValue != newValue)
        {
            ProgressPercentage = Maximum != 0 && Maximum > Minimum
            ? (newValue - Minimum) / (Maximum - Minimum) * 100
            : 0;
        }
    }
}
