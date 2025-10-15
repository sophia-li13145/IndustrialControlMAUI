using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;
using System.Text.Json;

namespace IndustrialControlMAUI.Pages;

public partial class WorkOrderSearchPage : ContentPage
{
    private readonly ScanService _scanSvc;
    private readonly WorkOrderSearchViewModel _vm;

    public WorkOrderSearchPage(WorkOrderSearchViewModel vm, ScanService scanSvc)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        //_scanSvc = scanSvc;

        //// 与现有页面一致的扫码策略
        //_scanSvc.Prefix = null;
        //_scanSvc.Suffix = null;
        //_scanSvc.DebounceMs = 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        //_scanSvc.Scanned += OnScanned;
        //_scanSvc.StartListening();
        //_scanSvc.Attach(OrderEntry);   // 支持键盘/手持枪统一输入
        OrderEntry.Focus();
    }

    protected override void OnDisappearing()
    {
        //_scanSvc.Scanned -= OnScanned;
        //_scanSvc.StopListening();
        base.OnDisappearing();
    }

    private void OnScanned(string data, string type)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _vm.Keyword = data;   // 回填查询条件
            await _vm.SearchAsync();    // 触发查询
        });
    }

    // 点整张卡片跳转（推荐）

    private async void OnOrderTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (e.Parameter is not WorkOrderDto item) return;

            var json = JsonSerializer.Serialize(item);

            await Shell.Current.GoToAsync(
                nameof(MoldOutboundExecutePage),
                new Dictionary<string, object?>
                {
                    // 执行页用 IQueryAttributable 接收：key 必须叫 "orderDto"
                    ["orderDto"] = json
                });
        }
        catch (Exception ex)
        {
            await DisplayAlert("导航失败", ex.Message, "确定");
        }
    }


    private async void OnScanHintClicked(object sender, EventArgs e)
            => await DisplayAlert("提示", "此按钮预留摄像头扫码；硬件扫描直接扣扳机。", "确定");
}
