
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class WarehouseLocationPickerViewModel : ObservableObject
    {
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<WarehouseVM> Warehouses { get; } = new();

        [ObservableProperty] private string? scanText;

        private readonly IWarehouseService _svc;

        /// <summary>页面希望拿到选中库位时的回调（可选）。</summary>
        public Func<LocationVM, Task>? RequestCloseWithResult { get; set; }
        public Func<WarehouseVM, LocationVM?, Task>? RequestReveal { get; set; }

        /// <summary>初始化任务：完成“查询所有仓库并填充列表”。后续逻辑需先 await 它。</summary>
        public Task Initialization { get; }

        /// <summary>执行 WarehouseLocationPickerViewModel 初始化逻辑。</summary>
        public WarehouseLocationPickerViewModel(IWarehouseService svc)
        {
            _svc = svc;
            Initialization = InitAsync();
        }

        /// <summary>执行 InitAsync 逻辑。</summary>
        private async Task InitAsync()
        {
            try
            {
                // 1) 后台拿仓库清单
                var list = await _svc.QueryAllWarehouseAsync().ConfigureAwait(false);

                // 2) 构造 VM 列表
                var vms = list.Select(w => new WarehouseVM(
                    code: w.WarehouseCode,
                    name: string.IsNullOrWhiteSpace(w.WarehouseName) ? w.WarehouseCode : w.WarehouseName,
                    svc: _svc)).ToList();

                // 3) 主线程更新集合
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Warehouses.Clear();
                    foreach (var vm in vms) Warehouses.Add(vm);
                });

                // 4) （可选）默认展开第一个，并在后台触发一次加载
                if (Warehouses.Count > 0)
                {
                    Warehouses[0].IsExpanded = true;
                    _ = Warehouses[0].EnsureLoadedAsync();
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current!.MainPage!.DisplayAlert("错误", $"加载仓库失败：{ex.Message}", "确定"));
            }
        }

        // —— Commands ——

        /// <summary>执行 ToggleExpand 逻辑。</summary>
        [RelayCommand]
        private async Task ToggleExpand(WarehouseVM? vm)
        {
            if (vm is null) return;

            // 保证初始化完成后再允许加载库位
            await Initialization.ConfigureAwait(false);

            foreach (var w in Warehouses)
                if (!ReferenceEquals(w, vm)) w.IsExpanded = false;

            if (!vm.IsExpanded)
            {
                vm.IsExpanded = true;
                await vm.EnsureLoadedAsync().ConfigureAwait(false);
            }
            else
            {
                vm.IsExpanded = false;
            }
        }

        /// <summary>执行 SelectLocation 逻辑。</summary>
        [RelayCommand]
        private async Task SelectLocation(LocationVM vm)
        {
            if (vm is null) return;
            if (RequestCloseWithResult is not null)
                await RequestCloseWithResult(vm);
        }

        // WarehouseLocationPickerViewModel.cs

        /// <summary>执行 TryParseWarehouseAndLocation 逻辑。</summary>
        private static bool TryParseWarehouseAndLocation(string raw, out string warehouseCode, out string locationCode)
        {
            warehouseCode = "";
            locationCode = "";
            if (string.IsNullOrWhiteSpace(raw)) return false;

            // 规范：仓库编码#%库位编码
            var idx = raw.IndexOf("#%", StringComparison.Ordinal);
            if (idx < 0) return false;

            warehouseCode = raw[..idx].Trim();
            locationCode = raw[(idx + 2)..].Trim();
            return !string.IsNullOrEmpty(warehouseCode) && !string.IsNullOrEmpty(locationCode);
        }

        /// <summary>执行 ScanSubmit 逻辑。</summary>
        [RelayCommand]
        private async Task ScanSubmit()
        {
            await Initialization.ConfigureAwait(false);

            var scan = (ScanText ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(scan)) return;

            // 只按“仓库编码#%库位编码”解析
            if (!TryParseWarehouseAndLocation(scan, out var whCode, out var locCode))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current!.MainPage!.DisplayAlert("提示", "条码格式应为：仓库编码#%库位编码", "确定"));
                return;
            }

            // 1) 找到指定仓库
            var wh = Warehouses.FirstOrDefault(w =>
                string.Equals(w.Code, whCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(w.Name, whCode, StringComparison.OrdinalIgnoreCase));

            if (wh is null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current!.MainPage!.DisplayAlert("提示", $"未找到仓库：{whCode}", "确定"));
                return;
            }

            // 2) 确保该仓库数据已加载
            wh.IsExpanded = true;               // UI 先展开（也会触发懒加载），但我们仍主动确保完成
            await wh.EnsureLoadedAsync().ConfigureAwait(false);

            // 3) 在该仓库内查找库位（用 Location 或按钮显示文本匹配）
            var hit = wh.Locations.FirstOrDefault(l =>
                string.Equals(l.Location, locCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(l.DisplayText, locCode, StringComparison.OrdinalIgnoreCase));

            if (hit is null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current!.MainPage!.DisplayAlert("提示", $"在仓库 {wh.Name} 中未找到库位：{locCode}", "确定"));
                return;
            }

            // 4) 滚动到该仓库（如果你在 .xaml.cs 里已接好 RequestReveal）
            if (RequestReveal is not null)
                await RequestReveal(wh, hit);

            // 5) 让命中的库位临时高亮（蓝色）
            await HighlightAsync(hit);
        }


        // 小工具：把库位高亮 1.2 秒
        /// <summary>执行 HighlightAsync 逻辑。</summary>
        private static async Task HighlightAsync(LocationVM vm)
        {
            vm.IsHighlighted = true;
            try { await Task.Delay(1200).ConfigureAwait(false); }
            finally { vm.IsHighlighted = false; }
        }

    }

    /// <summary>单个仓库 VM：展开时懒加载，仅加载一次；UI 线程更新集合。</summary>
    public partial class WarehouseVM : ObservableObject
    {
        private static readonly SemaphoreSlim _globalSerial = new(1, 1); // ⬅️ 所有仓库加载串行
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<LocationGroupVM> Groups { get; } = new(); // ✅ 新增：分组集合
        public string Code { get; }
        public string Name { get; }
        public string Title => Name;

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<LocationVM> Locations { get; } = new();
        public IRelayCommand ToggleCommand { get; }

        [ObservableProperty] private bool isExpanded;
        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool isLoaded;

        private readonly IWarehouseService _svc;

        // 一次性加载的任务缓存 + 并发锁
        private readonly SemaphoreSlim _once = new(1, 1);
        private Task? _firstLoad;


        /// <summary>执行 WarehouseVM 初始化逻辑。</summary>
        public WarehouseVM(string code, string name, IWarehouseService svc)
        {
            Code = code;
            Name = name;
            _svc = svc;
            ToggleCommand = new RelayCommand(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[Toggle] {Title}");
                IsExpanded = !IsExpanded;
            });
        }

        /// <summary>只加载一次；并发安全；仅在更新集合时切回 UI 线程。</summary>
        public async Task EnsureLoadedAsync(CancellationToken ct = default)
        {
            if (IsLoaded) return;

            await _once.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                _firstLoad ??= LoadOnceCoreAsync(ct);
            }
            finally
            {
                _once.Release();
            }

            await _firstLoad.ConfigureAwait(false);
        }

        /// <summary>执行 LoadOnceCoreAsync 逻辑。</summary>
        private async Task LoadOnceCoreAsync(CancellationToken ct)
        {
            if (IsLoaded || IsLoading) return;
            IsLoading = true;
            try
            {
                // ✅ 改为“分组查询”
                var segments = await _svc.QueryLocationSegmentsByWarehouseCodeAsync(Code, ct).ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Groups.Clear();
                    int idx = 0;
                    foreach (var seg in segments)
                    {
                        var vms = seg.Items.Select(m => new LocationVM(m));
                        var group = new LocationGroupVM(seg.Zone, vms)
                        {
                            IsFirst = (idx == 0)
                        };
                        Groups.Add(group);
                        idx++;
                    }
                    IsLoaded = true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadOnceCoreAsync FAIL] {ex}");
                throw;
            }
            finally { IsLoading = false; }
        }

        /// <summary>执行 OnIsExpandedChanged 逻辑。</summary>
        partial void OnIsExpandedChanged(bool value)
        {
            if (value)
            {
                _ = EnsureLoadedAsync();
            }
        }
    }

    /// <summary>UI 库位 VM（由 LocationItem 映射）。</summary>
    public partial class LocationVM : ObservableObject
    {
        public string WarehouseCode { get; }
        public string WarehouseName { get; }
        public string Zone { get; }
        public string Rack { get; }
        public string Layer { get; }
        public string Location { get; }
        public string InventoryStatus { get; }
        [ObservableProperty] private bool isHighlighted;

        /// <summary>执行 IsNullOrWhiteSpace 逻辑。</summary>
        public string DisplayText => string.IsNullOrWhiteSpace(Location) ? $"{Rack}-{Layer}" : Location;

        /// <summary>执行 LocationVM 初始化逻辑。</summary>
        public LocationVM(LocationItem m)
        {
            WarehouseCode = m.WarehouseCode;
            WarehouseName = m.WarehouseName;
            Zone = m.Zone;
            Rack = m.Rack;
            Layer = m.Layer;
            Location = m.Location;
            InventoryStatus = m.InventoryStatus;
        }
    }

    public partial class LocationGroupVM : ObservableCollection<LocationVM>
    {
        public string Zone { get; }
        /// <summary>执行 IsNullOrWhiteSpace 逻辑。</summary>
        public string Title => string.IsNullOrWhiteSpace(Zone) ? " " : Zone;

        // 是否为第一组（用来控制是否显示顶部虚线分隔）
        public bool IsFirst { get; set; }

        /// <summary>执行 LocationGroupVM 初始化逻辑。</summary>
        public LocationGroupVM(string zone, IEnumerable<LocationVM> items) : base(items)
        {
            Zone = zone ?? "";
        }
    }
}