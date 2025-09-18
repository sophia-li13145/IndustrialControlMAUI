using System.Net.Http.Headers;

namespace IndustrialControlMAUI;

public static class ApiClient
{
    public static readonly HttpClient Instance = new HttpClient();

    public static void ConfigureBase(string ip, int port)
        => Instance.BaseAddress = new Uri($"http://{ip}:{port}");

    public static void SetBearer(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            Instance.DefaultRequestHeaders.Authorization = null;
        else
            Instance.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
