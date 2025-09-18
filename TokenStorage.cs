// Utils/TokenStorage.cs
namespace IndustrialControlMAUI;

public static class TokenStorage
{
    private const string Key = "auth_token";

    public static async Task SaveAsync(string token)
    {
        await SecureStorage.SetAsync(Key, token);
        Preferences.Set(Key, token); // 兜底：极端设备 SecureStorage 取不到时从这里拿
        System.Diagnostics.Debug.WriteLine($"[TokenStorage] Saved token len={token?.Length}");
    }

    public static async Task<string?> LoadAsync()
    {
        try
        {
            var t = await SecureStorage.GetAsync(Key);
            if (!string.IsNullOrWhiteSpace(t))
            {
                System.Diagnostics.Debug.WriteLine($"[TokenStorage] Loaded from SecureStorage, len={t?.Length}");
                return t;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TokenStorage] SecureStorage error: {ex.Message}");
        }

        // 兜底
        var fallback = Preferences.Get(Key, null);
        System.Diagnostics.Debug.WriteLine($"[TokenStorage] Loaded from Preferences, len={fallback?.Length}");
        return fallback;
    }

    public static Task ClearAsync()
    {
        try { SecureStorage.Remove(Key); } catch { }
        Preferences.Remove(Key);
        System.Diagnostics.Debug.WriteLine("[TokenStorage] Cleared");
        return Task.CompletedTask;
    }
}
