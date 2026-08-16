using MedicalReportSystem.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly ThirdPartyConfigService _configService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        ThirdPartyConfigService configService,
        IConfiguration configuration,
        ILogger<ConfigController> logger)
    {
        _configService = configService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("thirdparty")]
    public IActionResult GetThirdPartyConfig()
    {
        var config = _configService.GetSettings();
        return Ok(new
        {
            baseUrl = config.BaseUrl,
            getReportEndpoint = config.ApiEndpoints["GetReport"]
        });
    }

    [HttpPost("thirdparty")]
    public IActionResult UpdateThirdPartyConfig([FromBody] ThirdPartyConfigDto dto)
    {
        _configService.UpdateSettings(new ThirdPartySettings
        {
            BaseUrl = dto.BaseUrl,
            ApiEndpoints = new Dictionary<string, string>
            {
                ["GetReport"] = dto.GetReportEndpoint
            }
        });
        return Ok();
    }

    /// <summary>
    /// 获取验证配置
    /// </summary>
    [HttpGet("validation-config")]
    public IActionResult GetValidationConfig()
    {
        try
        {
            var config = new
            {
                EnableMappingValidation = _configuration.GetValue<int>("CodeValidation:EnableMappingValidation", 1),
                EnableDataWriteBack = _configuration.GetValue<int>("CodeValidation:EnableDataWriteBack", 0)
            };

            _logger.LogInformation($"获取验证配置: {config}");
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验证配置失败");
            return StatusCode(500, new { error = "获取验证配置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取提醒设置配置（从appsettings.json读取）
    /// </summary>
    [HttpGet("reminder-settings")]
    public IActionResult GetReminderSettings()
    {
        try
        {
            var config = new
            {
                // 从appsettings.json读取ReminderSettings:DefaultLookbackDays，默认值30
                DefaultLookbackDays = _configuration.GetValue<int>("ReminderSettings:DefaultLookbackDays", 30)
            };

            _logger.LogInformation($"获取提醒设置: DefaultLookbackDays={config.DefaultLookbackDays}");
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取提醒设置失败");
            return StatusCode(500, new { error = "获取提醒设置失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有应用配置（综合配置）
    /// </summary>
    [HttpGet("app-config")]
    public IActionResult GetAppConfig()
    {
        try
        {
            var config = new
            {
                // 提醒设置
                ReminderSettings = new
                {
                    DefaultLookbackDays = _configuration.GetValue<int>("ReminderSettings:DefaultLookbackDays", 30)
                },

                // 验证配置
                CodeValidation = new
                {
                    EnableMappingValidation = _configuration.GetValue<int>("CodeValidation:EnableMappingValidation", 1),
                    EnableDataWriteBack = _configuration.GetValue<int>("CodeValidation:EnableDataWriteBack", 0)
                },

                // 文件存储配置
                FileStorage = new
                {
                    BasePath = _configuration.GetValue<string>("FileStorage:BasePath", "App_Data/TempData"),
                    DefaultExpirationHours = _configuration.GetValue<int>("FileStorage:DefaultExpirationHours", 24),
                    CleanupIntervalHours = _configuration.GetValue<int>("FileStorage:CleanupIntervalHours", 12)
                },

                // API端点
                ApiEndpoints = new
                {
                    Reports = _configuration.GetValue<string>("ApiEndpoints:Reports", "https://localhost:81/api/Reports/open")
                },

                // 基础URL
                BaseUrl = _configuration.GetValue<string>("BaseUrl", "http://10.10.1.46:8092")
            };

            _logger.LogInformation($"获取应用配置成功");
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取应用配置失败");
            return StatusCode(500, new { error = "获取应用配置失败", message = ex.Message });
        }
    }
}

public class ThirdPartyConfigDto
{
    public string? BaseUrl { get; set; }
    public string? GetReportEndpoint { get; set; }
}