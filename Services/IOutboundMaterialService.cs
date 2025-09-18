using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Services;

public interface IOutboundMaterialService
{

    // NEW: 查询列表（图1）
    Task<IEnumerable<OutboundOrderSummary>> ListOutboundOrdersAsync(
   string? orderNoOrBarcode,
   DateTime startDate,
   DateTime endDate,
   string[] outstockStatusList,
   string orderType,
   string[] orderTypeList,
   CancellationToken ct = default);

    Task<IReadOnlyList<OutboundPendingRow>> GetOutStockDetailAsync(string outstockId, CancellationToken ct = default);
    Task<IReadOnlyList<OutboundScannedRow>> GetOutStockScanDetailAsync(string outstockId, CancellationToken ct = default);
    /// <summary>扫描条码入库</summary>
    Task<SimpleOk> OutStockByBarcodeAsync(string outstockId, string barcode, CancellationToken ct = default);
    /// <summary>PDA 扫描通过（确认当前入库单已扫描项）</summary>
    // IOutboundMaterialService.cs
    Task<SimpleOk> ScanConfirmAsync(IEnumerable<(string barcode, string id)> items, CancellationToken ct = default);
    Task<SimpleOk> CancelScanAsync(IEnumerable<(string barcode, string id)> items, CancellationToken ct = default);

    Task<SimpleOk> ConfirmOutstockAsync(string outstockId, CancellationToken ct = default);
    /// <summary>判断入库单明细是否全部扫码确认</summary>
    Task<bool> JudgeOutstockDetailScanAllAsync(string outstockId, CancellationToken ct = default);


    Task<SimpleOk> UpdateOutstockLocationAsync(
        string detailId,
        string id,
        string outstockWarehouse,
        string outstockWarehouseCode,
        string location,
        CancellationToken ct = default);

    Task<SimpleOk> UpdateQuantityAsync(
    string barcode, string detailId, string id, int quantity, CancellationToken ct = default);

}




