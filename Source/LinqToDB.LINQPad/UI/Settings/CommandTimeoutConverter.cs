using System;
using System.Globalization;
using System.Windows.Data;

namespace LinqToDB.LINQPad.UI;

#pragma warning disable CA1812 // Remove unused type
sealed class CommandTimeoutConverter : IValueConverter
#pragma warning restore CA1812 // Remove unused type
{
	public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int intVal)
			return intVal.ToString(culture);

		if (value is string strValue)
			return strValue;

		return string.Empty;
	}

	public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int intValue && intValue >= 0)
			return intValue;

		if (value is string strValue && int.TryParse(strValue, NumberStyles.Integer, culture, out var intVal) && intVal >= 0)
			return intVal;

		return null;
	}
}
