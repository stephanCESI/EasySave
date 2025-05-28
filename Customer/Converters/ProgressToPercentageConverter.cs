using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Customer.Converters;
public class ProgressToPercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            int percentage = (int)(progress * 100);
            return $"{percentage} %";
        }

        return "0 %";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
