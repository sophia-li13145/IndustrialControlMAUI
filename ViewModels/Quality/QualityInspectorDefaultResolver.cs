using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.ViewModels;

/// <summary>
/// 质检员默认值解析器。
/// </summary>
internal static class QualityInspectorDefaultResolver
{
    /// <summary>根据当前登录用户从用户列表中解析默认质检员显示名称。</summary>
    public static string? Resolve(IEnumerable<UserInfoDto> users)
    {
        var loginUserName = Preferences.Get("UserName", string.Empty)?.Trim();
        if (string.IsNullOrWhiteSpace(loginUserName))
        {
            return null;
        }

        var normalizedLoginUserName = NormalizeUserName(loginUserName);
        var matchedUser = users.FirstOrDefault(user => IsCurrentLoginUser(user, loginUserName, normalizedLoginUserName));

        return matchedUser is null
            ? normalizedLoginUserName
            : FirstNonEmpty(matchedUser.realname, matchedUser.username, normalizedLoginUserName);
    }

    private static bool IsCurrentLoginUser(UserInfoDto user, string loginUserName, string normalizedLoginUserName)
        => IsSameUserName(user.username, loginUserName)
           || IsSameUserName(user.username, normalizedLoginUserName)
           || IsSameUserName(user.email, loginUserName)
           || IsSameUserName(user.email, normalizedLoginUserName);

    private static bool IsSameUserName(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeUserName(string userName)
    {
        var atIndex = userName.IndexOf('@');
        return atIndex > 0 ? userName[..atIndex] : userName;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
