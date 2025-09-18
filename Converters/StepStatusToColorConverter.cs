using System.Globalization;

namespace IndustrialControlMAUI.Converters
{
    public sealed class StepStatusToColorConverter : IValueConverter
    {
        // "完成" -> 绿；"进行中" -> 蓝；其他/空 -> 灰
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (value as string)?.Trim();
            if (string.Equals(s, "完成", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb("#4CAF50");
            if (string.Equals(s, "进行中", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb("#3F88F7");
            return Color.FromArgb("#BFC6D4");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class StepStatusToTextColorConverter : IValueConverter
    {
        // 圆点内部数字的前景色：深色（完成/进行中）- 白；未开始 - #607080
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (value as string)?.Trim();
            if (string.Equals(s, "完成", StringComparison.OrdinalIgnoreCase)) return Colors.White;
            if (string.Equals(s, "进行中", StringComparison.OrdinalIgnoreCase)) return Colors.White;
            return Color.FromArgb("#607080");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
