using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;

public interface IBinPickerService
{
    Task<BinInfo?> PickAsync(string? preselectBin = null);
}

public class BinPickerService : IBinPickerService
{
    public Task<BinInfo?> PickAsync(string? preselectBin = null)
        => BinPickerPage.ShowAsync(preselectBin);
}
