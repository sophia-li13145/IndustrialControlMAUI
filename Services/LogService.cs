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

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (File.Exists(TodayLogPath))
                        {
                            // 用 FileStream + 共享读，避免写入方占用时报错
                            using var fs = new FileStream(
                                TodayLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            using var sr = new StreamReader(fs);
                            var text = await sr.ReadToEndAsync();

                            if (!string.Equals(text, last, StringComparison.Ordinal))
                            {
                                last = text;
                                // 事件尽量在主线程触发（视你的订阅者而定）
                                MainThread.BeginInvokeOnMainThread(() => LogTextUpdated?.Invoke(text));
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // token 取消时会跑到这里，直接退出大循环
                        break;
                    }
                    catch
                    {
                        // 其他 IO 等异常忽略一轮
                    }

                    try
                    {
                        await Task.Delay(_interval, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break; // 被 Stop() 取消，正常退出
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 兜底（通常到不了这里）
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
