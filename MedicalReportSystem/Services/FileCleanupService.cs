// FileCleanupService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MedicalReportSystem.Services
{
    public class FileCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FileCleanupService> _logger;
        private readonly IConfiguration _configuration;
        private TimeSpan _cleanupInterval;

        public FileCleanupService(
            IServiceProvider serviceProvider,
            ILogger<FileCleanupService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            // 从配置读取清理间隔，默认6小时
            var intervalHours = _configuration.GetValue<int>("FileStorage:CleanupIntervalHours", 6);
            _cleanupInterval = TimeSpan.FromHours(intervalHours);

            _logger.LogInformation($"文件清理服务已初始化，清理间隔: {_cleanupInterval.TotalHours}小时");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("文件清理服务已启动");

            // 等待应用完全启动
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("开始定期清理过期文件...");

                    using var scope = _serviceProvider.CreateScope();
                    var storageService = scope.ServiceProvider.GetRequiredService<FileDataStorageService>();

                    int deletedCount = await storageService.CleanupExpiredFilesAsync();

                    if (deletedCount > 0)
                    {
                        _logger.LogInformation($"清理了 {deletedCount} 个过期文件");
                    }
                    else
                    {
                        _logger.LogDebug("没有需要清理的过期文件");
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但不抛出，继续运行
                    _logger.LogError(ex, "文件清理服务执行出错，继续运行");
                }

                // 等待下次清理
                try
                {
                    // 使用配置的间隔时间
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("文件清理服务已停止");
        }

        private async Task PerformCleanupAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<FileDataStorageService>();

            try
            {
                _logger.LogInformation("开始清理过期文件...");
                int deletedCount = await storageService.CleanupExpiredFilesAsync();

                if (deletedCount > 0)
                {
                    _logger.LogInformation($"清理完成，删除了 {deletedCount} 个过期文件");
                }
                else
                {
                    _logger.LogDebug("没有需要清理的过期文件");
                }

                // 可选：记录清理日志
                await LogCleanupResultAsync(deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行文件清理时出错");
                throw;
            }
        }

        private async Task LogCleanupResultAsync(int deletedCount)
        {
            try
            {
                var logPath = Path.Combine(
                    Environment.CurrentDirectory,
                    "Logs",
                    "CleanupLogs");

                Directory.CreateDirectory(logPath);

                var logFile = Path.Combine(logPath, $"cleanup_{DateTime.Now:yyyyMMdd}.log");

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 清理了 {deletedCount} 个过期文件\n";

                await File.AppendAllTextAsync(logFile, logEntry, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录清理日志失败");
            }
        }

        /// <summary>
        /// 手动触发清理（可从其他服务调用）
        /// </summary>
        public async Task<int> TriggerCleanupAsync()
        {
            _logger.LogInformation("手动触发文件清理...");

            using var scope = _serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<FileDataStorageService>();

            try
            {
                return await storageService.CleanupExpiredFilesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动触发清理失败");
                throw;
            }
        }
    }
}