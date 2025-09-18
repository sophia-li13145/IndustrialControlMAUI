using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Services;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly LogService _logSvc;

        [ObservableProperty] private string logText = "日志初始化中...";

        public string TodayPath => _logSvc.TodayLogPath;

        public LogsViewModel(LogService logSvc)
        {
            _logSvc = logSvc;
            _logSvc.LogTextUpdated += text => LogText = text;  // 订阅日志更新事件
        }

        // 页面显示时启动日志服务
        public void OnAppearing() => _logSvc.Start();

        // 页面隐藏时停止日志服务
        public void OnDisappearing() => _logSvc.Stop();

        // 外部调用更新日志的方法
        public void AddLog(string message)
        {
            _logSvc.WriteLog(message);  // 调用 LogService 的写日志方法
        }

        // 外部调用更新错误日志的方法
        public void AddErrorLog(Exception ex)
        {
            _logSvc.WriteLog($"错误日志: {ex.Message}");  // 调用 LogService 的写错误日志方法
        }
    }
}
