namespace BionicAthlete.Presentation;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using BionicCode.Utilities.Net;

[Localizability(LocalizationCategory.NeverLocalize)]
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Visibility hiddenVisibility = parameter is Visibility visibility
            ? visibility
            : Visibility.Collapsed;
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<Visibility>(hiddenVisibility);

        return (bool)value
            ? Visibility.Visible
            : hiddenVisibility;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<Visibility>((Visibility)value);
        return value is Visibility visibility && visibility is Visibility.Visible;
    }
}
