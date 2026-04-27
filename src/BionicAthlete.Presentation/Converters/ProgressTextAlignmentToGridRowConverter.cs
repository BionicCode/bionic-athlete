namespace BionicAthlete.Presentation;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class ProgressTextAlignmentToGridRowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProgressTextAlignment alignment)
        {
            return alignment switch
            {
                ProgressTextAlignment.Left => 1,
                ProgressTextAlignment.Center => 1,
                ProgressTextAlignment.Right => 1,
                ProgressTextAlignment.TopCenter => 0,
                ProgressTextAlignment.BottomCenter => 2,
                _ => 1
            };
        }

        return 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
