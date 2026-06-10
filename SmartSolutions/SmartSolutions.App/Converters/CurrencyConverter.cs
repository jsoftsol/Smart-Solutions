using System.Globalization;
using System.Windows.Data;

namespace SmartSolutions.App.Converters;

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is decimal d ? $"PKR {d:#,##0.00}" : "PKR 0.00";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
