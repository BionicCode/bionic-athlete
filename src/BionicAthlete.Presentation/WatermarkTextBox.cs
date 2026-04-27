namespace BionicAthlete.Presentation;

using System;
#region Info

// 2020/06/27  17:39
// Net.Wpf

#endregion

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

public class WatermarkTextBox : TextBox
{
    public object Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
      "Watermark",
      typeof(object),
      typeof(WatermarkTextBox),
      new PropertyMetadata(default));

    public DataTemplate WatermarkTemplate
    {
        get => (DataTemplate)GetValue(WatermarkTemplateProperty);
        set => SetValue(WatermarkTemplateProperty, value);
    }

    public static readonly DependencyProperty WatermarkTemplateProperty = DependencyProperty.Register(
      "WatermarkTemplate",
      typeof(DataTemplate),
      typeof(WatermarkTextBox),
      new PropertyMetadata(default(DataTemplate), OnWatermarkTemplateChanged));

    public DataTemplateSelector WatermarkTemplateSelector
    {
        get => (DataTemplateSelector)GetValue(WatermarkTemplateSelectorProperty);
        set => SetValue(WatermarkTemplateSelectorProperty, value);
    }

    public static readonly DependencyProperty WatermarkTemplateSelectorProperty = DependencyProperty.Register(
      "WatermarkTemplateSelector",
      typeof(DataTemplateSelector),
      typeof(WatermarkTextBox),
      new PropertyMetadata(default(DataTemplateSelector), OnWatermarkTemplateSelectorChanged));

    public string WatermarkStringFormat
    {
        get => (string)GetValue(WatermarkStringFormatProperty);
        set => SetValue(WatermarkStringFormatProperty, value);
    }

    public static readonly DependencyProperty WatermarkStringFormatProperty = DependencyProperty.Register(
      "WatermarkStringFormat",
      typeof(string),
      typeof(WatermarkTextBox),
      new PropertyMetadata(default(string), OnWatermarkStringFormatChanged));

    public Brush WatermarkForeground
    {
        get => (Brush)GetValue(WatermarkForegroundProperty);
        set => SetValue(WatermarkForegroundProperty, value);
    }

    public static readonly DependencyProperty WatermarkForegroundProperty = DependencyProperty.Register(
      "WatermarkForeground",
      typeof(Brush),
      typeof(WatermarkTextBox),
      new PropertyMetadata(Brushes.LightGray, OnWatermarkMetaTextPropertyChanged));

    public double WatermarkFontSize
    {
        get => (double)GetValue(WatermarkFontSizeProperty);
        set => SetValue(WatermarkFontSizeProperty, value);
    }

    public static readonly DependencyProperty WatermarkFontSizeProperty = DependencyProperty.Register(
      "WatermarkFontSize",
      typeof(double),
      typeof(WatermarkTextBox),
      new PropertyMetadata(default(double), OnWatermarkMetaTextPropertyChanged));

    public FontStyle WatermarkFontStyle
    {
        get => (FontStyle)GetValue(WatermarkFontStyleProperty);
        set => SetValue(WatermarkFontStyleProperty, value);
    }

    public static readonly DependencyProperty WatermarkFontStyleProperty = DependencyProperty.Register(
      "WatermarkFontStyle",
      typeof(FontStyle),
      typeof(WatermarkTextBox),
      new PropertyMetadata(FontStyles.Italic, OnWatermarkMetaTextPropertyChanged));

    public FontWeight WatermarkFontWeight
    {
        get => (FontWeight)GetValue(WatermarkFontWeightProperty);
        set => SetValue(WatermarkFontWeightProperty, value);
    }

    public static readonly DependencyProperty WatermarkFontWeightProperty = DependencyProperty.Register(
      "WatermarkFontWeight",
      typeof(FontWeight),
      typeof(WatermarkTextBox),
      new PropertyMetadata(FontWeights.ExtraLight, OnWatermarkMetaTextPropertyChanged));

    public FontStretch WatermarkFontStretch
    {
        get => (FontStretch)GetValue(WatermarkFontStretchProperty);
        set => SetValue(WatermarkFontStretchProperty, value);
    }

    public static readonly DependencyProperty WatermarkFontStretchProperty = DependencyProperty.Register(
      "WatermarkFontStretch",
      typeof(FontStretch),
      typeof(WatermarkTextBox),
      new PropertyMetadata(FontStretches.ExtraExpanded, OnWatermarkMetaTextPropertyChanged));

    public FontFamily WatermarkFontFamily
    {
        get => (FontFamily)GetValue(WatermarkFontFamilyProperty);
        set => SetValue(WatermarkFontFamilyProperty, value);
    }

    public static readonly DependencyProperty WatermarkFontFamilyProperty = DependencyProperty.Register(
      "WatermarkFontFamily",
      typeof(FontFamily),
      typeof(WatermarkTextBox),
      new PropertyMetadata(default));

    public bool IsNumeric
    {
        get => (bool)GetValue(IsNumericProperty);
        set => SetValue(IsNumericProperty, value);
    }

    public static readonly DependencyProperty IsNumericProperty = DependencyProperty.Register(
      "IsNumeric",
      typeof(bool),
      typeof(WatermarkTextBox),
      new PropertyMetadata(false, OnIsNumericChanged));

    public static readonly RoutedUICommand ClearCommand = new("Clears content", nameof(ClearCommand), typeof(WatermarkTextBox));

    private UIElement? PART_WatermarkHost { get; set; }
    private Button? PART_ClearButton { get; set; }

    static WatermarkTextBox() => DefaultStyleKeyProperty.OverrideMetadata(
        typeof(WatermarkTextBox),
        new FrameworkPropertyMetadata(typeof(WatermarkTextBox)));

    public WatermarkTextBox()
    {
        var clearCommandBinding = new CommandBinding(
            ClearCommand,
            executed: (s, e) => Clear(),
            canExecute: (s, e) => e.CanExecute = !string.IsNullOrEmpty(Text));
        _ = CommandBindings.Add(clearCommandBinding);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        PART_WatermarkHost = GetTemplateChild("PART_WatermarkHost") as UIElement;
        if (PART_WatermarkHost is not null)
        {
            InitializeWatermarkHost();
        }

        PART_ClearButton = GetTemplateChild("PART_ClearButton") as Button;
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        if (this.PART_WatermarkHost is null)
        {
            return;
        }

        this.PART_WatermarkHost.Visibility = Visibility.Collapsed;
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        if (this.PART_WatermarkHost is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(this.Text))
        {
            this.PART_WatermarkHost.Visibility = Visibility.Visible;
        }
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);
        if (PART_ClearButton is null)
        {
            return;
        }

        PART_ClearButton.Visibility = string.IsNullOrEmpty(Text)
            ? Visibility.Hidden
            : Visibility.Visible;
    }

    private static void OnWatermarkTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as WatermarkTextBox).OnWatermarkTemplateChanged((DataTemplate)e.OldValue, (DataTemplate)e.NewValue);
    private static void OnWatermarkTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as WatermarkTextBox).OnWatermarkTemplateSelectorChanged((DataTemplateSelector)e.OldValue, (DataTemplateSelector)e.NewValue);
    private static void OnWatermarkStringFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as WatermarkTextBox).OnWatermarkStringFormatChanged((string)e.OldValue, (string)e.NewValue);
    private static void OnWatermarkMetaTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as WatermarkTextBox).OnWatermarkMetaTextPropertyChanged(e.OldValue, e.NewValue);

    private static void OnIsNumericChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as WatermarkTextBox).OnIsNumericChanged((bool)e.OldValue, (bool)e.NewValue);
    private void OnIsNumericChanged(bool oldValue, bool newValue) => throw new NotImplementedException();

    protected virtual void OnWatermarkTemplateChanged(DataTemplate oldTemplate, DataTemplate newTemplate)
    {
        if (this.PART_WatermarkHost is ContentControl contentControl)
        {
            contentControl.ContentTemplate = newTemplate;
        }
    }

    protected virtual void OnWatermarkTemplateSelectorChanged(DataTemplateSelector oldValue, DataTemplateSelector newValue)
    {
        if (this.PART_WatermarkHost is ContentControl contentControl)
        {
            contentControl.ContentTemplateSelector = newValue;
        }
    }

    protected virtual void OnWatermarkStringFormatChanged(string oldValue, string newValue)
    {
        if (this.PART_WatermarkHost is ContentControl contentControl)
        {
            contentControl.ContentStringFormat = newValue;
        }
    }

    protected virtual void OnWatermarkMetaTextPropertyChanged(object oldValue, object newValue) => UpdateWatermarkHostTextProperties();

    private void InitializeWatermarkHost()
    {
        switch (this.PART_WatermarkHost)
        {
            case ContentControl contentControl:
                PrepareContentControl(contentControl);
                break;
            case ContentPresenter contentPresenter:
                contentPresenter.ContentSource = "Watermark";
                break;
            case Decorator decorator:
                var contentHost = new ContentControl();
                PrepareContentControl(contentHost);
                decorator.Child = contentHost;
                break;
            default:
                break;
        }
    }

    private void UpdateWatermarkHostTextProperties()
    {
        switch (this.PART_WatermarkHost)
        {
            case ContentControl contentControl:
                UpdateContentControlTextProperties(contentControl);
                break;
            case ContentPresenter:
                break;
            case Decorator decorator:
                if (decorator.Child is ContentControl contentHost)
                {
                    UpdateContentControlTextProperties(contentHost);
                }

                break;
            default:
                break;
        }
    }

    private void PrepareContentControl(ContentControl contentControl)
    {
        contentControl.ContentTemplate = this.WatermarkTemplate;
        contentControl.ContentTemplateSelector = this.WatermarkTemplateSelector;
        contentControl.ContentStringFormat = this.WatermarkStringFormat;
        contentControl.Content = this.Watermark;
        UpdateContentControlTextProperties(contentControl);
    }

    private void UpdateContentControlTextProperties(ContentControl contentControl)
    {
        contentControl.Foreground = this.WatermarkForeground == default ? this.Foreground : this.WatermarkForeground;
        contentControl.FontStyle = this.WatermarkFontStyle;
        contentControl.FontSize = this.WatermarkFontSize == default ? this.FontSize : this.WatermarkFontSize;
        contentControl.FontFamily = this.WatermarkFontFamily == default ? this.FontFamily : this.WatermarkFontFamily;
        contentControl.FontStretch = this.WatermarkFontStretch == default ? this.FontStretch : this.WatermarkFontStretch;
        contentControl.FontWeight = this.WatermarkFontWeight == default ? this.FontWeight : this.WatermarkFontWeight;
    }
}
