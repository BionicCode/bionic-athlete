#region Info

// 2021/02/26  12:49
// Net.Wpf

#endregion

namespace FitToCsvConverter.Controls;

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using BionicCode.Utilities.Net;

internal class BindingResolver : FrameworkElement
{
    #region ResolvedValue attached property

    public static readonly DependencyProperty ResolvedValueProperty = DependencyProperty.RegisterAttached(
      "ResolvedValue", typeof(object), typeof(BindingResolver), new PropertyMetadata(default, BindingResolver.OnResolvedValueChanged));

    public static void SetResolvedValue(DependencyObject attachingElement, object? value) => attachingElement?.SetValue(BindingResolver.ResolvedValueProperty, value);

    public static object? GetResolvedValue(DependencyObject attachingElement) => attachingElement?.GetValue(BindingResolver.ResolvedValueProperty);

    #endregion ResolvedValue attached property

    public DependencyProperty TargetProperty { get; private set; }
    public WeakReference<DependencyObject> Target { get; private set; }
    public WeakReference<Binding> OriginalBinding { get; private set; }

    public IValueConverter? Converter { get; init; }
    public Func<object?, object?> ResolvedSourceValueFilter { get; init; }
    public Func<object?, object?> ResolvedTargetValueFilter { get; init; }
    private bool IsUpDating { get; set; }
    private static ConditionalWeakTable<DependencyObject, BindingResolver> BindingTargetToBindingResolversMap { get; } = [];

    public BindingResolver(DependencyObject target, DependencyProperty targetProperty)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(target);
        ArgumentNullExceptionAdvanced.ThrowIfNull(targetProperty);

        Target = new WeakReference<DependencyObject>(target);
        TargetProperty = targetProperty;
        OriginalBinding = new WeakReference<Binding>(null!);
        ResolvedSourceValueFilter = _ => _;
        ResolvedTargetValueFilter = _ => _;
    }

    private void AddBindingTargetToLookupTable(DependencyObject target) => BindingResolver.BindingTargetToBindingResolversMap.Add(target, this);

    /// <summary>
    /// Resolves the passed <see cref="Binding"/> expression by creating two separate <see cref="Binding"/> instances: one for listening to the data source and another for delegating the resolved value to the original target of the original <see cref="Binding"/>.
    /// </summary>
    /// <param name="bindingExpression"></param>
    /// <returns>The resolved value of the binding. Return this value from the <see cref="ProvideValue"/> method of a <see cref="MarkupExtension"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public object ResolveBinding(Binding bindingExpression, IServiceProvider serviceProvider)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(bindingExpression);

        if (!Target.TryGetTarget(out DependencyObject? bindingTarget))
        {
            throw new InvalidOperationException("Unable to resolve sourceBinding. Binding target is 'null', because the reference has already been garbage collected.");
        }

        AddBindingTargetToLookupTable(bindingTarget);

        Binding binding = bindingExpression;
        OriginalBinding = new WeakReference<Binding>(binding);

        // Listen to data source
        Binding sourceBinding = CloneBinding(binding);
        _ = BindingOperations.SetBinding(
          bindingTarget,
          BindingResolver.ResolvedValueProperty,
          sourceBinding);

        // Delegate data source value to original target of the original Binding
        Binding targetBinding = CloneBinding(binding, this);
        targetBinding.Path = new PropertyPath(BindingResolver.ResolvedValueProperty);

        return targetBinding.ProvideValue(serviceProvider);
    }

    private static Binding CloneBinding(Binding binding)
    {
        Binding clonedBinding;
        if (!string.IsNullOrWhiteSpace(binding.ElementName))
        {
            clonedBinding = CloneBinding(binding, binding.ElementName);
        }
        else if (binding.Source != null)
        {
            clonedBinding = CloneBinding(binding, binding.Source);
        }
        else if (binding.RelativeSource != null)
        {
            clonedBinding = CloneBinding(binding, binding.RelativeSource);
        }
        else
        {
            clonedBinding = CloneBindingWithoutSource(binding);
        }

        return clonedBinding;
    }

    private static Binding CloneBinding(Binding binding, object bindingSource)
    {
        Binding clonedBinding = CloneBindingWithoutSource(binding);
        clonedBinding.Source = bindingSource;
        return clonedBinding;
    }

    private static Binding CloneBinding(Binding binding, RelativeSource relativeSource)
    {
        Binding clonedBinding = CloneBindingWithoutSource(binding);
        clonedBinding.RelativeSource = relativeSource;
        return clonedBinding;
    }

    private static Binding CloneBinding(Binding binding, string elementName)
    {
        Binding clonedBinding = CloneBindingWithoutSource(binding);
        clonedBinding.ElementName = elementName;
        return clonedBinding;
    }

    private static MultiBinding CloneBinding(MultiBinding binding)
    {
        IEnumerable<BindingBase> bindings = binding.Bindings;
        MultiBinding clonedBinding = CloneBindingWithoutSource(binding);
        bindings.ToList().ForEach(clonedBinding.Bindings.Add);
        return clonedBinding;
    }

    private static PriorityBinding CloneBinding(PriorityBinding binding)
    {
        IEnumerable<BindingBase> bindings = binding.Bindings;
        PriorityBinding clonedBinding = CloneBindingWithoutSource(binding);
        bindings.ToList().ForEach(clonedBinding.Bindings.Add);
        return clonedBinding;
    }

    private static TBinding CloneBindingWithoutSource<TBinding>(TBinding sourceBinding) where TBinding : BindingBase, new()
    {
        var clonedBinding = new TBinding();
        switch (sourceBinding)
        {
            case Binding binding:
                {
                    Binding newBinding = (clonedBinding as Binding)!;
                    newBinding.AsyncState = binding.AsyncState;
                    newBinding.BindingGroupName = binding.BindingGroupName;
                    newBinding.BindsDirectlyToSource = binding.BindsDirectlyToSource;
                    newBinding.Converter = binding.Converter;
                    newBinding.ConverterCulture = binding.ConverterCulture;
                    newBinding.ConverterParameter = binding.ConverterParameter;
                    newBinding.FallbackValue = binding.FallbackValue;
                    newBinding.IsAsync = binding.IsAsync;
                    newBinding.Mode = binding.Mode;
                    newBinding.NotifyOnSourceUpdated = binding.NotifyOnSourceUpdated;
                    newBinding.NotifyOnTargetUpdated = binding.NotifyOnTargetUpdated;
                    newBinding.NotifyOnValidationError = binding.NotifyOnValidationError;
                    newBinding.Path = binding.Path;
                    newBinding.StringFormat = binding.StringFormat;
                    newBinding.TargetNullValue = binding.TargetNullValue;
                    newBinding.UpdateSourceExceptionFilter = binding.UpdateSourceExceptionFilter;
                    newBinding.UpdateSourceTrigger = binding.UpdateSourceTrigger;
                    newBinding.ValidatesOnDataErrors = binding.ValidatesOnDataErrors;
                    newBinding.ValidatesOnExceptions = binding.ValidatesOnExceptions;
                    newBinding.XPath = binding.XPath;
                    newBinding.Delay = binding.Delay;
                    newBinding.ValidatesOnNotifyDataErrors = binding.ValidatesOnNotifyDataErrors;
                    binding.ValidationRules.ToList().ForEach(newBinding.ValidationRules.Add);
                    break;
                }
            case PriorityBinding priorityBinding:
                {
                    PriorityBinding newBinding = (clonedBinding as PriorityBinding)!;
                    newBinding.BindingGroupName = priorityBinding.BindingGroupName;
                    newBinding.FallbackValue = priorityBinding.FallbackValue;
                    newBinding.StringFormat = priorityBinding.StringFormat;
                    newBinding.TargetNullValue = priorityBinding.TargetNullValue;
                    newBinding.Delay = priorityBinding.Delay;
                    break;
                }
            case MultiBinding multiBinding:
                {
                    MultiBinding newBinding = (clonedBinding as MultiBinding)!;
                    newBinding.BindingGroupName = multiBinding.BindingGroupName;
                    newBinding.Converter = multiBinding.Converter;
                    newBinding.ConverterCulture = multiBinding.ConverterCulture;
                    newBinding.ConverterParameter = multiBinding.ConverterParameter;
                    newBinding.FallbackValue = multiBinding.FallbackValue;
                    newBinding.Mode = multiBinding.Mode;
                    newBinding.NotifyOnSourceUpdated = multiBinding.NotifyOnSourceUpdated;
                    newBinding.NotifyOnTargetUpdated = multiBinding.NotifyOnTargetUpdated;
                    newBinding.NotifyOnValidationError = multiBinding.NotifyOnValidationError;
                    newBinding.StringFormat = multiBinding.StringFormat;
                    newBinding.TargetNullValue = multiBinding.TargetNullValue;
                    newBinding.UpdateSourceExceptionFilter = multiBinding.UpdateSourceExceptionFilter;
                    newBinding.UpdateSourceTrigger = multiBinding.UpdateSourceTrigger;
                    newBinding.ValidatesOnDataErrors = multiBinding.ValidatesOnDataErrors;
                    newBinding.ValidatesOnExceptions = multiBinding.ValidatesOnExceptions;
                    newBinding.Delay = multiBinding.Delay;
                    newBinding.ValidatesOnNotifyDataErrors = multiBinding.ValidatesOnNotifyDataErrors;
                    multiBinding.ValidationRules.ToList().ForEach(newBinding.ValidationRules.Add);
                    break;
                }
            default:
                throw new NotSupportedException($"Binding type '{clonedBinding.GetType().FullName}' is not supported.");
        }

        return clonedBinding;
    }

    private static void OnResolvedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BindingResolver bindingResolver)
        {
            if (bindingResolver.IsUpDating)
            {
                return;
            }

            bindingResolver.IsUpDating = true;
            bindingResolver.UpdateSource();
            bindingResolver.IsUpDating = false;
        }
        else
        {
            if (BindingResolver.BindingTargetToBindingResolversMap.TryGetValue(d, out bindingResolver!))
            {
                if (bindingResolver.IsUpDating)
                {
                    return;
                }

                bindingResolver.IsUpDating = true;
                bindingResolver.UpdateTarget();
                bindingResolver.IsUpDating = false;
            }
        }
    }

    private static bool TryClearBindings(DependencyObject bindingTarget, BindingResolver bindingResolver)
    {
        if (bindingTarget == null)
        {
            return false;
        }

        Binding binding = BindingOperations.GetBinding(bindingTarget, bindingResolver.TargetProperty);
        if (binding != null && binding.Mode == BindingMode.OneTime)
        {
            BindingOperations.ClearBinding(bindingTarget, BindingResolver.ResolvedValueProperty);
            BindingOperations.ClearBinding(bindingTarget, bindingResolver.TargetProperty);
        }

        return true;
    }

    private void UpdateTarget()
    {
        if (!Target.TryGetTarget(out DependencyObject? target))
        {
            return;
        }

        object? resolvedValue = BindingResolver.GetResolvedValue(target);
        if (Converter is not null)
        {
            CultureInfo? converterCulture = OriginalBinding.TryGetTarget(out Binding? originalBinding) && originalBinding.ConverterCulture is not null
                ? originalBinding.ConverterCulture 
                : Thread.CurrentThread.CurrentUICulture;
            resolvedValue = Converter.Convert(resolvedValue, typeof(object), null, converterCulture);
        }
        else if (ResolvedSourceValueFilter is not null)
        {
            resolvedValue = ResolvedSourceValueFilter.Invoke(resolvedValue);
        }

        BindingResolver.SetResolvedValue(this, resolvedValue);
    }

    private void UpdateSource()
    {
        if (!Target.TryGetTarget(out DependencyObject? target))
        {
            return;
        }

        object? resolvedValue = BindingResolver.GetResolvedValue(this);
        if (Converter is not null)
        {
            CultureInfo? converterCulture = OriginalBinding.TryGetTarget(out Binding? originalBinding) && originalBinding.ConverterCulture is not null
                ? originalBinding.ConverterCulture
                : Thread.CurrentThread.CurrentUICulture;
            resolvedValue = Converter.ConvertBack(resolvedValue, typeof(object), null, converterCulture);
        }
        else if (ResolvedSourceValueFilter is not null)
        {
            resolvedValue = ResolvedTargetValueFilter.Invoke(resolvedValue);
        }

        BindingResolver.SetResolvedValue(target, resolvedValue);
    }
}