using System.Globalization;

namespace IndustrialControlMAUI.Converters;
public class IntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() ?? "0";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value?.ToString();
        return int.TryParse(s, out var n) && n >= 0 ? n : 0; // 非法输入归零；需要可负数就去掉判断
    }
}
