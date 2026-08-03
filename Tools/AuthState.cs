using CommunityToolkit.Mvvm.Messaging;
using System.Threading;
using IndustrialControlMAUI.Services.Permissions;

namespace IndustrialControlMAUI.Tools
{
    public sealed class AuthState
    {
        private readonly SemaphoreSlim _once = new(1, 1);
        private int _loggingOut = 0;
        private readonly PdaPermissionState _permissionState;

        public AuthState(PdaPermissionState permissionState) => _permissionState = permissionState;

        public async Task LogoutAsync(string reason = "登录状态已过期，请重新登录")
        {
            if (Interlocked.Exchange(ref _loggingOut, 1) == 1) return;
            await _once.WaitAsync();
            try
            {
                // 1) 清理登录态
                await TokenStorage.ClearAsync();
                _permissionState.Clear();

                // 2)（可选）全局通知：用于各页面收起弹窗/停止轮询
                WeakReferenceMessenger.Default.Send(new LoggedOutMessage(reason));

                // 3) 回到未登录壳
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    App.SwitchToLoggedOutShell();
                });
            }
            finally
            {
                Interlocked.Exchange(ref _loggingOut, 0);
                _once.Release();
            }
        }
    }


    public sealed record LoggedOutMessage(string Reason);
}
