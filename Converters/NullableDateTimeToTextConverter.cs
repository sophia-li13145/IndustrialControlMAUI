using System.Globalization;

namespace IndustrialControlMAUI.Converters;

public class NullableDateTimeToTextConverter : IValueConverter
{
    public string Placeholder { get; set; } = "清选择";
    public string Format { get; set; } = "yyyy-MM-dd HH:mm";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return dt.ToString(Format, culture);

        return Placeholder;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

