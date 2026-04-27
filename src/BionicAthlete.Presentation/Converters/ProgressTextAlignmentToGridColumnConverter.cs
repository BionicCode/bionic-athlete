namespace BionicAthlete.Presentation;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class ProgressTextAlignmentToGridColumnConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProgressTextAlignment alignment)
        {
            return alignment switch
            {
                ProgressTextAlignment.Left => 0,
                ProgressTextAlignment.Center => 1,
                ProgressTextAlignment.Right => 2,
                ProgressTextAlignment.TopCenter => 1,
                ProgressTextAlignment.BottomCenter => 1,
                _ => 1
            };
        }

        return 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}