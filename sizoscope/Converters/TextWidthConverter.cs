using Avalonia.Data.Converters;
using System.Globalization;

namespace sizoscope.Converters;

public class TextWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            return width - 32;
        }

        throw new InvalidOperationException();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
