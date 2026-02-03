using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;
using System.Text.Json;

namespace IndustrialControlMAUI.Pages;

// 注意：不再使用 [QueryProperty]，改用 IQueryAttributable
public partial class MoldOutboundExecutePage : ContentPage, IQueryAttributable
{
    private readonly MoldOutboundExecuteViewModel _vm;

    /// <summary>执行 MoldOutboundExecutePage 初始化逻辑。</summary>
    public MoldOutboundExecutePage(MoldOutboundExecuteViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    // Shell 在导航时会调用这里，参数肯定已到
    /// <summary>执行 ApplyQueryAttributes 逻辑。</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // 1) 优先：整条 WorkOrderDto（JSON 传参）
        if (query.TryGetValue("baseInfo", out var obj) && obj is string json && !string.IsNullOrWhiteSpace(json))
        {
            var dto = JsonSerializer.Deserialize<WorkOrderDto>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto != null)
            {
                _vm.OrderNo = dto.OrderNo;
                _vm.OrderId = dto.Id;
                _vm.StatusText = dto.Status;
                _vm.OrderName = dto.OrderName;
                _vm.Urgent = dto.Urgent;
                _vm.ProductName = dto.MaterialName;
                _vm.PlanQtyText = dto.CurQty?.ToString();
                _vm.CreateDateText = dto.CreateDate;
                _vm.BomCode = dto.BomCode;
                _vm.RouteName = dto.RouteName;
                _vm.PlanStartText = dto.PlanStartText;
                return; // 已就绪
            }
        }

        // 2) 兼容：旧的 orderNo/orderId/baseInfo 三参数
        if (query.TryGetValue("orderNo", out var ono)) _vm.OrderNo = ono as string;
        if (query.TryGetValue("orderId", out var oid)) _vm.OrderId = oid as string;

    }


    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!string.IsNullOrWhiteSpace(_vm.OrderNo))
            await _vm.LoadAsync(_vm.OrderNo!, _vm.OrderId);
    }

}
