namespace FitToCsvConverter.Controls;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class DoubleToCornerRadiusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new CornerRadius((double)value);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => ((CornerRadius)value).TopLeft;
}
