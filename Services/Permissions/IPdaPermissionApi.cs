using IndustrialControlMAUI.Models.Permissions;

namespace IndustrialControlMAUI.Services.Permissions;

public interface IPdaPermissionApi
{
    Task<IReadOnlyList<PdaMenuPermissionNode>> GetCurrentUserPermissionsAsync(
        string systemType, CancellationToken cancellationToken = default);
}
