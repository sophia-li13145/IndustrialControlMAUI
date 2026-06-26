using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class ReferenceImagePreviewPopup : Popup
{
    private readonly TaskCompletionSource _tcs = new();

    public ObservableCollection<OrderQualityAttachmentItem> Images { get; }

    public ReferenceImagePreviewPopup(IEnumerable<OrderQualityAttachmentItem> images)
    {
        InitializeComponent();
        Images = new ObservableCollection<OrderQualityAttachmentItem>(images);
        BindingContext = this;
        Closed += (_, _) => _tcs.TrySetResult();
    }

    public static Task ShowAsync(IEnumerable<OrderQualityAttachmentItem> images)
    {
        var popup = new ReferenceImagePreviewPopup(images);
        Application.Current?.MainPage?.ShowPopup(popup);
        return popup._tcs.Task;
    }

    private void OnCloseClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult();
        Close();
    }
}
