using IndustrialControlMAUI.Services.Common;
using AndroidX.Annotations;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Text.Json;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IEnergyApi
    {
        /// <summary>查询仪表分页（图1）</summary>
        Task<PageResponeResult<MeterRecordDto>> MeterPageQueryAsync(
            int pageNo,
            int pageSize,
            string? meterCode,
            string? energyType,   // electric/water/gas/compressed_air，null 表示全部
            string? workshopId,
            string? lineId,
            bool searchCount,
            CancellationToken ct = default);

        /// <summary>能源类型字典（electric/water/gas/compressed_air → 显示名）</summary>
        Task<List<EnergyDictItem>> GetEnergyTypeDictAsync(CancellationToken ct = default);

        /// <summary>车间列表（默认 workshopsType=workshop）</summary>
        Task<List<IdNameOption>> GetWorkshopsAsync(string? workshopsType = "workshop", CancellationToken ct = default);

        /// <summary>用户列表（抄表人候选）</summary>
        Task<List<IdNameOption>> GetUsersAsync(CancellationToken ct = default);

        Task<List<IdNameOption>> GetProductLinesAsync(string? workshopsType = "production_line", CancellationToken ct = default);

        Task<List<MeterPointItem>> GetMeterPointsByMeterCodeAsync(string meterCode, CancellationToken ct = default);

        Task<LastReadingResult?> GetLastReadingAsync(string meterCode, string meterPointCode, CancellationToken ct = default);

        Task<bool> SaveMeterReadingAsync(MeterSaveReq req, CancellationToken ct = default);

        Task<List<DevItem>> GetDevListAsync(CancellationToken ct = default);
    }

    // ===================== 实现 =====================
    public class EnergyApi : IEnergyApi
    {
        private readonly HttpClient _http;
        private readonly AuthState _auth;
        private readonly string _meterPageEndpoint;   // /pda/emMeter/queryMeterPageList
        private readonly string _dictEndpoint;        // /pda/emMeter/queryDictList
        private readonly string _workshopEndpoint;    // /pda/common/queryWorkShopList
        private readonly string _userListEndpoint;    // /pda/common/queryUserList
        private readonly string _productLineEndpoint;
        private readonly string _pointsByMeterEndpoint;
        private readonly string _lastReadingMeterEndpoint;
        private readonly string _saveReadingEndpoint;
        private readonly string _devListEndpoint;

        public EnergyApi(HttpClient http, IConfigLoader configLoader, AuthState auth)
        {
            _http = http;
            _auth = auth;

            if (_http.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                _http.Timeout = TimeSpan.FromSeconds(15);

            var baseUrl = configLoader.GetBaseUrl();
            if (_http.BaseAddress is null)
                _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

            var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";

            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 端点路径允许走你的 appconfig.json 动态配置；未配置时使用默认值
            _meterPageEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("energy.meterPage", "/pda/emMeter/queryMeterPageList"), servicePath);
            _dictEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("energy.dictList", "/pda/emMeter/queryDictList"), servicePath);
            _workshopEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("energy.workshops", "/pda/common/queryWorkShopList"), servicePath);
            _userListEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("energy.users", "/pda/common/queryUserList"), servicePath);
            _productLineEndpoint = ServiceUrlHelper.NormalizeRelative(
           configLoader.GetApiPath("energy.productLines", "/pda/common/queryProductLineList"),
           servicePath);
            _pointsByMeterEndpoint = ServiceUrlHelper.NormalizeRelative(
           configLoader.GetApiPath("energy.pointsByMeter", "/pda/emMeter/queryPointListByMeterCode"),
           servicePath);
            _lastReadingMeterEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("energy.lastReadingMeter", "/pda/emMeter/queryLastReadingMeter"),
            servicePath);
            _saveReadingEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("energy.saveReading", "/pda/emMeter/save"),
            servicePath);
            _devListEndpoint = ServiceUrlHelper.NormalizeRelative(
    configLoader.GetApiPath("energy.devList", "/pda/common/queryDevList"),
    servicePath);

           
        }

        private static string BuildQuery(IDictionary<string, string> p)
            => string.Join("&", p.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        // ===== 方法实现 =====

        public async Task<PageResponeResult<MeterRecordDto>> MeterPageQueryAsync(
            int pageNo,
            int pageSize,
            string? meterCode,
            string? energyType,
            string? workshopId,
            string? lineId,
            bool searchCount,
            CancellationToken ct = default)
        {
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["searchCount"] = searchCount ? "true" : "false"
            };
            if (!string.IsNullOrWhiteSpace(meterCode)) p["meterCode"] = meterCode!.Trim();
            if (!string.IsNullOrWhiteSpace(energyType)) p["energyType"] = energyType!.Trim();
            if (!string.IsNullOrWhiteSpace(workshopId)) p["workShopId"] = workshopId!.Trim();
            if (!string.IsNullOrWhiteSpace(lineId)) p["lineId"] = lineId!.Trim();

            var url = _meterPageEndpoint + "?" + BuildQuery(p);
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
            {
                return new PageResponeResult<MeterRecordDto>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<PageResponeResult<MeterRecordDto>>(body, _json)
                   ?? new PageResponeResult<MeterRecordDto> { success = false, message = "Empty body" };
        }

        public async Task<List<EnergyDictItem>> GetEnergyTypeDictAsync(CancellationToken ct = default)
        {
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _dictEndpoint);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode) return new();

            var dto = JsonSerializer.Deserialize<EnergyDictResponse>(json, _json);
            var field = dto?.result?.FirstOrDefault(f =>
                string.Equals(f.field, "energyType", StringComparison.OrdinalIgnoreCase));

            return field?.dictItems?.Select(i => new EnergyDictItem
            {
                dictItemValue = i.dictItemValue,
                dictItemName = i.dictItemName
            }).ToList() ?? new List<EnergyDictItem>();
        }

        public async Task<List<IdNameOption>> GetWorkshopsAsync(string? workshopsType = "workshop", CancellationToken ct = default)
        {
            var url = _workshopEndpoint + (string.IsNullOrWhiteSpace(workshopsType) ? "" : $"?workshopsType={Uri.EscapeDataString(workshopsType)}");
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode) return new();

            var dto = JsonSerializer.Deserialize<WorkShopResponse>(json, _json);
            var list = dto?.result?.Where(x => !string.IsNullOrWhiteSpace(x.workShopId))
                .Select(x => new IdNameOption { Id = x.workShopId!, Name = x.workShopName ?? "" })
                .ToList() ?? new List<IdNameOption>();

            // 习惯性在列表头加“全部”
            list.Insert(0, new IdNameOption { Id = null, Name = "全部" });
            return list;
        }

        public async Task<List<IdNameOption>> GetUsersAsync(CancellationToken ct = default)
        {
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _userListEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode) return new();

            var dto = JsonSerializer.Deserialize<UserListResponse>(json, _json);
            return dto?.result?.Select(u => new IdNameOption
            {
                Id = u.id,
                Name = string.IsNullOrWhiteSpace(u.realname) ? (u.username ?? "") : u.realname!,
                UserName = u.username!,
            }).ToList() ?? new List<IdNameOption>();
        }

        public async Task<List<IdNameOption>> GetProductLinesAsync(string? workshopsType = "production_line", CancellationToken ct = default)
        {
            // /normalService/pda/common/queryProductLineList?workshopsType=production_line
            var url = _productLineEndpoint + (string.IsNullOrWhiteSpace(workshopsType) ? "" : $"?workshopsType={Uri.EscapeDataString(workshopsType)}");
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
            if (!res.IsSuccessStatusCode) return new();

            var dto = JsonSerializer.Deserialize<ProductLineResponse>(json, _json);
            var list = dto?.result?
                .Where(x => !string.IsNullOrWhiteSpace(x.productLineId))
                .Select(x => new IdNameOption { Id = x.productLineId!, Name = x.productLineName ?? "" })
                .ToList() ?? new List<IdNameOption>();

            // 头部加“全部”
            list.Insert(0, new IdNameOption { Id = null, Name = "全部" });
            return list;
        }

        public async Task<List<MeterPointItem>> GetMeterPointsByMeterCodeAsync(string meterCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(meterCode)) return new();
            var url = _pointsByMeterEndpoint + $"?meterCode={Uri.EscapeDataString(meterCode)}";
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
            if (!res.IsSuccessStatusCode) return new();

            var dto = JsonSerializer.Deserialize<MeterPointResp>(json, _json);
            return dto?.result ?? new();
        }

        public async Task<LastReadingResult?> GetLastReadingAsync(string meterCode, string meterPointCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(meterCode) || string.IsNullOrWhiteSpace(meterPointCode)) return null;

            var url = _lastReadingMeterEndpoint + "?" + BuildQuery(new Dictionary<string, string>
            {
                ["meterCode"] = meterCode,
                ["meterPointCode"] = meterPointCode
            });
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
            var dto = JsonSerializer.Deserialize<ApiResp<LastReadingResult>>(json, _json);
            return dto?.success == true ? dto.result : null;
        }

        public async Task<bool> SaveMeterReadingAsync(MeterSaveReq req, CancellationToken ct = default)
        {
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _saveReadingEndpoint);
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
            {
                Content = new StringContent(JsonSerializer.Serialize(req, _json), System.Text.Encoding.UTF8, "application/json")
            };
            using var res = await _http.SendAsync(httpReq, ct);
            if (!res.IsSuccessStatusCode) return false;

            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
            var r = JsonSerializer.Deserialize<ApiResp<bool>>(json, _json);
            return r?.success == true && r.result;
        }

        public async Task<List<DevItem>> GetDevListAsync(CancellationToken ct = default)
        {
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _devListEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);

            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
                return new();

            var dto = JsonSerializer.Deserialize<ApiResp<List<DevItem>>>(json, _json);
            return dto?.result ?? new List<DevItem>();
        }

    }
}
