using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IMoldApi
    {
        Task<MoldOutScanQueryResp?> InStockScanQueryAsync(
   string code, CancellationToken ct = default);
        Task<SimpleOk> ConfirmInStockByListAsync(InStockConfirmReq req, CancellationToken ct = default);

        //-------------------------------出库-------------------------------------------
        /// <summary>
        /// 返回页面所需：工单号、物料名称、以及“型号+基础需求数量+模具编码列表”的分组视图
        /// </summary>
        Task<WorkOrderMoldView> GetViewAsync(string workOrderNo, CancellationToken ct = default);

        /// <summary>出库扫描查询</summary>
        Task<MoldOutScanQueryResp?> OutStockScanQueryAsync(string code, string workOrderNo, CancellationToken ct = default);

        /// <summary>确认出库</summary>
        Task<SimpleOk> ConfirmOutStockAsync(MoldOutConfirmReq req, CancellationToken ct = default);
    }

    // ===================== 实现 =====================
    public class MoldApi : IMoldApi
    {
        private readonly HttpClient _http;
        private readonly AuthState _auth;

        // 统一由 appconfig.json 管理的端点路径（相对服务路径）
        private readonly string _inStockScanQueryEndpoint;
        private readonly string _confirmInStockEndpoint;
        private readonly string _outStockScanQueryEndpoint;
        private readonly string _outStockConfirmEndpoint;
        private readonly string _queryForWorkOrderEndpoint;

        public MoldApi(HttpClient http, IConfigLoader configLoader, AuthState auth)
        {
            _http = http;

            if (_http.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                _http.Timeout = TimeSpan.FromSeconds(15);

            // === 基地址 ===
            var baseUrl = configLoader.GetBaseUrl();
            if (_http.BaseAddress is null)
                _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

            // 服务路径（如 /normalService），用于去重
            var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";

            // 接受 JSON
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


            _confirmInStockEndpoint = ServiceUrlHelper.NormalizeRelative(
                configLoader.GetApiPath("mold.inStock", "/pda/mold/inStock"),
                servicePath);
            _inStockScanQueryEndpoint = ServiceUrlHelper.NormalizeRelative(
               configLoader.GetApiPath("mold.inStockScanQuery", "/pda/mold/inStockScanQuery"),
               servicePath);

            //-------------------------------出库-------------------------------------------
            _queryForWorkOrderEndpoint = ServiceUrlHelper.NormalizeRelative(
                configLoader.GetApiPath("mold.queryForWorkOrder", "/pda/mold/queryForWorkOrder"),
                servicePath);

            _outStockScanQueryEndpoint = ServiceUrlHelper.NormalizeRelative(
                configLoader.GetApiPath("mold.outStockScanQuery", "/pda/mold/outStockScanQuery"),
                servicePath);

            _outStockConfirmEndpoint = ServiceUrlHelper.NormalizeRelative(
                configLoader.GetApiPath("mold.outStock", "/pda/mold/outStock"),
                servicePath);
            _auth = auth;
        }

        static int ToInt(decimal? v) => v.HasValue ? (int)Math.Round(v.Value, MidpointRounding.AwayFromZero) : 0;
       /// <summary>
       /// 入库扫描查询
       /// </summary>
       /// <param name="code"></param>
       /// <param name="ct"></param>
       /// <returns></returns>
        public async Task<MoldOutScanQueryResp?> InStockScanQueryAsync(string code, CancellationToken ct = default)
        {
            // 构造查询串
            var qs = $"code={Uri.EscapeDataString(code ?? string.Empty)}";
            var url = _inStockScanQueryEndpoint + (_inStockScanQueryEndpoint.Contains("?") ? "&" : "?") + qs;
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            req.Headers.Accept.Clear();
            req.Headers.Accept.ParseAdd("application/json");

            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct).ConfigureAwait(false);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (!res.IsSuccessStatusCode || string.IsNullOrWhiteSpace(json))
            {
                return new MoldOutScanQueryResp
                {
                    success = false,
                    message = !res.IsSuccessStatusCode ? $"HTTP {(int)res.StatusCode}: {json}" : "空响应"
                };
            }

            try
            {
                return JsonSerializer.Deserialize<MoldOutScanQueryResp>(json, options);
            }
            catch (JsonException)
            {
                return new MoldOutScanQueryResp
                {
                    success = false,
                    message = "响应解析失败"
                };
            }
        }

        // =============== 确认入库（POST） ===============
        public async Task<SimpleOk> ConfirmInStockByListAsync(InStockConfirmReq req, CancellationToken ct = default)
        {
            var body = JsonSerializer.Serialize(req);
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _confirmInStockEndpoint);

            using var msg = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
            { Content = new StringContent(body, Encoding.UTF8, "application/json") };

            using var res = await _http.SendAsync(msg, ct);
            var txt = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            var dto = JsonSerializer.Deserialize<ConfirmResp>(
                txt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var ok = dto?.success == true && dto.result == true;
            return new SimpleOk(ok, dto?.message ?? (ok ? "确认入库成功" : "确认入库失败"));
        }

        //------------------------------- 出库：页面视图 -------------------------------
        /// <summary>
        /// 把 /pda/mold/queryForWorkOrder 的返回映射为页面所需分组：
        /// 一级：ModelCode + BaseQty；二级：MoldNumbers（取资源分配表里的 resourceCode）
        /// </summary>
        public async Task<WorkOrderMoldView> GetViewAsync(string workOrderNo, CancellationToken ct = default)
        {
            var raw = await GetRawAsync(workOrderNo, ct).ConfigureAwait(false);
            var result = raw?.result;

            var view = new WorkOrderMoldView
            {
                WorkOrderNo = result?.workOrderNo?.Trim() ?? workOrderNo?.Trim() ?? string.Empty,
                MaterialName = result?.materialName?.Trim() ?? string.Empty,
                Models = new List<MoldModelView>()
            };

            // ✅ 正确的数据来源：
            // 一级：result.planProcessRouteResourceDemandDTOS[i].model（型号）
            // 数量：result.planProcessRouteResourceDemandDTOS[i].demandQty（无则用分配数兜底）
            // 二级：result.planProcessRouteResourceDemandDTOS[i].planProcessRouteResourceAllocationDTOS[*].resourceCode（模具编码）
            var demands = result?.planProcessRouteResourceDemandDTOS ?? new List<PlanProcessRouteResourceDemandDTO>();

            foreach (var d in demands)
            {
                var model = (d.model ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(model)) continue;

                var allocs = d.planProcessRouteResourceAllocationDTOS ?? new List<PlanProcessRouteResourceAllocationDTO>();
                var codes = allocs
                    .Where(a => !string.IsNullOrWhiteSpace(a.code))
                    .Select(a => a.code!.Trim())
                    .Distinct()
                    .ToList();

                var baseQty = d.demandQty > 0 ? d.demandQty : codes.Count; // 无数量时用分配数兜底

                view.Models.Add(new MoldModelView
                {
                    ModelCode = model,
                    BaseQty = ToInt(baseQty),
                    MoldNumbers = codes
                });
            }

            // ★ 已扫描列表：来源 pdaBasMoldInfoDTOS
            var scannedDtos = result?.pdaBasMoldInfoDTOS ?? new List<PdaBasMoldInfoDTO>();
            foreach (var s in scannedDtos)
            {
                view.Scanned.Add(new ScannedItemView
                {
                    MoldCode = (s.moldCode ?? "").Trim(),
                    MoldModel = (s.moldModel ?? "").Trim(),
                    Location = (s.location ?? "").Trim(),
                    IzOutStock = s.izOutStock,
                    WarehouseCode = s.warehouseCode?.Trim(),
                    WarehouseName = s.warehouseName?.Trim(),
                    OutstockDate = s.outstockDate?.Trim()
                });
            }

            return view;
        }


        /// <summary>底层：按工单号取原始视图数据</summary>
        public async Task<QueryForWorkOrderResp?> GetRawAsync(string workOrderNo, CancellationToken ct = default)
        {
            var url = $"{_queryForWorkOrderEndpoint}?workOrderNo={Uri.EscapeDataString(workOrderNo ?? string.Empty)}";
            var requestUrl = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(requestUrl, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
            {
                var head = await PeekAsync(res.Content, 1024, ct).ConfigureAwait(false);
                throw new HttpRequestException($"GET {requestUrl} -> {(int)res.StatusCode}. Head: {head}");
            }

            var txt = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<QueryForWorkOrderResp>(txt,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static async Task<string> PeekAsync(HttpContent content, int maxLen, CancellationToken ct)
        {
            var txt = await ResponseGuard.ReadAsStringSafeAsync(content, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(txt)) return "";
            return txt.Length <= maxLen ? txt : txt.Substring(0, maxLen) + "...";
        }

        //------------------------------- 出库：扫描/确认 -------------------------------
        public async Task<MoldOutScanQueryResp?> OutStockScanQueryAsync(
    string code, string workOrderNo, CancellationToken ct = default)
        {
            // 构造查询串
            var qs = $"code={Uri.EscapeDataString(code ?? string.Empty)}&workOrderNo={Uri.EscapeDataString(workOrderNo ?? string.Empty)}";
            var url = _outStockScanQueryEndpoint + (_outStockScanQueryEndpoint.Contains("?") ? "&" : "?") + qs;
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            req.Headers.Accept.Clear();
            req.Headers.Accept.ParseAdd("application/json");

            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct).ConfigureAwait(false);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (!res.IsSuccessStatusCode || string.IsNullOrWhiteSpace(json))
            {
                return new MoldOutScanQueryResp
                {
                    success = false,
                    message = !res.IsSuccessStatusCode ? $"HTTP {(int)res.StatusCode}: {json}" : "空响应"
                };
            }

            try
            {
                return JsonSerializer.Deserialize<MoldOutScanQueryResp>(json, options);
            }
            catch (JsonException)
            {
                return new MoldOutScanQueryResp
                {
                    success = false,
                    message = "响应解析失败"
                };
            }
        }



        public async Task<SimpleOk> ConfirmOutStockAsync(MoldOutConfirmReq req, CancellationToken ct = default)
        {
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _outStockConfirmEndpoint);

            var json = JsonSerializer.Serialize(req, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            using var http = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var res = await _http.SendAsync(http, ct).ConfigureAwait(false);
            var txt = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
                return new SimpleOk(false, $"HTTP {(int)res.StatusCode}: {txt}");

            var dto = JsonSerializer.Deserialize<ConfirmResp>(txt,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var ok = dto?.success == true && (dto.result is bool b ? b : true);
            return new SimpleOk(ok, dto?.message ?? (ok ? "出库成功" : "出库失败"));
        }
    }


}
