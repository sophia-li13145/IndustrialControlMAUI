namespace IndustrialControlMAUI.Services
{
    public class LogService : IDisposable
    {
        private readonly string _logsDir = Path.Combine(FileSystem.AppDataDirectory, "logs");
        private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(800);
        private CancellationTokenSource? _cts;

        public event Action<string>? LogTextUpdated;

        public string TodayLogPath => Path.Combine(_logsDir, $"gr-{DateTime.Now:yyyy-MM-dd}.txt");

        public LogService()
        {
            // 确保日志目录存在
            Directory.CreateDirectory(_logsDir);
        }

        public void Start()
        {
            Stop();
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => LoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        // 读取日志并更新
        private async Task LoopAsync(CancellationToken token)
        {
            string last = string.Empty;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (File.Exists(TodayLogPath))
                    {
                        var text = await File.ReadAllTextAsync(TodayLogPath, token);
                        if (!string.Equals(text, last, StringComparison.Ordinal))
                        {
                            last = text;
                            LogTextUpdated?.Invoke(text); // 更新UI显示
                        }
                    }
                }
                catch { }
                await Task.Delay(_interval, token);
            }
        }

        // 写日志
        public void WriteLog(string message)
        {
            try
            {
                var logMessage = $"{DateTime.Now:HH:mm:ss} - {message}";
                // 将日志内容追加到文件末尾
                File.AppendAllText(TodayLogPath, logMessage + Environment.NewLine);
                // 触发更新事件
                LogTextUpdated?.Invoke(logMessage);
            }
            catch (Exception ex)
            {
                // 如果写入失败，可以在这里处理错误
                LogTextUpdated?.Invoke($"错误: {ex.Message}");
            }
        }

        public void Dispose() => Stop();
    }
}
