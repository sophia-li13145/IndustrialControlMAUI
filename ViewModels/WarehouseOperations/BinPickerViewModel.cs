// 修改点：1) ToTree 不再丢弃 storage_rack_layer，而是生成 Level=3 的节点
//        2) SelectNode 在 Level=3 时调用 GetBinsByLayerAsync，然后用 BinListPage 展示

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class BinPickerViewModel : ObservableObject
{
    public ObservableCollection<LocationTreeNodeVM> Tree { get; } = new();
    public ObservableCollection<LocationTreeNodeVM> VisibleTree { get; } = new();

    private readonly IInboundMaterialService _api;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorText;

    private Func<BinInfo, Task>? _onPicked;
    private Func<Task>? _onCanceled;

    public BinPickerViewModel(IInboundMaterialService api) => _api = api;

    public void SetCloseHandlers(Func<BinInfo, Task> onPicked, Func<Task> onCanceled)
    {
        _onPicked = onPicked;
        _onCanceled = onCanceled;
    }

    public async void Initialize(string? preselectBinCode) => await LoadAsync();

    private async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            IsBusy = true;
            ErrorText = null;

            var roots = await _api.GetLocationTreeAsync(ct);
            Tree.Clear();

            foreach (var r in roots)
            {
                var node = ToTree(r, parentPath: null);
                if (node != null) Tree.Add(node);
            }

            foreach (var root in Tree) root.IsExpanded = true;
            RebuildVisible();
        }
        catch (Exception ex)
        {
            ErrorText = $"加载库位树失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 递归把 DTO 转为树节点；包含“货架层”
    private LocationTreeNodeVM? ToTree(LocationNodeDto dto, string? parentPath)
    {
        var (level, displayName) = dto.catalogueType?.ToLowerInvariant() switch
        {
            "warehouse" => (0, dto.warehouseName ?? dto.catalogueName ?? "仓库"),
            "area" => (1, dto.catalogueName ?? dto.parentName ?? "区域"),
            "storage_rack" => (2, dto.catalogueName ?? "货架"),
            "storage_rack_layer" => (3, dto.catalogueName ?? (dto.location ?? "货架层")),
            _ => (0, dto.catalogueName ?? dto.warehouseName ?? dto.parentName ?? "节点")
        };

        var currentPath = string.IsNullOrWhiteSpace(parentPath) ? displayName : $"{parentPath}/{displayName}";

        // 直接创建节点（包含层），并继续递归 children
        var node = new LocationTreeNodeVM(
            name: displayName,
            level: level,
            path: currentPath,
            catalogueType: dto.catalogueType,
            warehouseCode: dto.warehouseCode,
            layerCode: dto.location ?? dto.catalogueCode // 层编码：后端一般在 location/catelogueCode
        );

        foreach (var child in dto.children)
        {
            var childNode = ToTree(child, currentPath);
            if (childNode != null) node.Children.Add(childNode);
        }

        return node;
    }

    [RelayCommand]
    private void Toggle(LocationTreeNodeVM node)
    {
        node.IsExpanded = !node.IsExpanded;
        RebuildVisible();
    }

    [RelayCommand]
    private async Task SelectNode(LocationTreeNodeVM node)
    {
        // 只有选择“货架层”时才拉库位
        if (!string.Equals(node.CatalogueType, "storage_rack_layer", StringComparison.OrdinalIgnoreCase) || node.Level != 3)
        {
            node.IsExpanded = !node.IsExpanded;
            RebuildVisible();
            return;
        }

        try
        {
            IsBusy = true;
            // 传递：warehouseCode + layer（层编码）
            var bins = await _api.GetBinsByLayerAsync(node.WarehouseCode, node.LayerCode, pageNo: 1, pageSize: 50, status: 1);

            // 弹出库位列表；closeParent=true 选中后会连 BinPickerPage 一并关闭
            var picked = await BinListPage.ShowAsync(bins, closeParent: true);
            if (picked == null) return;
            if (_onPicked != null) await _onPicked.Invoke(picked);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("错误", $"获取库位失败：{ex.Message}", "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public Task CancelAsync() => _onCanceled?.Invoke() ?? Task.CompletedTask;

    private void RebuildVisible()
    {
        VisibleTree.Clear();
        foreach (var root in Tree) AddVisible(root);
    }

    private void AddVisible(LocationTreeNodeVM n)
    {
        VisibleTree.Add(n);
        if (!n.IsExpanded) return;
        foreach (var c in n.Children) AddVisible(c);
    }
}
