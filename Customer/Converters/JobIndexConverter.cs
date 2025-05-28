using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Customer.Model;

namespace Customer.Converters;

public class JobIndexConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is IList<BackupJob> jobs && values[1] is BackupJob currentJob)
        {
            return jobs.IndexOf(currentJob);
        }
        return 0; // Retourne 0 si les valeurs ne sont pas valides
    }


    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
