using MedicalReportSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalReportSystem.Controllers
{
    // FileStorageController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class FileStorageController : ControllerBase
    {
        private readonly FileDataStorageService _storageService;
        private readonly FileCleanupService _cleanupService;
        private readonly ILogger<FileStorageController> _logger;

        public FileStorageController(
            FileDataStorageService storageService,
            IHostedService cleanupService,
            ILogger<FileStorageController> logger)
        {
            _storageService = storageService;
            _cleanupService = cleanupService as FileCleanupService;
            _logger = logger;
        }

        /// <summary>
        /// 获取存储统计信息
        /// </summary>
        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            try
            {
                var stats = _storageService.GetStorageStatistics();

                return Ok(new
                {
                    Success = true,
                    Statistics = stats,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取存储统计信息失败");
                return StatusCode(500, new { Success = false, Message = "获取统计信息失败" });
            }
        }

        /// <summary>
        /// 手动触发清理
        /// </summary>
        [HttpPost("trigger-cleanup")]
        public async Task<IActionResult> TriggerCleanup()
        {
            try
            {
                if (_cleanupService == null)
                {
                    return BadRequest(new { Success = false, Message = "清理服务不可用" });
                }

                int deletedCount = await _cleanupService.TriggerCleanupAsync();

                return Ok(new
                {
                    Success = true,
                    DeletedCount = deletedCount,
                    Message = $"清理了 {deletedCount} 个过期文件",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动触发清理失败");
                return StatusCode(500, new { Success = false, Message = "触发清理失败" });
            }
        }
    }
}
