using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.App.Converters;

// Returns Visible when RateType == PerSqft (dimension fields shown)
public class RateTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is RateType.PerSqft ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
