using IndustrialControlMAUI.Models.Permissions;

namespace IndustrialControlMAUI.Services.Permissions;

public sealed class PdaPermissionState
{
    private readonly object _lock = new();
    private HashSet<string> _menuCodes = new(StringComparer.OrdinalIgnoreCase);
    public bool IsLoaded { get; private set; }

    public void Replace(IEnumerable<PdaMenuPermissionNode>? nodes)
    {
        var codes = Flatten(nodes).Select(x => x.MenuCode?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        lock (_lock) { _menuCodes = codes; IsLoaded = true; }
    }

    public bool Has(string menuCode)
    {
        if (string.IsNullOrWhiteSpace(menuCode)) return false;
        lock (_lock) return _menuCodes.Contains(menuCode);
    }

    public bool HasAny(params string[] menuCodes)
    {
        lock (_lock) return menuCodes.Any(HasUnsafe);
    }

    public void Clear() { lock (_lock) { _menuCodes.Clear(); IsLoaded = false; } }

    private bool HasUnsafe(string code) => !string.IsNullOrWhiteSpace(code) && _menuCodes.Contains(code);
    private static IEnumerable<PdaMenuPermissionNode> Flatten(IEnumerable<PdaMenuPermissionNode>? nodes)
    {
        if (nodes is null) yield break;
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children)) yield return child;
        }
    }
}
