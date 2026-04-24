//#region Info

//// 2021/02/26  12:50
//// Net.Wpf

//#endregion

//using System;
//using System.Collections;
//using System.Reflection;
//using System.Windows;
//using System.Windows.Data;
//using System.Windows.Markup;

//namespace Net.Wpf
//{
//  class DictBindExtension : MarkupExtension
//  {
//    public object Source { get; }
//    public object Key { get; set; }
//    public string ValuePropertyName { get; set; }

//    public DictBindExtension(object source)
//    {
//      this.Source = source;
//      this.Key = null;
//      this.ValuePropertyName = string.Empty;
//    }

//    #region Overrides of MarkupExtension

//    /// <inheritdoc />
//    public override object ProvideValue(IServiceProvider serviceProvider)
//    {
//      IDictionary sourceDictionary = null;
//      switch (this.Source)
//      {
//        case IDictionary dictionary:
//          sourceDictionary = dictionary;
//          break;
//        case BindingBase binding:
//          var provideValueTargetService =
//            serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
//          object targetObject = provideValueTargetService?.TargetObject;
//          if (targetObject == null)
//          {
//            return this;
//          }

//          var bindingResolver = new BindingResolver(
//            targetObject as FrameworkElement,
//            provideValueTargetService.TargetProperty as DependencyProperty)
//          {
//            ResolvedSourceValueFilter = value => GetValueFromDictionary(value as IDictionary)
//          };

//          var filteredBindingBinding = bindingResolver.ResolveBinding(binding as Binding) as BindingBase;
//          return filteredBindingBinding?.ProvideValue(serviceProvider);
//        case MarkupExtension markup:
//          sourceDictionary = markup.ProvideValue(serviceProvider) as IDictionary;
//          break;
//      }


//      return GetValueFromDictionary(sourceDictionary);
//    }

//    private object GetValueFromDictionary(IDictionary sourceDictionary)
//    {
//      if (sourceDictionary == null)
//      {
//        throw new ArgumentNullException(nameof(sourceDictionary), "No source specified");
//      }

//      object value = sourceDictionary[this.Key];
//      PropertyInfo propertyInfo = value?.GetType().GetProperty(this.ValuePropertyName);
//      return propertyInfo == null ? null : propertyInfo.GetValue(value);
//    }

//    #endregion
//  }
//}