using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Services;

public interface IInboundMaterialService
{

    // NEW: 查询列表（图1）
    Task<IEnumerable<InboundOrderSummary>> ListInboundOrdersAsync(
   string? orderNoOrBarcode,
   DateTime startDate,
   DateTime endDate,
   string orderType,
   string[] orderTypeList,
   CancellationToken ct = default);

    Task<IReadOnlyList<InboundPendingRow>> GetInStockDetailAsync(string instockId, CancellationToken ct = default);
    Task<IReadOnlyList<InboundScannedRow>> GetInStockScanDetailAsync(string instockId, CancellationToken ct = default);
    /// <summary>扫描条码入库</summary>
    Task<SimpleOk> InStockByBarcodeAsync(string instockId, string barcode, CancellationToken ct = default);
    /// <summary>PDA 扫描通过（确认当前入库单已扫描项）</summary>
    // IInboundMaterialService.cs
    Task<SimpleOk> ScanConfirmAsync(IEnumerable<(string barcode, string id)> items, CancellationToken ct = default);
    Task<SimpleOk> CancelScanAsync(IEnumerable<(string barcode, string id)> items, CancellationToken ct = default);

    Task<SimpleOk> ConfirmInstockAsync(string instockId, CancellationToken ct = default);
    /// <summary>判断入库单明细是否全部扫码确认</summary>
    Task<bool> JudgeInstockDetailScanAllAsync(string instockId, CancellationToken ct = default);


    // 新增：按你截图的真实接口返回结构（树形）
    Task<List<LocationNodeDto>> GetLocationTreeAsync(CancellationToken ct = default);

    Task<List<BinInfo>> GetBinsByLayerAsync(
string warehouseCode, string layer,
int pageNo = 1, int pageSize = 50, int status = 1,
CancellationToken ct = default);

    /// <summary>更新扫描明细的库位（/normalService/pda/wmsMaterialInstock/updateLocation）</summary>
    Task<SimpleOk> UpdateInstockLocationAsync(
        string detailId,
        string id,
        string instockWarehouse,
        string instockWarehouseCode,
        string location,
        CancellationToken ct = default);

    Task<SimpleOk> UpdateQuantityAsync(
    string barcode, string detailId, string id, int quantity, CancellationToken ct = default);

}

public record InboundOrder(string OrderNo, string Supplier, string LinkedNo, int ExpectedQty);

public record OutboundOrder(string OrderNo, string Supplier, string LinkedNo, int ExpectedQty);
public record ScanItem(int Index, string Barcode, string? Bin, int Qty);
public record SimpleOk(bool Succeeded, string? Message = null);


