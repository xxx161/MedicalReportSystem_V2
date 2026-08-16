using MedicalReportSystem.Models;
using MedicalReportSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;
using System.Web;

namespace MedicalReportSystem.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        //private readonly IReportService _reportService;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReportsController> _logger;
        private readonly FileDataStorageService _storageService;

        public ReportsController(IWebHostEnvironment env, HttpClient httpClient, ILogger<ReportsController> logger, FileDataStorageService storageService)
        {
            _env = env;
            _httpClient = httpClient;
            _logger = logger;
            _storageService = storageService;
        }
        /// <summary>
        /// 调阅程序入口 - 使用文件存储JSON数据
        /// </summary>
        [HttpPost("open")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> OpenReview(
            [FromForm] string GHId = "",
            [FromForm] string PatientID = "",
            [FromForm] string mode = "recognize",
            [FromForm] string searchType = "",
            [FromForm] string type = "",
            [FromForm] string doctorId = "",
            [FromForm] string feeSource = "",
            [FromForm] string idCard = "",
            [FromForm] string dataCode = "",
            [FromForm] string json = "")
        {
            try
            {
                // 参数验证
                if (string.IsNullOrWhiteSpace(GHId))
                    return BadRequest(new { Success = false, Message = "需要传入用户ID" });

                string fileId = null;

                // 存储JSON数据到文件
                if (!string.IsNullOrEmpty(json))
                {
                    // 保存到文件，设置过期时间24小时
                    fileId = await _storageService.SaveDataAsync(json, TimeSpan.FromHours(24));

                    _logger.LogInformation($"存储JSON到文件: fileId={fileId}, GHId={GHId}, 长度={json.Length}");
                }
                else
                {
                    _logger.LogWarning("JSON数据为空，不存储到文件");
                }

                // 构建URL
                string targetUrl = $"{Request.Scheme}://{Request.Host}/index.html?" +
                                  $"GHId={HttpUtility.UrlEncode(GHId)}&" +
                                  $"PatientID={HttpUtility.UrlEncode(PatientID)}&" +
                                  $"mode={HttpUtility.UrlEncode(mode)}&" +
                                  $"searchType={HttpUtility.UrlEncode(searchType)}&" +
                                  $"type={HttpUtility.UrlEncode(type)}&" +
                                  $"doctorId={HttpUtility.UrlEncode(doctorId)}&" +
                                  $"feeSource={HttpUtility.UrlEncode(feeSource)}&" +
                                  $"idCard={HttpUtility.UrlEncode(idCard)}&" +
                                  $"dataCode={HttpUtility.UrlEncode(dataCode)}&";

                // 如果有文件ID，添加到URL
                if (!string.IsNullOrEmpty(fileId))
                {
                    targetUrl += $"&fileId={HttpUtility.UrlEncode(fileId)}";
                }

                return Ok(new
                {
                    Success = true,
                    Url = targetUrl,
                    FileId = fileId,
                    Message = "调阅地址生成成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成调阅地址时出错");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "内部服务器错误",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 从文件获取JSON数据
        /// </summary>
        [HttpGet("get-data")]
        public async Task<IActionResult> GetDataFromFile([FromQuery] string fileId)
        {
            try
            {
                if (string.IsNullOrEmpty(fileId))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "需要fileId参数"
                    });
                }

                // 从文件读取数据
                string jsonData = await _storageService.GetDataAsync(fileId);

                if (string.IsNullOrEmpty(jsonData))
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "数据不存在或已过期",
                        FileId = fileId
                    });
                }
                 

                return Ok(new
                {
                    Success = true,
                    FileId = fileId,
                    JsonData = jsonData,
                    Message = "获取数据成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取文件数据时出错: fileId={fileId}");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "内部服务器错误",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 手动清理文件数据
        /// </summary>
        [HttpPost("cleanup-file")]
        public async Task<IActionResult> CleanupFile([FromQuery] string fileId)
        {
            try
            {
                if (!string.IsNullOrEmpty(fileId))
                {
                    bool deleted = await _storageService.DeleteDataAsync(fileId);

                    return Ok(new
                    {
                        Success = deleted,
                        Message = deleted ? "文件已删除" : "文件不存在",
                        FileId = fileId
                    });
                }

                return BadRequest(new { Success = false, Message = "需要fileId参数" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理文件数据时出错");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "清理失败",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 获取用户专属的检验明细
        /// </summary>
        [HttpGet("detail/{userId}/{reportId}")]
        public async Task<IActionResult> GetUserReportDetail(
            string userId,
            string reportId,
            [FromServices] ThirdPartyConfigService configService)
        {
            try
            {
                // 创建日志目录
                var userDirLog = Path.Combine(_env.ContentRootPath, "Data", $"user_Log");
                Directory.CreateDirectory(userDirLog);
                // 日志文件名称
                var listPath = Path.Combine(userDirLog, "listLog.txt");
                // 验证用户目录存在
                var userDir = Path.Combine(_env.ContentRootPath, "Data", $"user_{userId}");
                if (!Directory.Exists(userDir))
                    return NotFound("用户目录不存在");

                // 检查本地缓存
                var detailPath = Path.Combine(userDir, "Detail", $"{reportId}.json");
                if (System.IO.File.Exists(detailPath))
                   return PhysicalFile(detailPath, "application/json");

                // 调用第三方API获取数据
                var config=configService.GetSettings();
                var endpoint = config.ApiEndpoints["GetReport"].Replace("{reportId}", reportId);
                //var thirdPartyUrl = $"https://localhost:7224/api/Data/{reportId}";
                var thirdPartyUrl =$"{config.BaseUrl}/{endpoint}";
                //var response = await _httpClient.GetAsync(thirdPartyUrl);//get请求
                System.IO.File.AppendAllText(listPath, "三方地址:"+thirdPartyUrl + Environment.NewLine); // 追加内容并换行
                // 准备POST请求体（根据第三方API要求调整）
                var requestData = new
                {
                    //UserId = userId,
                    ReportId = reportId
                };

                // 序列化请求体
                //var jsonContent = new StringContent(
                //    JsonSerializer.Serialize(requestData),
                //    Encoding.UTF8,
                //    "application/json");

                //直接发送原始字符串
                var stringContent = new StringContent(
                reportId,
                Encoding.UTF8,
                "text/plain"); // 注意Content-Type改为text/plain

                // 示例：带认证头的POST请求
                //var request = new HttpRequestMessage(HttpMethod.Post, thirdPartyUrl);
                //request.Content = jsonContent;
                //request.Headers.Add("Authorization", $"Bearer {config.AccessToken}");

                //var response = await _httpClient.SendAsync(request);
                // 发送POST请求
                var response = await _httpClient.PostAsync(thirdPartyUrl, stringContent);
                System.IO.File.AppendAllText(listPath, "请求信息:"+response+ ",stringContent:"+ stringContent + Environment.NewLine); // 追加内容并换行
                if (!response.IsSuccessStatusCode)
                {
                    System.IO.File.AppendAllText(listPath, "第三方接口调用失败:"+response + Environment.NewLine); // 追加内容并换行
                    _logger.LogError($"第三方接口调用失败，状态码：{response.StatusCode}");
                    return StatusCode(502, "Failed to fetch from third party");
                }

                //if (!response.IsSuccessStatusCode)
                //    return StatusCode(502, "Failed to fetch from third party");

                var content = await response.Content.ReadAsStringAsync();

                // 保存到用户缓存
                await System.IO.File.WriteAllTextAsync(detailPath, content);

                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting detail for user {userId}");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("list/{userId}")]
        public IActionResult GetReportList(string userId)
        {
            try
            {
                // 验证用户目录存在
                var userDir = Path.Combine(_env.ContentRootPath, "Data", $"user_{userId}");
                if (!Directory.Exists(userDir))
                    return NotFound("User data not found");

                // 检查本地缓存
                var detailPath = Path.Combine(userDir, "list.json");
                //if (System.IO.File.Exists(detailPath))
                    //return PhysicalFile(detailPath, "application/json");

                return PhysicalFile(detailPath, "application/json");
            }
            catch (Exception)
            {

                return StatusCode(500, "获取检验列表失败");
            }
            //var filePath = Path.Combine(_env.ContentRootPath, "Data", "SimulatedData.json");

            //if (!System.IO.File.Exists(filePath))
            //{
            //    return NotFound("检验列表不存在");
            //}

            //return PhysicalFile(filePath, "application/json");
        }
        [HttpGet("detail/{reportId}")]
        public async Task<IActionResult> GetReportDetail(string reportId)
        {
            // 检查本地缓存
            var detailPath = Path.Combine(_env.ContentRootPath, "Data", "Detail", $"{reportId}.json");
            if (System.IO.File.Exists(detailPath))
            {
                return PhysicalFile(detailPath, "application/json");
            }

            // 调用三方接口获取
            try
            {
                var thirdPartyUrl = $"http://third-party-api.com/reports/{reportId}";
                var response = await _httpClient.GetAsync(thirdPartyUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // 缓存到本地
                    var detailDir = Path.Combine(_env.ContentRootPath, "Data", "Detail");
                    if (!Directory.Exists(detailDir))
                        Directory.CreateDirectory(detailDir);

                    await System.IO.File.WriteAllTextAsync(detailPath, content);
                    return Content(content, "application/json");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"调用三方接口失败: {ex.Message}");
            }

            return NotFound();
        }
    }
}