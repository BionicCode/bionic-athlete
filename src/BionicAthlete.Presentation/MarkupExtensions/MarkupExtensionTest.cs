namespace BionicAthlete.Presentation;

using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using BionicCode.Utilities.Net;

public class ExampleTextExtension : MarkupExtension
{
    private readonly Binding _binding;

    public MarkupExtension ConverterResource { get; set; }

    public IValueConverter Converter { get; set; }

    public string LamdaExpression { get; set; } // The lambda as a string, e.g., "x => x * 2"

    // Accept BindingBase to support MultiBinding etc.
    public ExampleTextExtension(Binding binding) => this._binding = binding;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(serviceProvider);

        var provideValueTargetService = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        if (provideValueTargetService?.TargetObject is not DependencyObject targetObject
          || provideValueTargetService?.TargetProperty is not DependencyProperty targetProperty)
        {
            return this;
        }

        IValueConverter? converter = ConverterResource is not null
            ? ConverterResource.ProvideValue(serviceProvider) as IValueConverter
            : Converter;

        var bindingResolver = new BindingResolver(targetObject, targetProperty)
        {
            Converter = converter
        };
        object resolvedResult = bindingResolver.ResolveBinding(_binding, serviceProvider);

        return resolvedResult;
    }
}

//public class CanvasExtension : Canvas
//{
//    public SolidColorBrush TextBrush
//    {
//        get => (SolidColorBrush)GetValue(TextBrushProperty);
//        set => SetValue(TextBrushProperty, value);
//    }

//    public static readonly DependencyProperty TextBrushProperty = DependencyProperty.Register(
//      "TextBrush",
//      typeof(SolidColorBrush),
//      typeof(CanvasExtension),
//      new FrameworkPropertyMetadata(default(SolidColorBrush), FrameworkPropertyMetadataOptions.AffectsRender));

//    protected override void OnRender(DrawingContext dc)
//    {
//        base.OnRender(dc);
//        var formattedTextInput = new FormattedText("Hello world", System.Globalization.CultureInfo.GetCultureInfo("en-US"),
//            FlowDirection.LeftToRight, new Typeface("Verdana"), 12, this.TextBrush, 1);
//        dc.DrawText(formattedTextInput, new Point());
//    }
//}
