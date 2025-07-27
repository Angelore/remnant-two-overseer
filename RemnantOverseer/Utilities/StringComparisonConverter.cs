using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace RemnantOverseer.Utilities;
public class StringComparisonConverter: IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;

        if (value is string comparableValue && parameter is string strParameter)
        {
            return comparableValue.Equals(strParameter);
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
