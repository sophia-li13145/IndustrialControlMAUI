using System.Globalization;

namespace IndustrialControlMAUI.Converters;

public class EyeGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return "\uE70E"; // 眼睛（显示密码）
        return "\uEB11";    // 眼睛关闭（隐藏密码）
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
