using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class ReferenceImagePreviewPopup : Popup
{
    public ObservableCollection<OrderQualityAttachmentItem> Images { get; }

    public ReferenceImagePreviewPopup(IEnumerable<OrderQualityAttachmentItem> images)
    {
        InitializeComponent();
        Images = new ObservableCollection<OrderQualityAttachmentItem>(images);
        BindingContext = this;
    }

    public static async Task ShowAsync(IEnumerable<OrderQualityAttachmentItem> images)
    {
        var popup = new ReferenceImagePreviewPopup(images);
        var page = Application.Current?.Windows.FirstOrDefault()?.Page ?? Application.Current?.MainPage;
        if (page is null) return;

        await page.ShowPopupAsync(popup);
    }

    private void OnCloseClicked(object? sender, EventArgs e)
    {
        Close();
    }
}
