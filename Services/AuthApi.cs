using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Models;
using Org.Apache.Http.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Services
{
    public interface IAuthApi
    {
        Task<List<UserInfoDto>> GetAllUsersAsync(CancellationToken ct = default);
    }
    public sealed class AuthApi : IAuthApi
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
        private readonly string _alluserEndpoint;
        public AuthApi(HttpClient http,IConfigLoader configLoader ) {

            _http = http;
            if (_http.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                _http.Timeout = TimeSpan.FromSeconds(15);

            var baseUrl = configLoader.GetBaseUrl();
            if (_http.BaseAddress is null)
                _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

            var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";

            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _alluserEndpoint = ServiceUrlHelper.NormalizeRelative(
                configLoader.GetApiPath("auth.alluser", "/pda/auth/allUsers"), servicePath);
        }

        public async Task<List<UserInfoDto>> GetAllUsersAsync(CancellationToken ct = default)
        {
            var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _alluserEndpoint);
            using var req = new HttpRequestMessage(HttpMethod.Get, full);
            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            await using var s = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ApiResp<List<UserInfoDto>>>(s, _json, ct)
                       ?? new ApiResp<List<UserInfoDto>>();

            var list = data.result ?? new();

            // 去重（以 username 为主键；为空则用 id）
            var dedup = list
                .Where(u => !string.IsNullOrWhiteSpace(u.username) || !string.IsNullOrWhiteSpace(u.id))
                .GroupBy(u => string.IsNullOrWhiteSpace(u.username) ? u.id : u.username, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            return dedup;
        }
    }

}
