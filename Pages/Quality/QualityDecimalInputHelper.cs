namespace IndustrialControlMAUI.Pages;

internal static class QualityDecimalInputHelper
{
    private const int MaxDecimalPlaces = 4;

    public static bool RejectIfTooManyDecimalPlaces(Entry entry, TextChangedEventArgs e)
    {
        if (!HasTooManyDecimalPlaces(e.NewTextValue))
        {
            return false;
        }

        entry.Text = e.OldTextValue;
        return true;
    }

    private static bool HasTooManyDecimalPlaces(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var dotIndex = text.IndexOf('.');
        if (dotIndex < 0)
        {
            return false;
        }

        return text.Length - dotIndex - 1 > MaxDecimalPlaces;
    }
}
