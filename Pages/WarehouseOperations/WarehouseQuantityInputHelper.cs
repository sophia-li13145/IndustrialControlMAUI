namespace IndustrialControlMAUI.Pages;

internal static class WarehouseQuantityInputHelper
{
    private const int MaxDecimalPlaces = 4;

    public static bool RejectIfTooManyDecimalPlaces(Entry entry, TextChangedEventArgs e)
    {
        if (HasTooManyDecimalPlaces(e.NewTextValue))
        {
            entry.Text = e.OldTextValue;
            return true;
        }

        return false;
    }

    private static bool HasTooManyDecimalPlaces(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var dotIndex = text.IndexOf('.');
        if (dotIndex < 0) dotIndex = text.IndexOf('。');
        if (dotIndex < 0) return false;

        return text.Length - dotIndex - 1 > MaxDecimalPlaces;
    }
}
