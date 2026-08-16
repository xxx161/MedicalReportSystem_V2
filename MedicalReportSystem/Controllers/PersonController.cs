using com.gbase8c.fastpath;
using com.sun.xml.@internal.bind.v2.runtime.unmarshaller;
using com.sun.xml.@internal.ws.client;
using java.util.regex;
using MedicalReportSystem.Models;
using MedicalReportSystem.Models.Config;
using MedicalReportSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using static com.sun.tools.@internal.xjc.reader.xmlschema.bindinfo.BIConversion;
using static com.sun.tools.javadoc.JavaScriptScanner;
using static com.sun.tools.javah.Util;
using static MedicalReportSystem.Models.JsonConverters;
using static MedicalReportSystem.Services.NewStandardService;

namespace MedicalReportSystem.Controllers
{
    /// <summary>
    /// Oracle业务交互层
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly PersonService _personService;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        private readonly string _recognitionApiUrl;
        private readonly string _reminderApiUrl;
        private readonly string _uploadRetrievalRecord;
        private readonly string _revokeNonRecognition;
        private readonly string _uploadQuoteLogUrl;
        private readonly ReminderSettings _reminderSettings;


        private readonly INewStandardService _newStandardService;

        public PersonController(PersonService personService, IConfiguration configuration, AppDbContext context, IWebHostEnvironment env, INewStandardService newStandardService)
        {
            _personService = personService;
            _configuration = configuration;
            _context = context;
            _env = env;

            // 从配置中读取第三方接口地址
            _recognitionApiUrl = _configuration["ThirdPartyApi:Recognition"];//互认记录上传(上传不护认记录)
            _reminderApiUrl = _configuration["ThirdPartyApi:Reminder"];//提醒记录上传
            _uploadRetrievalRecord = _configuration["ThirdPartyApi:uploadRetrievalRecord"];//调阅记录上传
            _revokeNonRecognition = _configuration["ThirdPartyApi:revokeNonRecognition"];//撤销不互认操作
            _uploadQuoteLogUrl = _configuration["ThirdPartyApi:uploadQuoteLogUrl"];//引用记录上传
            _reminderSettings = new ReminderSettings();
            _configuration.GetSection("ReminderSettings").Bind(_reminderSettings);
            _newStandardService = newStandardService;
        }
        /// <summary>
        /// 检查报告是否存在(Oracle版本)​通用查询
        /// </summary>
        [HttpGet("exists-oracle")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckReportExistsOracle(
            [FromQuery] string userID,
            [FromQuery] string? type = "lab",
            [FromQuery] string? dataCode = null)
        {
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "检查报告是否存在(Oracle版本)");
                var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                bool exists = await _personService.CheckReportExistsAsync(userID, type, dataCode);

                await LogToFileAsync(logPath, $" 返回请求 | userID: {userID}, exists: {exists}");

                // 返回标准的 JSON 响应格式
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = exists,
                    Message = exists ? "报告存在" : "报告不存在"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"服务器内部错误: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// 查询GLHR检查检验映射表(Oracle版本)检查是否存在​
        /// </summary>
        [HttpGet("query-glhr-mapping")]
        public async Task<ActionResult<ApiResponse<object>>> QueryGLHRMapping(
            [FromQuery] string type,
            [FromQuery] string mutualCode,
            [FromQuery] string? treatmentCode=null,
            [FromQuery] string? part = null,
            [FromQuery] string? method = null)
        {
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询GLHR检查检验映射表");
                var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"开始查询GLHR映射 |type:{type}, mutualCode: {mutualCode}, treatmentCode: {treatmentCode}, part: {part}, method: {method}");

                // 调用服务层方法查询映射关系
                var result = await _personService.QueryGLHRMappingAsync(type,mutualCode, treatmentCode, part, method);
                
                await LogToFileAsync(logPath, $"查询结果 | mutualTreatmentId: {result.mutualTreatmentId} ,mutualTreatmentName: {result.mutualTreatmentName}, part: {result.part}, method: {result.method}");

                // 构建返回数据对象
                var responseData = new
                {
                    MutualTreatmentId = result.mutualTreatmentId,
                    MutualTreatmentName = result.mutualTreatmentName,
                    Part = result.part,
                    Method = result.method,
                    Exists = !string.IsNullOrEmpty(result.mutualTreatmentId),
                    ExecDeptId = result.execDeptId,
                    SpecimenSite = result.specimenSite,
                    CollectionName=result.collectionName
                };

                // 返回标准的 JSON 响应格式
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = responseData,
                    Message = !string.IsNullOrEmpty(result.mutualTreatmentId) ? "查询成功，存在映射记录" : "查询成功，未找到映射记录"
                });
            }
            catch (Exception ex)
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询GLHR检查检验映射表");
                var logPath = Path.Combine(logDir, $"error{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"查询GLHR映射失败 | mutualCode: {mutualCode}, treatmentCode: {treatmentCode}, error: {ex.Message}");

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Message = $"查询GLHR映射失败: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// 查询病人基本信息
        /// </summary>
        [HttpGet("query-patient-info")]
        public async Task<ActionResult<ApiResponse<object>>> QueryPatientInfo([FromQuery] string GHID)
        {
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询病人基本信息");
                var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"开始查询病人基本信息 | GHID: {GHID}");

                // 调用服务层方法查询病人信息
                var result = await _personService.QueryPatientInfoAsync(GHID);

                await LogToFileAsync(logPath, $"查询结果 | DoctorName: {result.DoctorName}, RegistrationNo: {result.RegistrationNo}, PatientId: {result.PatientId}, OutpatientNo: {result.OutpatientNo}");

                // 构建返回数据对象 - 使用英文属性名
                var responseData = new
                {
                    DoctorName = result.DoctorName,
                    RegistrationNo = result.RegistrationNo,
                    PatientId = result.PatientId,
                    OutpatientNo = result.OutpatientNo,
                    Exists = !string.IsNullOrEmpty(result.PatientId)
                };

                // 返回标准的 JSON 响应格式
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = responseData,
                    Message = !string.IsNullOrEmpty(result.PatientId) ? "查询成功，找到病人信息" : "查询成功，未找到病人信息"
                });
            }
            catch (Exception ex)
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询病人基本信息");
                var logPath = Path.Combine(logDir, $"error{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"查询病人基本信息失败 | GHID: {GHID}, error: {ex.Message}");

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Message = $"查询病人基本信息失败: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// 检查GLHR映射是否存在(Oracle版本)
        /// </summary>
        [HttpGet("check-glhr-mapping-exists")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckGLHRMappingExists(
            [FromQuery] string mutualCode,
            [FromQuery] string treatmentCode,
            [FromQuery] string? part = null,
            [FromQuery] string? method = null)
        {
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "检查GLHR映射是否存在");
                var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"开始检查GLHR映射是否存在 | mutualCode: {mutualCode}, treatmentCode: {treatmentCode}, part: {part}, method: {method}");

                // 调用服务层方法检查映射是否存在
                bool exists = await _personService.CheckGLHRMappingExistsAsync(mutualCode, treatmentCode, part, method);

                await LogToFileAsync(logPath, $"检查结果 | exists: {exists}");

                // 返回标准的 JSON 响应格式
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = exists,
                    Message = exists ? "GLHR映射存在" : "GLHR映射不存在"
                });
            }
            catch (Exception ex)
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "检查GLHR映射是否存在");
                var logPath = Path.Combine(logDir, $"error{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"检查GLHR映射失败 | mutualCode: {mutualCode}, treatmentCode: {treatmentCode}, error: {ex.Message}");

                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"检查GLHR映射失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 查询GLHR映射 - 检验项目专用
        /// </summary>
        [HttpGet("query-glhr-mapping-lab")]
        public async Task<ActionResult<ApiResponse<string>>> QueryGLHRMappingForLab(
            [FromQuery] string type,
            [FromQuery] string mutualCode,
            [FromQuery] string treatmentCode)
        {
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询GLHR映射检验项目");
                var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"开始查询GLHR映射(检验项目) |type:{type}, mutualCode: {mutualCode}, treatmentCode: {treatmentCode}");

                // 调用服务层方法查询检验项目映射
                string mutualTreatmentId = await _personService.QueryGLHRMappingForLabAsync(type,mutualCode, treatmentCode);

                await LogToFileAsync(logPath, $"查询结果 | mutualTreatmentId: {mutualTreatmentId}");

                // 返回标准的 JSON 响应格式
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = mutualTreatmentId,
                    Message = !string.IsNullOrEmpty(mutualTreatmentId) ? "查询成功" : "未找到映射记录"
                });
            }
            catch (Exception ex)
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询GLHR映射检验项目");
                var logPath = Path.Combine(logDir, $"error{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"查询GLHR映射(检验项目)失败 | mutualCode: {mutualCode}, treatmentCode: {treatmentCode}, error: {ex.Message}");

                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Data = null,
                    Message = $"查询GLHR映射失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 查询GLHR映射 - 检查项目专用
        /// </summary>
        [HttpGet("query-glhr-mapping-exam")]
        public async Task<ActionResult<ApiResponse<object>>> QueryGLHRMappingForExam(
            [FromQuery] string mutualCode,
            [FromQuery] string treatmentCode,
            [FromQuery] string part,
            [FromQuery] string method)
        {
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询GLHR映射检查项目");
                var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"开始查询GLHR映射(检查项目) | mutualCode: {mutualCode}, treatmentCode: {treatmentCode}, part: {part}, method: {method}");

                // 调用服务层方法查询检查项目映射
                var result = await _personService.QueryGLHRMappingForExamAsync(mutualCode, treatmentCode, part, method);

                await LogToFileAsync(logPath, $"查询结果 | mutualTreatmentId: {result.mutualTreatmentId}, part: {result.part}, method: {result.method}");

                // 构建返回数据对象
                var responseData = new
                {
                    MutualTreatmentId = result.mutualTreatmentId,
                    Part = result.part,
                    Method = result.method,
                    Exists = !string.IsNullOrEmpty(result.mutualTreatmentId)
                };

                // 返回标准的 JSON 响应格式
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = responseData,
                    Message = !string.IsNullOrEmpty(result.mutualTreatmentId) ? "查询成功" : "未找到映射记录"
                });
            }
            catch (Exception ex)
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询GLHR映射检查项目");
                var logPath = Path.Combine(logDir, $"error{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"查询GLHR映射(检查项目)失败 | mutualCode: {mutualCode}, treatmentCode: {treatmentCode}, part: {part}, method: {method}, error: {ex.Message}");

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Message = $"查询GLHR映射失败: {ex.Message}"
                });
            }
        }


        /// <summary>
        /// 获取报告列表或表头(Oracle版本)
        /// </summary>
        [HttpGet("report-headers-oracle")]
        public async Task<ActionResult<List<Test_Detail_oracle>>> GetReportHeadersOracle(
            [FromQuery] string? reportId = null,
            [FromQuery] string? userID = null,
            [FromQuery] string? dataCode = null,
            [FromQuery] string type = "lab")
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询报告明细(Oracle版本)");
            var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
            Directory.CreateDirectory(logDir);
            await LogToFileAsync(logPath, $" 开始处理请求 | userID: {userID},参数:reportId{reportId}");
            try
            {
                var results = await _personService.GetReportHeadersFixedAsync(reportId, userID, dataCode, type);
                return results !=null ? Ok(results) : NotFound("未找到匹配记录");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"服务器内部错误: {ex.Message}");
            }
        }
        /// <summary>
        /// 获取报告列表或表头(Oracle版本)获取提醒数据
        /// </summary>
        [HttpGet("report-headers-oracleReminder")]
        public async Task<ActionResult<List<Test_Detail_oracle>>> GetReportHeadersOracleReminder(
            [FromQuery] string? reportId = null,
            [FromQuery] string? userID = null,
            [FromQuery] string type = "lab",
            [FromQuery] int? days = null)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询报告明细(Oracle版本)");
            var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
            Directory.CreateDirectory(logDir);
            await LogToFileAsync(logPath, $" 开始处理请求 | userID: {userID},参数:reportId:{reportId},参数:days:{days}");
            try
            {
                var results = await _personService.GetReportHeadersFixedAsyncReminder(reportId, userID, type, days);
                return results != null ? Ok(results) : NotFound("未找到匹配记录");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"服务器内部错误: {ex.Message}");
            }
        }
        /// <summary>
        /// 查询报告明细(Oracle版本)
        /// </summary>
        [HttpGet("report-details-oracle/{businessNo}/{reportId}/{currentMode}")]
        public async Task<ActionResult<List<Test_Detail_oracle>>> GetReportDetailsOracle(
            [FromRoute] string? businessNo,
            [FromRoute] string? reportId,
            [FromRoute] string? currentMode)
        {
            // 🔥 添加缓存控制头
            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            Response.Headers.Vary = "User-Agent"; // 针对不同浏览器
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询报告明细(Oracle版本)");
            var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $" 开始处理请求 | businessNo: {businessNo},参数:reportId{reportId},参数:currentMode{currentMode}");

                var result = await _personService.GetReportDetailsFixedAsync(businessNo, reportId, currentMode);
                return result != null ? Ok(result) : NotFound("未找到匹配记录");
            }
            catch (Exception ex)
            { 
                return StatusCode(500, new
                {
                    success = false,
                    error = "服务器内部错误",
                    message = ex.Message,
                    reportId = reportId,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }
        [HttpGet("report-details-oracle4/{businessNo}/{reportId}/{currentMode}")]
        public async Task<ActionResult<List<Test_Detail_oracle>>> GetReportDetailsOracle4(
    [FromRoute] string? businessNo,
    [FromRoute] string? reportId,
    [FromRoute] string? currentMode)
        {
            // 特殊处理这个有问题的reportId
            if (reportId == "18769022edc446cf9f99ac2c9faa4aac")
            {
                var debugLog = Path.Combine(Path.GetTempPath(), "special_reportid_debug.log");
                await System.IO.File.AppendAllTextAsync(debugLog,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 特殊reportId请求\n" +
                    $"  businessNo: {businessNo}\n" +
                    $"  reportId: {reportId}\n" +
                    $"  currentMode: {currentMode}\n");
            }

            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询报告明细(Oracle版本)");
                var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $" 开始处理请求 | businessNo: {businessNo},参数:reportId:{reportId},参数:currentMode:{currentMode}");

                var result = await _personService.GetReportDetailsFixedAsync(businessNo, reportId, currentMode);

                await LogToFileAsync(logPath, $" 请求处理完成 | 找到数据: {(result != null ? "是" : "否")}");

                return result != null ? Ok(result) : NotFound("未找到匹配记录");
            }
            catch (Exception ex)
            {
                // 详细记录异常
                var errorLogDir = Path.Combine(_env.ContentRootPath, "Logs2", "API错误");
                var errorLogPath = Path.Combine(errorLogDir, $"error_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(errorLogDir);

                await System.IO.File.AppendAllTextAsync(errorLogPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 接口异常\n" +
                    $"  参数: businessNo={businessNo}, reportId={reportId}, currentMode={currentMode}\n" +
                    $"  异常类型: {ex.GetType().Name}\n" +
                    $"  异常消息: {ex.Message}\n" +
                    $"  堆栈跟踪: {ex.StackTrace}\n");

                if (ex is OracleException oracleEx)
                {
                    await System.IO.File.AppendAllTextAsync(errorLogPath,
                        $"  Oracle错误代码: {oracleEx.Number}\n" +
                        $"  Oracle过程: {oracleEx.Procedure}\n");
                }

                // 返回JSON错误，而不是让ASP.NET Core返回HTML
                return StatusCode(500, new
                {
                    success = false,
                    error = "服务器内部错误",
                    message = ex.Message,
                    reportId = reportId,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }
        [HttpGet("report-details-oracle2/{businessNo}/{reportId}/{currentMode}")]
        public async Task<ActionResult<List<Test_Detail_oracle>>> GetReportDetailsOracle2(
     [FromRoute] string? businessNo,
     [FromRoute] string? reportId,
     [FromRoute] string? currentMode)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询报告明细(Oracle版本)");
            var logPath = Path.Combine(logDir, $"details2_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                // 1. 创建目录
                Directory.CreateDirectory(logDir);

                // 2. 记录开始日志
                await LogToFileAsync(logPath, $"【开始】处理请求 | businessNo: {businessNo}, reportId: {reportId}, currentMode: {currentMode}");

                // 3. 直接返回测试数据，不调用服务层
                var testResult = new Test_Detail_oracle
                {
                    Reports_TEST_REC = new List<T_TEST_REC_oracle>
            {
                new T_TEST_REC_oracle
                {
                    Id = reportId ?? "test_id",
                    BusinessNo = businessNo ?? "test_business",
                    PatientName = "测试患者",
                    TestReportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }
            }
                };

                var details = new List<Test_Detail_oracle> { testResult };

                // 4. 记录完成日志
                await LogToFileAsync(logPath, $"【完成】返回测试数据 | 记录数: 1");

                return Ok(details);
            }
            catch (Exception ex)
            {
                // 5. 记录异常日志
                try
                {
                    var errorLogPath = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log");
                    await LogToFileAsync(errorLogPath, $"【异常】处理请求时出错: {ex.Message}");
                }
                catch { }

                return StatusCode(500, $"服务器内部错误: {ex.Message}");
            }
        }
        [HttpGet("test-log-only/{reportId}")]
        public async Task<IActionResult> TestLogOnly(string reportId)
        {
            // 使用和原接口完全相同的日志路径
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询报告明细(Oracle版本)");
            var logPath = Path.Combine(logDir, $"test_only_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                // 1. 创建目录
                Directory.CreateDirectory(logDir);
                var logDirExists = Directory.Exists(logDir);

                // 2. 记录日志 - 使用正确的方法
                var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - 测试日志，reportId={reportId}\n";
                await System.IO.File.AppendAllTextAsync(logPath, message);

                var logCreated = System.IO.File.Exists(logPath);
                var fileSize = logCreated ? new FileInfo(logPath).Length : 0;

                var result = new
                {
                    success = true,
                    logCreated = logCreated,
                    logPath = logPath,
                    logDirExists = logDirExists,
                    fileSize = fileSize,
                    reportId = reportId,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = new
                {
                    success = false,
                    logCreated = System.IO.File.Exists(logPath),
                    logPath = logPath,
                    logDirExists = Directory.Exists(logDir),
                    error = ex.Message,
                    errorType = ex.GetType().Name,
                    reportId = reportId
                };

                return StatusCode(500, result);
            }
        }

        [HttpGet("report-details-oracle3/{businessNo}/{reportId}/{currentMode}")]
        public async Task<ActionResult<List<Test_Detail_oracle>>> GetReportDetailsOracle3(
            [FromRoute] string? businessNo,
            [FromRoute] string? reportId,
            [FromRoute] string? currentMode)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询报告明细(Oracle版本)");
            var logPath = Path.Combine(logDir, $"details2_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                // 1. 创建目录
                Directory.CreateDirectory(logDir);

                // 2. 记录开始日志
                await System.IO.File.AppendAllTextAsync(logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 【开始】处理请求\n" +
                    $"  参数: businessNo={businessNo}\n" +
                    $"  参数: reportId={reportId}\n" +
                    $"  参数: currentMode={currentMode}\n");

                // 3. 直接返回测试数据
                var testResult = new Test_Detail_oracle
                {
                    Reports_TEST_REC = new List<T_TEST_REC_oracle>
            {
                new T_TEST_REC_oracle
                {
                    Id = reportId ?? "test_id",
                    BusinessNo = businessNo ?? "test_business",
                    PatientName = "测试患者",
                    TestReportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }
            },
                    Report_testr_res_indicate = new List<T_testr_res_indicate_oracle>(),
                    Report_TMICROBE_BACTERIA_RES = new List<T_MICROBE_BACTERIA_RES_oracle>(),
                    Report_TMICROBE_SUSCEPT_RES = new List<T_MICROBE_SUSCEPT_RES_oracle>()
                };

                var details = new List<Test_Detail_oracle> { testResult };

                // 4. 记录完成日志
                await System.IO.File.AppendAllTextAsync(logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 【完成】返回测试数据\n" +
                    $"  数据条数: 1\n");

                return Ok(details);
            }
            catch (Exception ex)
            {
                // 5. 记录异常日志
                try
                {
                    var errorLogPath = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log");
                    await System.IO.File.AppendAllTextAsync(errorLogPath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 【异常】\n" +
                        $"  参数: reportId={reportId}\n" +
                        $"  错误: {ex.Message}\n");
                }
                catch { }

                return StatusCode(500, $"服务器内部错误: {ex.Message}");
            }
        }
        /// <summary>
        /// 获取微生物检验及药敏结果(Oracle版本)
        /// </summary>
        [HttpGet("microbial-report-oracle/{businessNo}/{reportId}")]
        public async Task<ActionResult<Test_Detail_oracle>> GetCombinedMicrobialReportOracle(
            [FromRoute] string businessNo,
            [FromRoute] string reportId)
        {
            try
            {
                var result = await _personService.GetCombinedMicrobialReportAsync(businessNo, reportId);
                return result != null ? Ok(result) : NotFound("未找到匹配记录");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"服务器内部错误: {ex.Message}");
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Person>>> Get()
        {
            var persons = await _personService.GetAllPersonsAsync();
            return Ok(persons);
        }/// <summary>
         /// 获取检验明细
         /// </summary>
        [HttpGet("detail/{reportId}/{ID}")]
        public async Task<ActionResult<IEnumerable<Test_Detail_oracle>>> GetDetails(string reportId, string ID)
        {
            var persons = await _personService.GetDetailsAsync(reportId, ID);
            return Ok(persons);
        }
        /// <summary>
        /// 获取检验列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("List/{UserID}")]
        public async Task<ActionResult<IEnumerable<Test_Detail_oracle>>> GetDetailis(string UserID)
        {
            var reports = await _personService.GetAllPersonsListAsync(UserID);
            return Ok(reports.Select(r => new {
                reportId = r.TestReportNo, // 假设这是报告ID字段
                orgName = r.TestApplyOrgName,
                //projNameExp = r.TestProjCategoryName,
                reportTime = r.TestRecTime,
                ID = r.Id
            }));
        }
        [HttpGet("detailLis/{ReportId}")]
        public async Task<IActionResult> GetDetailisDetail(string reportId)
        {
            var reports = await _personService.GetAllPersonsDetailtAsync(reportId);
            return Ok(reports.Select(r => new {
                testProjNameExp = r.TestProjNameExp, // 假设这是报告ID字段
                testIndexResult = r.TestIndexResult,
                //testIndexUint = r.TestIndexUint,
                normalRefLimit = r.NormalRefLimit,
                anomalyCode = r.AnomalyCode
            }));
        }
        /// <summary>
        /// 获取病人信息
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        [HttpGet("PatientDetails/{patientId}")]
        public async Task<ActionResult<IEnumerable<PatientInfo>>> GetPatientDetails(
         string patientId)
        {
            var reports = await _personService.GetPatientDetailsAsync(patientId);
            return Ok(reports.Select(r => new {
                Name = r.Name,
                Gender = r.Gender,
                IdNumber = r.IdNumber,
                MobileNumber = r.MobileNumber,
                HomeAddress = r.HomeAddress,
                BirthDate = r.BirthDate
            }));
        }
        /// <summary>
        /// 获取报告的互认状态
        /// </summary>
        [HttpGet("recognition-status/{patientId}/{reportId}/{doctorId}")]
        public async Task<IActionResult> GetRecognitionStatus(string patientId, string reportId,string doctorId)
        {
            try
            {
                var status = await _personService.GetRecognitionStatusAsync(patientId, reportId, doctorId);
                return Ok(new
                {
                    isRecognized = status?.RECOGNITION_STATUS,//互认状态 0未互认 1已互认
                    externalCode = status?.EXTERNAL_CODE,
                    externalMsg = status?.EXTERNAL_MSG,
                    SearchType= status?.SearchType,
                    VIEW_STATUS= status?.VIEW_STATUS,//查阅状态 0未查阅 1已查阅
                    quote_STATUS= status?.REFERENCE_RECORD_ID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取互认状态失败: {ex.Message}");
            }
        }
        // <summary>
        /// 获取报告的申请单ID
        /// </summary>
        [HttpGet("apply-id/{patientId}/{reportId}/{doctorId}")]
        public async Task<IActionResult> GetApplyId(string patientId, string reportId, string doctorId)
        {
            try
            {
                var applyId = await _personService.GetRecognitionStatusAsync(patientId, reportId, doctorId);
                return Ok(new { applyId = applyId?.APPLY_ID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取申请单ID失败: {ex.Message}");
            }
        }
        // <summary>
        /// 获取报告的的样本条码
        /// </summary>
        [HttpGet("Spcmbc-No/{ApplyID}")]
        public async Task<IActionResult> GetSpcmbcNo(int ApplyID)
        {
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "获取报告的的样本条码");
                var logPath = Path.Combine(logDir, $"details{DateTime.Now:yyyyMMdd}.log");
                var result = await _personService.GetSpcmbcNo(ApplyID);
                await LogToFileAsync(logPath, $" 返回请求 | ApplyID: {ApplyID}, result: {result}");
                if (result == null || string.IsNullOrEmpty(result.SpcmbcNo))
                {
                    return NotFound(new { Message = "未找到对应的样本条码" });
                }

                return Ok(new { SpcmbcNo = result.SpcmbcNo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取样本条码失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 3.2.2.5.9.互认记录上传（上传不互认记录）
        /// </summary>
        [HttpPost("submit-recognition")]
        public async Task<IActionResult> SubmitRecognition([FromBody] RecognitionRequest request)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "3.2.2.5.9.互认记录上传（上传不互认记录）");
            var logPath = Path.Combine(logDir, $"recognition_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理请求 | 请求体: {JsonSerializer.Serialize(request)}");

                // 1. 根据reportType查询不同的报告表
                object report;
                if (request.ReportType == "exam")
                {
                    report = await _context.t_CHECK_RECs
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == request.ReportId);
                }
                else
                {
                    report = await _context.T_TEST_RECS
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == request.ReportId);
                }

                if (report == null)
                {
                    await LogToFileAsync(logPath, $"错误: 未找到报告ID {request.ReportId}");
                    return BadRequest("未找到对应的报告信息");
                }

                // 动态获取报告属性
                var reportNo = "";
                var projCategoryName = "";
                DateTime? reportDate = DateTime.Now;
                if (request.ReportType == "exam")
                {
                    reportNo = report.GetType().GetProperty("CheckReportNo")?.GetValue(report)?.ToString() ?? "";
                    projCategoryName = report.GetType().GetProperty("CheckProjNameExp")?.GetValue(report)?.ToString() ?? "";
                    reportDate = report.GetType().GetProperty("CheckReportDate")?.GetValue(report) as DateTime? ?? DateTime.Now;
                }
                else
                {
                    reportNo = report.GetType().GetProperty("TestReportNo")?.GetValue(report)?.ToString() ?? "";
                    projCategoryName = report.GetType().GetProperty("TestProjCategoryName")?.GetValue(report)?.ToString() ?? "";
                    reportDate = report.GetType().GetProperty("TestReportDate")?.GetValue(report) as DateTime? ?? DateTime.Now;
                }

                var orgCode = report.GetType().GetProperty("OrgCode")?.GetValue(report)?.ToString() ?? "";
                var orgName = report.GetType().GetProperty("OrgName")?.GetValue(report)?.ToString() ?? "";
                var dataCode = report.GetType().GetProperty("DataCode")?.GetValue(report)?.ToString() ?? "";
                var idCardTypeCode = report.GetType().GetProperty("IdCardTypeCode")?.GetValue(report)?.ToString() ?? "";

                await LogToFileAsync(logPath, $"成功查询到报告: {reportNo}");



                // 4. 调用三方接口，使用互认记录ID作为mrId
                var requestData = new[]
                {
                    new
                    {
                        patientId = request.PatientId,
                        GHId = request.GHId,
                        itemType = request.ReportType == "exam" ? "PACS" : "LIS",
                        itemCode = dataCode,
                        reportOrgCode = orgCode,
                        reportOrgName = orgName,
                        reportNo = reportNo,
                        mrId = Guid.NewGuid().ToString("N"), // 每条记录生成独立GUID,
                        idCardTypeCode = idCardTypeCode,
                        reportName = projCategoryName,
                        reportTime = reportDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                        operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        recognise = request.recognise,
                        reasonCode = request.reasonCode,
                        reasonName =  request.reasonName,
                        crossFlag = "0",
                        diagName =request.diagName,
                        REPORT_ID=request.ReportId,
                    }
                };

                await LogToFileAsync(logPath, $"数据处理平台接口请求数据: {JsonSerializer.Serialize(requestData)}");

                var client = new HttpClient();
                var response = await client.PostAsJsonAsync(_recognitionApiUrl, requestData);

                await LogToFileAsync(logPath, $"数据处理平台接口响应状态: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await LogToFileAsync(logPath, $"数据处理平台接口错误详情: {errorContent}");
                    return BadRequest("调用互认接口失败");
                }

                // 5. 处理响应并更新记录
                var result = await response.Content.ReadFromJsonAsync<RecognitionResponse>();
                await LogToFileAsync(logPath, $"三方接口原始响应: {result}");
                await LogToFileAsync(logPath, $"数据处理平台接口业务响应: code={result.code}, msg={result.msg}");

                // 4. 保存记录
                var recognitionRecord = new RecognitionRecord
                {
                    REPORT_ID = request.ReportId,
                    PATIENT_ID = request.PatientId,
                    GH_ID = request.GHId,
                    RECOGNITION_STATUS = request.czlxValue == "4" ? "2" : (result.code == "00000" ? "0" : "2"), //点击撤销互认时,上传不互认记录,同时把状态变成2未处理状态
                    RECOGNITION_RECORD_ID = requestData[0].mrId, // 设置互认记录上传ID
                                                                 //CREATE_TIME = DateTime.Now,
                    UPDATE_TIME = DateTime.Now,
                    DoctorId= request.DoctorId,
                };

                // 更新记录
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 保存本地记录 | 请求体: {JsonSerializer.Serialize(recognitionRecord)}");
                await _personService.SaveRecognitionRecordAsync(recognitionRecord);
                await LogToFileAsync(logPath, "本地记录更新成功");

                await LogToFileAsync(logPath, $"=========================================================================================================================================");
                return Ok(new { success = result.code == "00000", code = result.code, message = result.msg });
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex}\n堆栈: {ex.StackTrace}");
                return StatusCode(500, $"提交互认失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 批量提交报告互认 3.2.2.5.9.互认记录上传（上传不互认记录）
        /// </summary>
        //[HttpPost("submit-batch-recognition")]
        //public async Task<IActionResult> SubmitBatchRecognition([FromBody] List<ReminderRecordRequest> requests)
        //{
        //    var logDir = Path.Combine(_env.ContentRootPath, "Logs", "3.2.2.5.9.互认记录上传（上传不互认记录）(批量)");
        //    var logPath = Path.Combine(logDir, $"batch_recognition_{DateTime.Now:yyyyMMdd}.log");

        //    try
        //    {
        //        Directory.CreateDirectory(logDir);
        //        await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始批量处理请求 | 共 {requests.Count} 条");
        //        await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始批量处理请求 | 请求体: {JsonSerializer.Serialize(requests)}");

        //        var results = new List<RecognitionResponse>();
        //        int successCount = 0;
        //        int failedCount = 0;
        //        var recognitionRecords = new List<RecognitionRecord>();
        //        // 1. 准备批量请求数据



        //        var batchRequestData = requests.Select(request => new
        //        {

        //            patientId = request.PatientId,
        //            GHId = request.GHId,
        //            itemType = request.ReportType == "exam" ? "PACS" : "LIS",
        //            itemCode = request.dataCode,
        //            reportOrgCode = request.OrgCode,
        //            reportOrgName = request.OrgName,
        //            reportNo = request.ReportNo,
        //            mrId = Guid.NewGuid().ToString("N"), // 每条记录生成独立GUID,,checkProjNameExp
        //            reportName = request.ReportName,
        //            reportTime = request.ReportTime,
        //            operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        //            recognise = 0,
        //            reasonCode = request.reasonCode,
        //            reasonName = request.reasonName,
        //            crossFlag = "0",
        //            diagName = request.diagName,
        //            reportId = request.ReportId,
        //            DoctorId = request.doctorId
        //        }).ToList();

        //        await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 准备数据 | 请求体: {JsonSerializer.Serialize(batchRequestData)}");
        //        // 2. 批量保存本地记录
        //        //if (recognitionRecords.Any())
        //        //{
        //        //    await _personService.BulkSaveRecognitionRecordsAsync(recognitionRecords);
        //        //    await LogToFileAsync(logPath, $"成功保存 {recognitionRecords.Count} 条本地记录");
        //        //    await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 成功保存 | 请求体: {JsonSerializer.Serialize(recognitionRecords)}");
        //        //}

        //        // 3. 调用三方批量接口
        //        using (var client = new HttpClient())
        //        {
        //            var sw = System.Diagnostics.Stopwatch.StartNew();
        //            var response = await client.PostAsJsonAsync(_recognitionApiUrl, batchRequestData);
        //            sw.Stop();

        //            await LogToFileAsync(logPath, $"三方接口调用完成 | 耗时: {sw.ElapsedMilliseconds}ms | 状态: {response.StatusCode}");

        //            if (!response.IsSuccessStatusCode)
        //            {
        //                var errorContent = await response.Content.ReadAsStringAsync();
        //                await LogToFileAsync(logPath, $"三方接口错误: {errorContent}");
        //                return BadRequest("调用批量互认接口失败");
        //            }

        //            // 4. 处理批量响应
        //            var batchResult = await response.Content.ReadFromJsonAsync<RecognitionResponse>();
        //            string responseMessage = batchResult.msg ?? batchResult.message; // 兼容两种字段
        //            await LogToFileAsync(logPath, $"数据处理平台接口业务响应: code={batchResult.code}, msg={responseMessage}");

        //            // 5. 更新本地记录状态
        //            var updateRecords = new List<RecognitionRecord>();
        //            foreach (var request in batchRequestData)
        //            {
        //                // 生成互认记录上传ID (使用GUID)
        //                // 创建本地记录
        //                var recognitionRecord = new RecognitionRecord
        //                {
        //                    REPORT_ID = request.reportId,
        //                    PATIENT_ID = request.patientId,
        //                    GH_ID = request.GHId,
        //                    RECOGNITION_STATUS = batchResult.code == "00000" ? "0" : "2",
        //                    RECOGNITION_RECORD_ID = request.mrId,//
        //                    UPDATE_TIME = DateTime.Now,
        //                    REPORT_TYPE = request.itemType,
        //                    EXTERNAL_CODE = batchResult.code,
        //                    EXTERNAL_MSG = responseMessage,
        //                    DoctorId= request.DoctorId,
        //                };
        //                recognitionRecords.Add(recognitionRecord);

        //            }

        //            //批量保存本地记录
        //            if (recognitionRecords.Any())
        //            {
        //                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 保存本地记录 | 请求体: {JsonSerializer.Serialize(recognitionRecords)}");
        //                await _personService.BulkSaveRecognitionRecordsAsync(recognitionRecords);
        //                // await LogToFileAsync(logPath, $"成功保存 {recognitionRecords.Count} 条本地记录");

        //            }
        //            await LogToFileAsync(logPath, $"=========================================================================================================================================");
        //            //await LogToFileAsync(logPath, $"批量处理完成 | 成功: {successCount} | 失败: {failedCount}");
        //            return Ok(new { success = batchResult.code == "00000", code = batchResult.code, message = responseMessage });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await LogToFileAsync(logPath, $"批量处理出错: {ex}\n堆栈: {ex.StackTrace}");
        //        return StatusCode(500, $"批量提交互认失败: {ex.Message}");
        //    }
        //}

        /// <summary>
        /// 批量提交报告互认 3.2.2.5.9.互认记录上传（上传不互认记录）
        /// </summary>
        [HttpPost("submit-batch-recognition")]
        public async Task<IActionResult> SubmitBatchRecognition([FromBody] List<ReminderRecordRequest> requests)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "3.2.2.5.9.互认记录上传（上传不互认记录）(批量)");
            var logPath = Path.Combine(logDir, $"batch_recognition_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始批量处理请求 | 共 {requests.Count} 条");
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始批量处理请求 | 请求体: {JsonSerializer.Serialize(requests)}");

                var recognitionRecords = new List<RecognitionRecord>();

                // 1. 准备批量请求数据
                var batchRequestData = requests.Select(request => new
                {
                    patientId = request.PatientId,
                    GHId = request.GHId,
                    itemType = request.ReportType == "exam" ? "PACS" : "LIS",
                    itemCode = request.dataCode,
                    reportOrgCode = request.OrgCode,
                    reportOrgName = request.OrgName,
                    reportNo = request.ReportNo,
                    mrId = Guid.NewGuid().ToString("N"),
                    reportName = request.ReportName,
                    reportTime = request.ReportTime,
                    operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    recognise = 0,
                    reasonCode = request.reasonCode,
                    reasonName = request.reasonName,
                    crossFlag = "0",
                    diagName = request.diagName,
                    reportId = request.ReportId,
                    DoctorId = request.doctorId
                }).ToList();

                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 准备数据 | 请求体: {JsonSerializer.Serialize(batchRequestData)}");

                // 2. 调用三方批量接口
                using (var client = new HttpClient())
                {
                    // 设置合理的超时时间
                    client.Timeout = TimeSpan.FromSeconds(30);

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    HttpResponseMessage response;
                    string responseContent = "";

                    try
                    {
                        response = await client.PostAsJsonAsync(_recognitionApiUrl, batchRequestData);
                        responseContent = await response.Content.ReadAsStringAsync();
                    }
                    catch (HttpRequestException httpEx)
                    {
                        await LogToFileAsync(logPath, $"HTTP请求异常: {httpEx.Message}");
                        if (httpEx.InnerException != null)
                        {
                            await LogToFileAsync(logPath, $"内部异常: {httpEx.InnerException.Message}");
                        }

                        return StatusCode(503, new
                        {
                            success = false,
                            code = "NETWORK_ERROR",
                            message = $"网络请求失败: {httpEx.Message}",
                            originalResponse = httpEx.Message
                        });
                    }
                    catch (TaskCanceledException timeoutEx)
                    {
                        await LogToFileAsync(logPath, $"请求超时: {timeoutEx.Message}");

                        return StatusCode(504, new
                        {
                            success = false,
                            code = "TIMEOUT_ERROR",
                            message = "请求第三方接口超时",
                            originalResponse = timeoutEx.Message
                        });
                    }

                    sw.Stop();

                    await LogToFileAsync(logPath, $"三方接口调用完成 | 耗时: {sw.ElapsedMilliseconds}ms | 状态: {response.StatusCode}");
                    await LogToFileAsync(logPath, $"三方接口原始响应内容: {responseContent}");

                    // 3. 处理响应
                    bool isSuccess = false;
                    string responseCode = "";
                    string responseMessage = "";
                    RecognitionResponse batchResult = null;

                    // 检查响应内容是否为JSON
                    if (!string.IsNullOrEmpty(responseContent) && IsValidJson(responseContent))
                    {
                        try
                        {
                            batchResult = JsonSerializer.Deserialize<RecognitionResponse>(responseContent);
                            responseCode = batchResult?.code ?? "UNKNOWN_CODE";
                            responseMessage = batchResult?.msg ?? batchResult?.message ?? "未知响应";
                            isSuccess = responseCode == "00000";

                            await LogToFileAsync(logPath, $"成功解析JSON响应: code={responseCode}, message={responseMessage}, success={isSuccess}");
                        }
                        catch (JsonException jsonEx)
                        {
                            await LogToFileAsync(logPath, $"JSON解析失败: {jsonEx.Message}");
                            responseCode = "JSON_PARSE_ERROR";
                            responseMessage = $"JSON解析失败: {TruncateString(responseContent, 200)}";
                        }
                    }
                    else
                    {
                        // 非JSON响应，直接返回原始内容
                        responseCode = "NON_JSON_RESPONSE";
                        responseMessage = "第三方接口返回了非JSON格式的响应";

                        await LogToFileAsync(logPath, $"检测到非JSON响应，返回原始内容给前端");
                    }

                    // 4. 只有在成功时才写入本地库
                    if (isSuccess && batchResult != null)
                    {
                        await LogToFileAsync(logPath, $"开始创建本地记录，共 {batchRequestData.Count} 条");

                        foreach (var request in batchRequestData)
                        {
                            var recognitionRecord = new RecognitionRecord
                            {
                                REPORT_ID = request.reportId,
                                PATIENT_ID = request.patientId,
                                GH_ID = request.GHId,
                                RECOGNITION_STATUS = "0", // 成功状态
                                RECOGNITION_RECORD_ID = request.mrId,
                                UPDATE_TIME = DateTime.Now,
                                REPORT_TYPE = request.itemType,
                                EXTERNAL_CODE = responseCode,
                                EXTERNAL_MSG = responseMessage,
                                DoctorId = request.DoctorId,
                            };
                            recognitionRecords.Add(recognitionRecord);
                        }

                        // 保存到本地数据库
                        if (recognitionRecords.Any())
                        {
                            try
                            {
                                await _personService.BulkSaveRecognitionRecordsAsync(recognitionRecords);
                                await LogToFileAsync(logPath, $"成功保存 {recognitionRecords.Count} 条本地记录到数据库");
                            }
                            catch (Exception dbEx)
                            {
                                await LogToFileAsync(logPath, $"保存本地记录到数据库失败: {dbEx.Message}");
                                // 数据库保存失败不影响整体流程，继续返回成功
                            }
                        }
                    }
                    else
                    {
                        await LogToFileAsync(logPath, $"第三方接口响应失败，跳过保存本地记录");
                    }

                    await LogToFileAsync(logPath, $"=========================================================================================================================================");

                    // 5. 返回结果
                    if (isSuccess)
                    {
                        return Ok(new
                        {
                            success = true,
                            code = responseCode,
                            message = responseMessage,
                            originalResponse = responseContent,
                            localRecordsSaved = recognitionRecords.Count
                        });
                    }
                    else
                    {
                        // 将BadRequest改为Ok，但设置success为false
                        return Ok(new
                        {
                            success = false,  // 业务失败
                            code = responseCode,
                            message = responseMessage,
                            originalResponse = responseContent
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"批量处理出错: {ex}\n堆栈: {ex.StackTrace}");
                return StatusCode(500, new
                {
                    success = false,
                    code = "INTERNAL_ERROR",
                    message = $"批量提交互认失败: {ex.Message}",
                    originalResponse = ex.Message
                });
            }
        }

        // 辅助方法
        private bool IsValidJson(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return false;

            jsonString = jsonString.Trim();
            return (jsonString.StartsWith("{") && jsonString.EndsWith("}")) ||
                   (jsonString.StartsWith("[") && jsonString.EndsWith("]"));
        }

        private string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                return input;

            return input.Substring(0, maxLength) + "...";
        }
        /// <summary>
        /// 3.2.2.5.8.提醒记录上传
        /// </summary>
        [HttpPost("upload-reminder-records")]
        public async Task<IActionResult> UploadReminderRecords([FromBody] List<ReminderRecordRequest> requests)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "3.2.2.5.8.提醒记录上传日志");
            var logPath = Path.Combine(logDir, $"reminder_{DateTime.Now:yyyyMMdd}.log");
            var recognitionRecords = new List<RecognitionRecord>();
            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理提醒记录 | 共 {requests.Count} 条");

                // 生成互认记录上传ID (使用GUID)
                //var recognitionRecordId = Guid.NewGuid().ToString("N");
                // 1. 准备批量请求数据
                var batchRequestData = requests.Select(request => new
                {

                    patientId = request.PatientId,
                    GHId = request.GHId,
                    itemType = request.ReportType == "exam" ? "PACS" : "LIS",
                    reportOrgCode = request.OrgCode,
                    reportOrgName = request.OrgName,
                    reportNo = request.ReportNo,
                    reportName = request.ReportName,
                    reportTime = request.ReportTime,
                    operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    remindId = Guid.NewGuid().ToString("N"), // 每条记录生成独立GUID,
                    searchType = request.SearchType,
                    operateType = "1",// 1表示提醒
                    reportId = request.ReportId,
                    DoctorId= request.doctorId,
                    itemCode= request.dataCode,
                    OrderId =request.OrderId
                }).ToList();

                await LogToFileAsync(logPath, $"准备调用三方提醒接口，数据: {JsonSerializer.Serialize(batchRequestData)}");

                // 2. 调用三方接口
                var client = new HttpClient();
                //var response = await client.PostAsJsonAsync(
                //    "http://10.10.1.46:8092/uploadReminderRecords",
                //    batchRequestData);
                var response = await client.PostAsJsonAsync(_reminderApiUrl, batchRequestData);

                await LogToFileAsync(logPath, $"三方接口响应状态: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await LogToFileAsync(logPath, $"三方接口错误: {errorContent}");
                    return BadRequest("调用提醒接口失败");
                }


                var responseContent = await response.Content.ReadAsStringAsync();
                await LogToFileAsync(logPath, $"三方接口原始响应: {responseContent}");
                // 3. 处理响应
                var result = await response.Content.ReadFromJsonAsync<RecognitionResponse>();
                await LogToFileAsync(logPath, $"数据处理平台接口业务响应: code={result.code}, msg={result.msg}");



                foreach (var request in batchRequestData)
                {
                    // 生成互认记录上传ID (使用GUID)
                    // 创建本地记录
                    var recognitionRecord = new RecognitionRecord
                    {
                        REPORT_ID = request.reportId,
                        PATIENT_ID = request.patientId,
                        GH_ID = request.GHId,
                        //RECOGNITION_STATUS = 2, // 初始状态为未处理
                        REMINDER_RECORD_ID = request.remindId,//提醒记录上传ID
                        UPDATE_TIME = DateTime.Now,
                        REPORT_TYPE = request.itemType,
                        EXTERNAL_CODE = result.code,
                        EXTERNAL_MSG = result.msg,
                        DoctorId= request.DoctorId,
                        OrderId=request.OrderId,
                        VIEW_STATUS="1"

                    };
                    recognitionRecords.Add(recognitionRecord);

                }
                await LogToFileAsync(logPath, $"生成的本地记录数量: {recognitionRecords.Count}");
                //批量保存本地记录
                if (recognitionRecords.Any())
                {
                    await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 保存本地记录 | 请求体: {JsonSerializer.Serialize(recognitionRecords)}");
                    await _personService.BulkSaveRecognitionRecordsAsync(recognitionRecords);
                    // await LogToFileAsync(logPath, $"成功保存 {recognitionRecords.Count} 条本地记录");

                }
                await LogToFileAsync(logPath, $"=========================================================================================================================================");
                return Ok(new { success = result.code == "00000", code = result.code, message = result.msg });
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"处理提醒记录出错: {ex}\n堆栈: {ex.StackTrace}");
                return StatusCode(500, $"上传提醒记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 3.2.2.5.10.调阅记录上传
        /// </summary>
        [HttpPost("upload-view-record")]
        public async Task<IActionResult> UploadViewRecord([FromBody] ViewRecordRequest requestS)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "3.2.2.5.10.调阅记录上传日志");
            var logPath = Path.Combine(logDir, $"view_{DateTime.Now:yyyyMMdd}.log");
            var recognitionRecords = new List<RecognitionRecord>();
            // 1. 根据reportType查询不同的报告表
            object report = await GetReportByTypeAsync(requestS.ReportType, requestS.ReportId);

            if (report == null)
            {
                await LogToFileAsync(logPath, $"错误: 未找到报告ID {requestS.ReportId}");
                return BadRequest("未找到对应的报告信息");
            }

            // 检查实际类型和属性
            var reportType = report.GetType();

            //await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 实际类型 | : {reportType.FullName}");
            //await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 所有属性 | : " + string.Join(", ", reportType.GetProperties().Select(p => p.Name)));
            //Console.WriteLine($"实际类型: {reportType.FullName}");
            //Console.WriteLine("所有属性: " + string.Join(", ", reportType.GetProperties().Select(p => p.Name)));




            var reportNo = "";
            var projCategoryName = "";
            DateTime? reportDate = DateTime.Now;
            // 动态获取报告属性
            if (requestS.ReportType == "exam")
            {
                reportNo = report.GetType().GetProperty("CheckReportNo")?.GetValue(report)?.ToString() ?? "";//报告单编号
                projCategoryName = report.GetType().GetProperty("CheckProjNameExp")?.GetValue(report)?.ToString() ?? "";//报告名称
                reportDate = report.GetType().GetProperty("CheckReportDate")?.GetValue(report) as DateTime? ?? DateTime.Now;
            }
            else
            {
                reportNo = report.GetType().GetProperty("TestReportNo")?.GetValue(report)?.ToString() ?? "";//报告单编号 
                projCategoryName = report.GetType().GetProperty("TestProjCategoryName")?.GetValue(report)?.ToString() ?? "";//报告名称
                reportDate = report.GetType().GetProperty("TestReportDate")?.GetValue(report) as DateTime? ?? DateTime.Now;
            }


            var orgCode = report.GetType().GetProperty("OrgCode")?.GetValue(report)?.ToString() ?? "";//报告单医疗机构代码
            var orgName = report.GetType().GetProperty("OrgName")?.GetValue(report)?.ToString() ?? "";//报告单医疗机构名称
            var dataCode = report.GetType().GetProperty("DataCode")?.GetValue(report)?.ToString() ?? ""; //平台项目代码
            var businessNo = report.GetType().GetProperty("BusinessNo")?.GetValue(report)?.ToString() ?? ""; //平台项目代码
            var idCardTypeCode = report.GetType().GetProperty("IdCardTypeCode")?.GetValue(report)?.ToString() ?? ""; //患者证件类型代码 

            await LogToFileAsync(logPath, $"成功查询到报告: {reportNo}");
            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理调阅记录 | 报告ID: {requestS.ReportId}");

                // 1. 准备请求数据
                var requestData = new[]
                {
                    new
                    {
                        patientId = requestS.PatientId,
                        GHId = requestS.GHId,
                        itemType = requestS.ReportType == "exam" ? "PACS" : "LIS",
                        itemCode=dataCode,
                        reportOrgCode = orgCode,
                        reportOrgName = orgName,
                        reportNo = requestS.ReportId,
                        reportName = projCategoryName,
                        reportTime = reportDate,
                        viewTime = requestS.ViewTime,
                        operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        //operateType = "2" ,// 2表示调阅
                        idCardTypeCode = idCardTypeCode,
                        SearchType=requestS.SearchType,
                        businessNo=businessNo,
                        reviewId=Guid.NewGuid().ToString("N"), // 每条记录生成独立GUID,
                        crossFlag = "0",//跨市标记
                        DoctorId=requestS.DoctorId
                    }
                };

                await LogToFileAsync(logPath, $"准备调用三方调阅接口，数据: {JsonSerializer.Serialize(requestData)}");

                // 2. 调用三方接口
                var client = new HttpClient();

                var response = await client.PostAsJsonAsync(_uploadRetrievalRecord, requestData);
                //var response = await client.PostAsJsonAsync(
                //    "http://10.10.1.46:8092/uploadViewRecord",
                //    requestData);

                await LogToFileAsync(logPath, $"三方接口响应状态: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await LogToFileAsync(logPath, $"三方接口错误: {errorContent}");
                    return BadRequest("调用调阅接口失败");
                }

                // 3. 处理响应
                var result = await response.Content.ReadFromJsonAsync<RecognitionResponse>();
                await LogToFileAsync(logPath, $"调阅记录处理完成: {result.code}, {result.msg}");

                //if (result.code == "00000")
                //{
                foreach (var request in requestData)
                {
                    // 生成互认记录上传ID (使用GUID)
                    // 创建本地记录
                    var recognitionRecord = new RecognitionRecord
                    {
                        REPORT_ID = request.reportNo,
                        PATIENT_ID = request.patientId,
                        GH_ID = request.GHId,
                        RECOGNITION_STATUS = result.code == "00000" ? "1" : "2", // 状态为互认状态
                        VIEW_RECORD_ID = request.reviewId,//互认记录上传ID
                        UPDATE_TIME = DateTime.Now,
                        REPORT_TYPE = request.itemType,
                        EXTERNAL_CODE = result.code,
                        EXTERNAL_MSG = result.msg,
                        DoctorId= request.DoctorId
                    };
                    recognitionRecords.Add(recognitionRecord);

                }

                //批量保存本地记录
                if (recognitionRecords.Any())
                {
                    await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 保存本地记录 | 请求体: {JsonSerializer.Serialize(recognitionRecords)}");
                    await _personService.BulkSaveRecognitionRecordsAsync(recognitionRecords);
                    // await LogToFileAsync(logPath, $"成功保存 {recognitionRecords.Count} 条本地记录");

                }

                await LogToFileAsync(logPath, $"=========================================================================================================================================");
                return Ok(new { success = result.code == "00000", code = result.code, message = result.msg });
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"处理调阅记录出错: {ex}\n堆栈: {ex.StackTrace}");
                return StatusCode(500, $"上传调阅记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量互认3.2.2.5.10.调阅记录上传
        /// </summary>
        [HttpPost("PLupload-view-record")]
        public async Task<IActionResult> PLUploadViewRecord([FromBody] List<ViewRecordRequest> requestS)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "3.2.2.5.10.调阅记录上传日志");
            var logPath = Path.Combine(logDir, $"view_{DateTime.Now:yyyyMMdd}.log");
            var recognitionRecords = new List<RecognitionRecord>();

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"传入数据: {JsonSerializer.Serialize(requestS)}");
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理调阅记录 | 共: {requestS.Count}条");

                // 1. 准备请求数据 
                var requestData = requestS.Select(request => new
                {

                    patientId = request.PatientId,
                    GHId = request.GHId,
                    itemType = request.ReportType == "exam" ? "PACS" : "LIS",
                    itemCode = request.dataCode,
                    reportOrgCode = request.OrgCode,
                    reportOrgName = request.OrgName,
                    reportNo = request.ReportNo,
                    reportName = request.ReportName,
                    reportTime = request.ReportTime,
                    //viewTime = request.ViewTime,
                    operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    //operateType = "2",// 2表示调阅
                    idCardTypeCode = request.idCardTypeCode,
                    SearchType = request.SearchType,
                    businessNo = request.businessNo,
                    reviewId = Guid.NewGuid().ToString("N"), // 每条记录生成独立GUID,
                    crossFlag = "0",//跨市标记
                    DoctorId= request.DoctorId,
                    OrderId= request.OrderId,
                    ReportId = request.ReportId
                }).ToList();


                await LogToFileAsync(logPath, $"准备调用三方调阅接口，请求数据: {JsonSerializer.Serialize(requestData)}");
                await LogToFileAsync(logPath, $"请求URL: {_uploadRetrievalRecord}");
                // 2. 调用三方接口
                var client = new HttpClient();

                var response = await client.PostAsJsonAsync(_uploadRetrievalRecord, requestData);


                await LogToFileAsync(logPath, $"三方接口响应状态: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await LogToFileAsync(logPath, $"三方接口错误: {errorContent}");
                    return BadRequest("调用调阅接口失败");
                }

                // 3. 处理响应
                var result = await response.Content.ReadFromJsonAsync<RecognitionResponse>();
                string responseMessage = result.msg ?? result.message; // 兼容两种字段
                await LogToFileAsync(logPath, $"调阅记录处理完成: {result.code}, {responseMessage}");

                //if (result.code == "00000")
                //{
                foreach (var request in requestData)
                {
                    // 生成互认记录上传ID (使用GUID)
                    // 创建本地记录
                    var recognitionRecord = new RecognitionRecord
                    {
                        REPORT_ID = request.ReportId,
                        PATIENT_ID = request.patientId,
                        GH_ID = request.GHId,
                        RECOGNITION_STATUS = result.code == "00000" ? "1" : "2",
                        VIEW_RECORD_ID = request.reviewId,//互认记录上传ID
                        UPDATE_TIME = DateTime.Now,
                        REPORT_TYPE = request.itemType,
                        EXTERNAL_CODE = result.code,
                        EXTERNAL_MSG = responseMessage,
                        DoctorId= request.DoctorId,
                        SearchType= request.SearchType=="1"? (result.code == "00000" ? "1" : "2") :"2",
                        OrderId= request.OrderId,
                        VIEW_STATUS= result.code == "00000" ? "1" : "2",

                    };
                    recognitionRecords.Add(recognitionRecord);

                }
                //var resultlab = await _newStandardService.SubmitVerificationResult(requestS);
                await LogToFileAsync(logPath, $"生成的本地记录数量: {recognitionRecords.Count}");
                //批量保存本地记录
                if (recognitionRecords.Any())
                {
                    await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 保存本地记录 | 请求体: {JsonSerializer.Serialize(recognitionRecords)}");
                    await _personService.BulkSaveRecognitionRecordsAsync(recognitionRecords);
                    // await LogToFileAsync(logPath, $"成功保存 {recognitionRecords.Count} 条本地记录");

                }
                await LogToFileAsync(logPath, $"=========================================================================================================================================");
                return Ok(new { success = result.code == "00000", code = result.code, message = responseMessage });
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"处理调阅记录出错: {ex}\n堆栈: {ex.StackTrace}");
                return StatusCode(500, $"上传调阅记录失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 3.2.2.5.12.撤销‘不互认’操作接口
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("revoke-action")]
        public async Task<IActionResult> RevokeNonRecognition([FromBody] List<ReminderRecordRequest> requestS)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "3.2.2.5.12.撤销‘不互认’操作接口记录日志");
            var logPath = Path.Combine(logDir, $"recognition_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理请求 | 请求体: {JsonSerializer.Serialize(requestS)}");

                await LogToFileAsync(logPath, $"传入数据: {JsonSerializer.Serialize(requestS)}");
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理调阅记录 | 共: {requestS.Count}条");

                // 1. 准备请求数据 
                
                var requestData = new
                {

                    patientId = requestS[0].PatientId,
                    GHId = requestS[0].GHId,
                    reportId = requestS[0].ReportId,
                    itemType = requestS[0].ReportType == "exam" ? "PACS" : "LIS", // 根据类型设置itemType
                    itemCode = requestS[0].dataCode,
                    clientType = 2,//固定值2,1:平台客户端；2.自研客户端；
                    idCardTypeCode = requestS[0].idCardTypeCode,
                    doctorId= requestS[0].doctorId,
                    //operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                await LogToFileAsync(logPath, $"数据处理平台接口请求数据: {JsonSerializer.Serialize(requestData)}");

                var client = new HttpClient();
                var response = await client.PostAsJsonAsync(_revokeNonRecognition, requestData);
                //var response = await client.PostAsJsonAsync(
                //    "http://10.10.1.46:8092/uploadRecognitionRecord",
                //    requestData);

                await LogToFileAsync(logPath, $"数据处理平台接口响应状态: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await LogToFileAsync(logPath, $"数据处理平台接口错误详情: {errorContent}");
                    return BadRequest("调用互认接口失败");
                }

                // 3. 处理响应
                var result = await response.Content.ReadFromJsonAsync<ReminderRecordRequest>();
                string responseMessage = result.msg ?? result.message; // 兼容两种字段
                //await LogToFileAsync(logPath, $"数据处理平台接口业务响应: code={result.code}, msg={result.msg}");
                await LogToFileAsync(logPath, $"数据处理平台接口业务响应: code={result.code}, msg={responseMessage}");

                var existingRecord = await _personService.GetRecognitionStatusAsync(requestS[0].PatientId, requestS[0].ReportId, requestS[0].doctorId);
                //var newStatus = existingRecord?.RECOGNITION_STATUS;

                string finalStatus;
                if (result.code == "00000")
                {
                    finalStatus = "2"; // 
                    await LogToFileAsync(logPath, $"状态查询(成功): code={finalStatus}");
                }
                else
                {
                    finalStatus = existingRecord == null ? "2" : existingRecord.RECOGNITION_STATUS;
                    await LogToFileAsync(logPath, $"状态查询(!00000): code={finalStatus}");
                }

                // 4. 保存记录
                var recognitionRecord = new RecognitionRecord
                {
                    REPORT_ID = requestS[0].ReportId,
                    PATIENT_ID =requestS[0].PatientId,
                    GH_ID = requestS[0].GHId,
                    RECOGNITION_STATUS = finalStatus,
                    REQUEST_ID = result.request_id,
                    EXTERNAL_CODE = result.code,
                    EXTERNAL_MSG = responseMessage,
                    DoctorId=requestS[0].doctorId
                    //REPORT_TYPE = request.ReportType // 保存报告类型
                };

                await _personService.SaveRecognitionRecordAsync(recognitionRecord);
                await LogToFileAsync(logPath, "本地记录保存成功");
                await LogToFileAsync(logPath, $"=========================================================================================================================================");
                return Ok(new { success = result.code == "00000", code = result.code, message = responseMessage });
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex}\n堆栈: {ex.StackTrace}");
                return StatusCode(500, $"提交互认失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 撤销互认记录
        /// </summary>
        /// <param name="requests"></param>
        /// <returns></returns>
        [HttpPost("undo-Recognition")]
        public async Task<IActionResult> RecognitionRevokeCommand([FromBody] List<RecognitionRequest> requests)
        {
            // 初始化日志路径
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "撤销互认记录日志");
            var logPath = Path.Combine(logDir, $"recognition_{DateTime.Now:yyyyMMdd}.log");
            Directory.CreateDirectory(logDir);

            try
            {
                await LogToFileAsync(logPath, $"传入数据d的: {JsonSerializer.Serialize(requests)}");
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理撤销互认 | 共 {requests.Count} 条");
                //await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理请求 | 请求体: {JsonSerializer.Serialize(requests)}");

                // 2. 数据验证
                if (requests == null || !requests.Any())
                {
                    await LogToFileAsync(logPath, "错误: 请求体为空");
                    return BadRequest(new { success = false, code = "400", message = "请求数据不能为空" });
                }

                // 3. 生成撤销记录集合
                var recognitionRecords = requests.Select(request => new RecognitionRecord
                {
                    REPORT_ID = request.ReportId,
                    PATIENT_ID = request.PatientId,
                    GH_ID = request.GHId,
                    RECOGNITION_STATUS = "2", // 0表示撤销
                    //RECOGNITION_RECORD_ID = request.mrId,
                    UPDATE_TIME = DateTime.Now,
                    DoctorId=request.DoctorId

                }).ToList();

                // 4. 批量保存到数据库
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 准备保存 {recognitionRecords.Count} 条记录");
                var saveResult = await _personService.BulkSaveRecognitionRecordsAsync(recognitionRecords);

                // 5. 返回成功响应
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 处理完成");
                await LogToFileAsync(logPath, $"=========================================================================================================================================");
                return Ok(new
                {
                    success = true,
                    code = "00000",
                    message = $"成功撤销 {recognitionRecords.Count} 条记录",
                    data = recognitionRecords.Select(r => r.REPORT_ID)
                });
            }
            catch (Exception ex)
            {
                // 异常处理
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                return StatusCode(500, new
                {
                    success = false,
                    code = "500",
                    message = $"批量撤销失败: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// 3.2.2.5.11.引用记录上传接口
        /// 功能说明：上传引用的检查检验互认报告相关记录信息
        /// 非实时调用接口，记录产生后当天上传即可
        /// </summary>
        [HttpPost("upload-quote-log")]
        public async Task<IActionResult> UploadQuoteLog([FromBody] List<ReminderRecordRequest> requests)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "3.2.2.5.11.引用记录上传接口日志");
            var logPath = Path.Combine(logDir, $"quote_log_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理引用记录上传请求 | 请求体: {JsonSerializer.Serialize(requests)}");
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理引用记录 | 共: {requests.Count}条");

                if (requests == null || requests.Count == 0)
                {
                    await LogToFileAsync(logPath, "请求数据为空");
                    return BadRequest(new { success = false, code = "A1001", message = "请求数据不能为空" });
                }

                var requestData = new
                {
                    patientId = requests[0].PatientId,
                    GHId = requests[0].GHId,
                    reportId = requests[0].ReportId,
                    itemType = requests[0].ReportType == "exam" ? "PACS" : "LIS",
                    itemCode = requests[0].dataCode,
                    clientType = 2, // 固定值2,1:平台客户端；2.自研客户端
                    idCardTypeCode = requests[0].idCardTypeCode,
                    doctorId = requests[0].doctorId,
                    quoteId = Guid.NewGuid().ToString("N"),
                    reportOrgCode = requests[0].OrgCode,
                    reportOrgName = requests[0].OrgName,
                    reportName = requests[0].ReportName,
                    reportTime = requests[0].ReportTime,
                    businessNo = requests[0].BusinessNo,
                    operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    reportNo = requests[0].ReportNo,
                    microbiaMark = requests[0].MicrobiaMark ?? "0" // 默认为"0"
                };

                await LogToFileAsync(logPath, $"数据处理平台接口请求数据: {JsonSerializer.Serialize(requestData)}");

                // 检查必要字段
                if (string.IsNullOrEmpty(requestData.itemCode))
                {
                    await LogToFileAsync(logPath, "错误: itemCode 为空");
                    return BadRequest(new { success = false, code = "A1077", message = "引用项目的指标不能为空" });
                }

                // 2. 调用外部接口
                var client = new HttpClient();
                var response = await client.PostAsJsonAsync(_uploadQuoteLogUrl, requestData);

                await LogToFileAsync(logPath, $"数据处理平台接口响应状态: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                await LogToFileAsync(logPath, $"数据处理平台接口原始响应: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        code = "HTTP_ERROR",
                        message = $"接口调用失败: {response.StatusCode}"
                    });
                }

                // 3. 解析响应
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                string code = result?.ContainsKey("code") == true ? result["code"]?.ToString() : "UNKNOWN";
                string message = result?.ContainsKey("msg") == true ? result["msg"]?.ToString() :
                                result?.ContainsKey("message") == true ? result["message"]?.ToString() : "未知错误";
                string requestId = result?.ContainsKey("request_id") == true ? result["request_id"]?.ToString() : "";

                await LogToFileAsync(logPath, $"数据处理平台接口业务响应: code={code}, msg={message}");

                // 4. 保存记录
                var recognitionRecord = new RecognitionRecord
                {
                    REPORT_ID = requests[0].ReportId,
                    PATIENT_ID = requests[0].PatientId,
                    GH_ID = requests[0].GHId,
                    REQUEST_ID = requestId,
                    EXTERNAL_CODE = code,
                    EXTERNAL_MSG = message,
                    REFERENCE_RECORD_ID = code == "00000" ? "1" : "0", // 根据结果设置
                    DoctorId = requests[0].doctorId,
                    CREATE_TIME = DateTime.Now
                };

                await _personService.SaveRecognitionRecordAsyncUploadQuoteLog(recognitionRecord);

                await LogToFileAsync(logPath, "本地引用记录保存成功");
                await LogToFileAsync(logPath, $"引用结果: code={code}, message={message}, reference_record_id={recognitionRecord.REFERENCE_RECORD_ID}");

                await LogToFileAsync(logPath, $"=========================================================================================================================================");

                // 返回标准格式的响应
                return Ok(new
                {
                    success = code == "00000",
                    code = code,
                    message = message,
                    data = new
                    {
                        reference_record_id = recognitionRecord.REFERENCE_RECORD_ID,
                        request_id = requestId
                    }
                });
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");

                // 保存错误记录
                if (requests != null && requests.Count > 0)
                {
                    try
                    {
                        var errorRecord = new RecognitionRecord
                        {
                            REPORT_ID = requests[0].ReportId,
                            PATIENT_ID = requests[0].PatientId,
                            GH_ID = requests[0].GHId,
                            EXTERNAL_CODE = "EXCEPTION",
                            EXTERNAL_MSG = ex.Message,
                            REFERENCE_RECORD_ID = "0", // 失败为0
                            DoctorId = requests[0]?.doctorId,
                            CREATE_TIME = DateTime.Now
                        };
                        await _personService.SaveRecognitionRecordAsyncUploadQuoteLog(errorRecord);
                    }
                    catch (Exception innerEx)
                    {
                        await LogToFileAsync(logPath, $"保存错误记录失败: {innerEx.Message}");
                    }
                }

                return StatusCode(500, new
                {
                    success = false,
                    code = "EXCEPTION",
                    message = $"上传引用记录失败: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// 调用新标准服务 - 通用接口-新标准服务入口
        /// </summary> 

        [HttpPost("call-new-standard/{serviceMethod}")]
        public async Task<ActionResult<ApiResponse<object>>> CallNewStandardService(
            [FromRoute] string serviceMethod,
            [FromBody] JsonElement requestJson)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S3112新标准服务调用");
            var logPath = Path.Combine(logDir, $"newstandard_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理新标准服务调用");
                await LogToFileAsync(logPath, $"服务方法: {serviceMethod}");
                await LogToFileAsync(logPath, $"请求参数: {JsonSerializer.Serialize(requestJson, new JsonSerializerOptions { WriteIndented = true })}");

                // 1. 验证参数
                if (requestJson.ValueKind == JsonValueKind.Null || requestJson.ValueKind == JsonValueKind.Undefined)
                {
                    await LogToFileAsync(logPath, "错误: 请求参数为空");
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Data = null,
                        Message = "请求参数不能为空"
                    });
                }

                if (string.IsNullOrEmpty(serviceMethod))
                {
                    await LogToFileAsync(logPath, "错误: 服务方法名为空");
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Data = null,
                        Message = "服务方法名不能为空"
                    });
                }

                // 2. 根据服务方法名调用不同的服务
                object result = serviceMethod.ToUpper() switch
                {
                    //"uploadviewrecords" => await HandleUploadViewRecords(requestJson),
                    //"uploadrecognitionrecords" => await HandleUploadRecognitionRecords(requestJson),
                    //"uploadreminderrecords" => await HandleUploadReminderRecords(requestJson),
                    //"revokenonrecognition" => await HandleRevokeNonRecognition(requestJson),
                    //"getreportdetails" => await HandleGetReportDetails(requestJson),
                    "S3112" => await S3112(requestJson, serviceMethod), // 医嘱发送接口
                    //"s4024"=>await S4024(requestJson, serviceMethod),
                    //"reportinspectionfinding" => await HandleReportInspectionFinding(requestJson, serviceMethod),
                    _ => throw new ArgumentException($"不支持的服务方法: {serviceMethod}")
                };

                await LogToFileAsync(logPath, $"处理完成，结果: {JsonSerializer.Serialize(result)}");
                await LogToFileAsync(logPath, $"=========================================================================================================================================");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = result,
                    Message = "操作成功"
                });
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Message = $"调用新标准服务失败: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// S3112二号接口操作 - 开单申请
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="serviceMethod">服务名</param>
        /// <returns></returns>
        [HttpPost("call-new-S3112-v2/{serviceMethod}")]
        public async Task<ActionResult<object>> S3112V2(
            [FromBody] S3112V2Request request,
            [FromRoute] string serviceMethod)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(request.PatientId))
                {
                    return BadRequest(new { Message = "患者ID(patientId)不能为空" });
                }

                if (string.IsNullOrEmpty(request.PatientVisitType))
                {
                    return BadRequest(new { Message = "患者来源(patientVisitType)不能为空" });
                }

                if (string.IsNullOrEmpty(request.SequenceNo))
                {
                    return BadRequest(new { Message = "序号(sequenceNo)不能为空" });
                }

                if (string.IsNullOrEmpty(request.PlacerName))
                {
                    return BadRequest(new { Message = "开单人姓名(placerName)不能为空" });
                }

                if (string.IsNullOrEmpty(request.ApplyDeptId))
                {
                    return BadRequest(new { Message = "开单科室ID(applyDeptId)不能为空" });
                }

                if (string.IsNullOrEmpty(request.ItemId))
                {
                    return BadRequest(new { Message = "项目ID(itemId)不能为空" });
                }

                if (string.IsNullOrEmpty(request.ItemName))
                {
                    return BadRequest(new { Message = "项目名称(itemName)不能为空" });
                }

                // 门诊病人需要挂号单号验证
                if (request.PatientVisitType == "1" && string.IsNullOrEmpty(request.RegistrationNo))
                {
                    return BadRequest(new { Message = "门诊病人必须提供挂号单号(registrationNo)" });
                }

                // 调用S3112二号接口服务
                var result = await _newStandardService.S3112V2(
                    serviceMethod,
                    request.PatientId,
                    request.PatientVisitType,
                    request.HomePageId ?? "",
                    request.SequenceNo,
                    request.PlacerName,
                    request.ApplyDeptId,
                    request.ExecuteDeptId ?? "",
                    request.ItemType ?? "",
                    request.ItemId,
                    request.ItemName,
                    request.ItemExecuteDeptId ?? "",
                    request.PartsName ?? "",
                    request.MethodName ?? "",
                    request.RegistrationNo ?? "");

                // 直接返回接口的原始响应
                return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"S3112二号接口操作失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        // S3112二号接口请求DTO
        public class S3112V2Request
        {
            [JsonPropertyName("patientId")]
            public string PatientId { get; set; }

            [JsonPropertyName("patientVisitType")]
            public string PatientVisitType { get; set; }

            [JsonPropertyName("homePageId")]
            public string HomePageId { get; set; }

            [JsonPropertyName("sequenceNo")]
            public string SequenceNo { get; set; }

            [JsonPropertyName("placerName")]
            public string PlacerName { get; set; }

            [JsonPropertyName("applyDeptId")]
            public string ApplyDeptId { get; set; }

            [JsonPropertyName("executeDeptId")]
            public string ExecuteDeptId { get; set; }

            [JsonPropertyName("itemType")]
            public string ItemType { get; set; }

            [JsonPropertyName("itemId")]
            public string ItemId { get; set; }

            [JsonPropertyName("itemName")]
            public string ItemName { get; set; }

            [JsonPropertyName("itemExecuteDeptId")]
            public string ItemExecuteDeptId { get; set; }

            [JsonPropertyName("partsName")]
            public string PartsName { get; set; }

            [JsonPropertyName("methodName")]
            public string MethodName { get; set; }

            [JsonPropertyName("registrationNo")]
            public string RegistrationNo { get; set; }
        }
        /// <summary>
        /// 处理医嘱发送信息
        /// </summary>
        private async Task<object> S3112(JsonElement requestJson,string serviceMethod)
        {
            try
            {
                // 解析输入数据
                if (!requestJson.TryGetProperty("input", out var inputElement))
                {
                    throw new ArgumentException("缺少input字段");
                }

                if (!inputElement.TryGetProperty("req_info", out var reqInfoElement))
                {
                    throw new ArgumentException("缺少req_info字段");
                }

                //var orderRequests = reqInfoElement.Deserialize<List<OrderInfoRequest>>();
                //if (orderRequests == null || !orderRequests.Any())
                //{
                //    throw new ArgumentException("req_info字段格式错误或为空");
                //}

                //var viewRecords = orderRequests.Select(order => new ViewRecordRequest
                //{
                //    PatientId = order.patientId?.ToString(),
                //    OrderId = order.orderId?.ToString(),
                //    dataCode = order.dataCode,
                //    // 根据你的业务逻辑映射其他字段
                //    ReportType = "lab", // 默认为检验类型
                //    ReportId = order.orderId?.ToString(),
                //    ReportName = order.citem_name,
                //    ReportTime = order.apply_time?.ToString("yyyy-MM-dd HH:mm:ss"),
                //    // 其他字段映射...
                //    DoctorId = order.placer_name,
                //    SearchType = "1" // 默认搜索类型
                //}).ToList();

                // 调用新标准服务处理调阅记录
                var result = await _newStandardService.ReportInspectionFindingAsync(requestJson, serviceMethod);
                // 直接返回接口的原始响应
                return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"处理请求失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }
        /// <summary>
        /// 生成检验条码
        /// </summary>
        /// <param name="requestJson">请求数据</param>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="patVisitType">病人来源</param>
        /// <param name="operatorId">操作员ID</param>
        /// <param name="operatorName">操作员姓名</param>
        /// <param name="outpno">门诊号</param>
        /// <param name="idno">身份证号</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("call-new-S4024/{serviceMethod}")]
        public async Task<ActionResult<object>> S4024(
            [FromBody] JsonElement requestJson,
            [FromRoute] string serviceMethod,
            [FromQuery] string patVisitType = null,
            [FromQuery] string operatorId = null,
            [FromQuery] string operatorName = null,
            [FromQuery] string outpno = null,
            [FromQuery] string idno = null,
            [FromQuery] string startDate = null,
            [FromQuery] string endDate = null)
        {
            try
            {
                // 设置默认值
                patVisitType ??= "1";
                operatorId ??= "2355";
                operatorName ??= "系统管理员";

                // 调用新标准服务生成检验条码，传递所有参数
                var result = await _newStandardService.S4024(
                    requestJson,
                    serviceMethod,
                    patVisitType,
                    operatorId,
                    operatorName,
                    outpno,
                    idno,
                    startDate,
                    endDate);
                // 返回标准成功格式
                return Ok(new
                {
                    Success = true,
                    Data = JsonSerializer.Deserialize<object>(result.GetRawText()),
                    Message = "操作成功",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                //return Ok(new
                //{
                //    ServiceMethod = serviceMethod,
                //    Success = result.code == "00000",
                //    ResultCode = result.code,
                //    ResultMessage = result.msg ?? result.message,
                //    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //    Data = result
                //});
            }
            catch (Exception ex)
            {
                // 记录错误日志
               // _logger.LogError(ex, "调用新标准服务失败: {ServiceMethod}", serviceMethod);

                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"处理请求失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }
        /// <summary>
        /// S4008操作 - 作废申请单
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="serviceMethod">服务名</param>
        /// <returns></returns>
        [HttpPost("call-new-S4008/{serviceMethod}")]
        public async Task<ActionResult<object>> S4008(
            [FromBody] S4008Request request,
            [FromRoute] string serviceMethod)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(request.PatientId))
                {
                    return BadRequest(new { Message = "患者ID(patientId)不能为空" });
                }

                if (string.IsNullOrEmpty(request.PatientVisitType))
                {
                    return BadRequest(new { Message = "患者来源(patientVisitType)不能为空" });
                }

                if (string.IsNullOrEmpty(request.ApplyId))
                {
                    return BadRequest(new { Message = "申请ID(applyId)不能为空" });
                }

                if (string.IsNullOrEmpty(request.PlacerName))
                {
                    return BadRequest(new { Message = "作废人姓名(placerName)不能为空" });
                }

                // 门诊病人需要挂号单号验证
                if (request.PatientVisitType == "1" && string.IsNullOrEmpty(request.RegistrationNo))
                {
                    return BadRequest(new { Message = "门诊病人必须提供挂号单号(registrationNo)" });
                }

                // 调用S4008服务
                var result = await _newStandardService.S4008(
                    serviceMethod,
                    request.PatientId,
                    request.PatientVisitType,
                    request.RegistrationNo,
                    request.ApplyId,
                    request.PlacerName);

                // 直接返回接口的原始响应
                return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"S4008操作失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        // S4008请求DTO
        public class S4008Request
        {
            [JsonPropertyName("patientId")]
            public string PatientId { get; set; }

            [JsonPropertyName("patientVisitType")]
            public string PatientVisitType { get; set; }

            [JsonPropertyName("registrationNo")]
            public string RegistrationNo { get; set; }

            [JsonPropertyName("applyId")]
            public string ApplyId { get; set; }

            [JsonPropertyName("placerName")]
            public string PlacerName { get; set; }
        }
        /// <summary>
        /// S4009操作
        /// </summary>
        /// <param name="requestJson">请求数据</param>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="operatorType">操作类型</param>
        /// <param name="operatorName">操作员姓名</param>
        /// <param name="organizationName">操作科室名称</param>
        /// <param name="equipmentId">仪器ID</param>
        /// <returns></returns>
        [HttpPost("call-new-S4009/{serviceMethod}")]
        public async Task<ActionResult<object>> S4009(
            [FromBody] JsonElement requestJson,
            [FromRoute] string serviceMethod,
            [FromQuery] string operatorType,
            [FromQuery] string operatorName, 
            [FromQuery] string applyId,
            [FromQuery] string organizationName = "",
            [FromQuery] string equipmentId = "")
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(operatorType))
                {
                    return BadRequest(new { Message = "操作类型(operatorType)不能为空" });
                }
                if (string.IsNullOrEmpty(operatorName))
                {
                    return BadRequest(new { Message = "操作员姓名(operatorName)不能为空" });
                }

                // 调用S4009服务
                var result = await _newStandardService.S4009(
                    requestJson,
                    serviceMethod,
                    operatorType,
                    operatorName,
                    applyId,
                    organizationName,
                    equipmentId);
                //return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
                return Ok(new
                {
                    Success = true,
                    Data = JsonSerializer.Deserialize<object>(result.GetRawText()),
                    Message = "操作成功",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                //return Ok(new
                //{
                //    ServiceMethod = serviceMethod,
                //    Success = result.code == "00000",
                //    ResultCode = result.code,
                //    ResultMessage = result.msg ?? result.message,
                //    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //    Data = result
                //});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"S4009操作失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }/// <summary>
         /// S4009二号接口操作
         /// </summary>
         /// <param name="request">请求数据</param>
         /// <param name="serviceMethod">服务名</param>
         /// <returns></returns>
        [HttpPost("call-new-S4009-v2/{serviceMethod}")]
        public async Task<ActionResult<object>> S4009V2(
            [FromBody] S4009V2Request request,
            [FromRoute] string serviceMethod)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(request.ApplyId))
                {
                    return BadRequest(new { Message = "申请单ID(applyId)不能为空" });
                }

                if (string.IsNullOrEmpty(request.OperatorType))
                {
                    return BadRequest(new { Message = "操作类型(operatorType)不能为空" });
                }

                if (string.IsNullOrEmpty(request.SpecimenBarcodeNo))
                {
                    return BadRequest(new { Message = "样本条码(specimenBarcodeNo)不能为空" });
                }

                if (string.IsNullOrEmpty(request.OperatorName))
                {
                    return BadRequest(new { Message = "操作员姓名(operatorName)不能为空" });
                }

                // 调用S4009二号接口服务
                var result = await _newStandardService.S4009V2(
                    serviceMethod,
                    request.ApplyId,
                    request.OperatorType,
                    request.SpecimenBarcodeNo,
                    request.OperatorName,
                    request.OrganizationName,
                    request.EquipmentId);

                // 直接返回接口的原始响应
                return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"S4009二号接口操作失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        // S4009二号接口请求DTO
        public class S4009V2Request
        {
            [JsonPropertyName("applyId")]
            public string ApplyId { get; set; }

            [JsonPropertyName("operatorType")]
            public string OperatorType { get; set; }

            [JsonPropertyName("specimenBarcodeNo")]
            public string SpecimenBarcodeNo { get; set; }

            [JsonPropertyName("operatorName")]
            public string OperatorName { get; set; }

            [JsonPropertyName("organizationName")]
            public string OrganizationName { get; set; }

            [JsonPropertyName("equipmentId")]
            public string EquipmentId { get; set; }
        }
        /// <summary>
        /// S4010操作 - 检验报告上传
        /// </summary>
        /// <param name="request">包含检验报告数据和附加信息的请求</param>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="applyId">申请单ID</param>
        /// <param name="checker">审核人</param>
        /// <param name="reporter">报告人</param>
        /// <param name="equipmentId">设备ID</param>
        /// <param name="reportId">报告ID</param>
        /// <param name="doctorId">医生ID</param>
        /// <param name="patientId">病人ID</param>
        /// <returns></returns>
        [HttpPost("call-new-S4010/{serviceMethod}")]
        public async Task<ActionResult<object>> S4010(
            [FromBody] S4010Request request,
            [FromRoute] string serviceMethod,
            [FromQuery] string applyId,
            [FromQuery] string checker = "系统管理员",
            [FromQuery] string reporter = "系统管理员",
            [FromQuery] string equipmentId = "",
            [FromQuery] string reportId = "",
            [FromQuery] string doctorId = "",
            [FromQuery] string patientId = "")
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(applyId))
                {
                    return BadRequest(new { Message = "申请单ID(applyId)不能为空" });
                }

                if (request == null || request.ReportData == null || !request.ReportData.Any())
                {
                    return BadRequest(new { Message = "检验报告数据不能为空" });
                }

                // 将附加信息对象转换为JsonElement
                var additionalInfoJson = JsonSerializer.SerializeToElement(request.AdditionalInfo ?? new object());

                // 调用S4010服务
                var result = await _newStandardService.S4010(
                    request.ReportData,
                    additionalInfoJson,
                    serviceMethod,
                    applyId,
                    reportId,
                    doctorId,
                    patientId,
                    checker,
                    reporter,
                    equipmentId);
                //return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
                return Ok(new
                {
                    Success = true,
                    Data = JsonSerializer.Deserialize<object>(result.GetRawText()),
                    Message = "操作成功",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                //return Ok(new
                //{
                //    ServiceMethod = serviceMethod,
                //    Success = result.code == "00000",
                //    ResultCode = result.code,
                //    ResultMessage = result.msg ?? result.message,// 单条互认操作
                //    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //    Data = result
                //});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"S4010操作失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }
        /// <summary>
        /// S4011操作 - 样本信息查询/操作
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="serviceMethod">服务名</param>
        /// <returns></returns>
        [HttpPost("call-new-S4011/{serviceMethod}")]
        public async Task<ActionResult<object>> S4011(
            [FromBody] S4011Request request,
            [FromRoute] string serviceMethod)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(request.ApplyId))
                {
                    return BadRequest(new { Message = "申请单ID(applyId)不能为空" });
                }

                if (string.IsNullOrEmpty(request.SpecimenBarcodeNo))
                {
                    return BadRequest(new { Message = "样本条码(specimenBarcodeNo)不能为空" });
                }

                if (string.IsNullOrEmpty(request.OperatorName))
                {
                    return BadRequest(new { Message = "操作员姓名(operatorName)不能为空" });
                }

                // 调用S4011服务
                var result = await _newStandardService.S4011(
                    serviceMethod,
                    request.ApplyId,
                    request.SpecimenBarcodeNo,
                    request.OperatorName);

                // 直接返回接口的原始响应
                return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"S4011操作失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        // S4011请求DTO
        public class S4011Request
        {
            [JsonPropertyName("applyId")]
            public string ApplyId { get; set; }

            [JsonPropertyName("specimenBarcodeNo")]
            public string SpecimenBarcodeNo { get; set; }

            [JsonPropertyName("operatorName")]
            public string OperatorName { get; set; }
        }
        // S4010请求DTO
        public class S4010Request
        {
            [JsonPropertyName("reportData")]
            public List<T_testr_res_indicate_oracle> ReportData { get; set; }

            [JsonPropertyName("additionalInfo")]
            public object AdditionalInfo { get; set; }
        }
        /// <summary>
        /// S5006操作 - 检查报到状态更新
        /// </summary>
        /// <param name="requestJson">请求数据</param>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="operatorName">操作员姓名</param>
        /// <param name="applyStatus">执行状态</param>
        /// <param name="exeProcess">执行过程</param>
        /// <param name="exeRoom">执行间</param>
        /// <returns></returns>
        [HttpPost("call-new-S5006/{serviceMethod}")]
        public async Task<ActionResult<object>> S5006(
            [FromBody] JsonElement requestJson,
            [FromRoute] string serviceMethod,
            [FromQuery] string operatorName,
            [FromQuery] string applyStatus = "3",
            [FromQuery] string exeProcess = "1",
            [FromQuery] string exeRoom = "3")
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(operatorName))
                {
                    return BadRequest(new { Message = "操作员姓名(operatorName)不能为空" });
                }

                // 调用S5006服务
                var result = await _newStandardService.S5006(
                    requestJson,
                    serviceMethod,
                    operatorName,
                    applyStatus,
                    exeProcess,
                    exeRoom);

                // 直接返回接口的原始响应
                return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"S5006操作失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }
        /// <summary>
        /// S5008操作 - 检查报告上传
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="serviceMethod">服务名</param>
        /// <returns></returns>
        [HttpPost("call-new-S5008/{serviceMethod}")]
        public async Task<ActionResult<object>> S5008(
            [FromBody] S5008Request request,
            [FromRoute] string serviceMethod)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(request.ApplyId))
                {
                    return BadRequest(new { Message = "申请单ID(applyId)不能为空" });
                }

                if (string.IsNullOrEmpty(request.ReporterName))
                {
                    return BadRequest(new { Message = "报告人姓名(reporterName)不能为空" });
                }

                if (string.IsNullOrEmpty(request.CheckerName))
                {
                    return BadRequest(new { Message = "审核人姓名(checkerName)不能为空" });
                }

                if (request.ReportResults == null || !request.ReportResults.Any())
                {
                    return BadRequest(new { Message = "报告结果不能为空" });
                }

                // 调用S5008服务
                var result = await _newStandardService.S5008(
                    serviceMethod,
                    request.ApplyId,
                    request.ApplyStatus ?? "1",
                    request.ReporterName,
                    request.CheckerName,
                    request.reportId,
                    request.doctorId, 
                    request.patientId,
                    request.ReportResults,
                    request.ReportFiles);

                // 直接返回接口的原始响应
                return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"S5008操作失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }
        /// <summary>
        /// S5009操作 - 取消检查报告
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="serviceMethod">服务名</param>
        /// <returns></returns>
        [HttpPost("call-new-S5009/{serviceMethod}")]
        public async Task<ActionResult<object>> S5009(
            [FromBody] S5009Request request,
            [FromRoute] string serviceMethod)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(request.ApplyId))
                {
                    return BadRequest(new { Message = "申请单ID(applyId)不能为空" });
                }

                if (string.IsNullOrEmpty(request.OperatorName))
                {
                    return BadRequest(new { Message = "操作员姓名(operatorName)不能为空" });
                }

                // 调用S5009服务
                var result = await _newStandardService.S5009(
                    serviceMethod,
                    request.ApplyId,
                    request.OperatorName);

                // 直接返回接口的原始响应
                return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ServiceMethod = serviceMethod,
                    Success = false,
                    ResultCode = "500",
                    ResultMessage = $"S5009操作失败: {ex.Message}",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        // S5009请求DTO
        public class S5009Request
        {
            [JsonPropertyName("applyId")]
            public string ApplyId { get; set; }

            [JsonPropertyName("operatorName")]
            public string OperatorName { get; set; }
        }
        // S5008请求DTO
        public class S5008Request
        {
            [JsonPropertyName("applyId")]
            public string ApplyId { get; set; }

            [JsonPropertyName("applyStatus")]
            public string ApplyStatus { get; set; } = "1";

            [JsonPropertyName("reporterName")]
            public string ReporterName { get; set; }

            [JsonPropertyName("checkerName")]
            public string CheckerName { get; set; }

            [JsonPropertyName("reportResults")]
            public List<ReportResultItem> ReportResults { get; set; }

            [JsonPropertyName("reportFiles")]
            public List<ReportFileItem> ReportFiles { get; set; }
            
            [JsonPropertyName("reportId")]
            public string reportId { get; set; }

            [JsonPropertyName("patientId")]
            public string patientId { get; set; }

            [JsonPropertyName("doctorId")]
            public string doctorId { get; set; }
            
        }
        /// <summary>
        /// 处理报告检查结果操作
        /// </summary>
        private async Task<object> HandleReportInspectionFinding(JsonElement requestJson,string serviceMethod)
        {
            try
            {
                // 调用新标准服务
                var result = await _newStandardService.ReportInspectionFindingAsync(requestJson, serviceMethod);
                return Ok(JsonSerializer.Deserialize<object>(result.GetRawText()));
                //return new
                //{
                //    ServiceMethod = "ReportInspectionFinding",
                //    Success = result.code == "00000",
                //    ResultCode = result.code,
                //    ResultMessage = result.msg ?? result.message,
                //    RequestId = result.request_id,
                //    Timestamp = result.timestamp
                //};
            }
            catch (Exception ex)
            {
                throw new Exception($"处理报告检查结果失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 根据报告类型和ID查询报告
        /// </summary>
        /// <param name="reportType">报告类型（exam/其他）</param>
        /// <param name="reportId">报告ID</param>
        /// <returns>查询到的报告对象，如果未找到返回null</returns>
        private async Task<object> GetReportByTypeAsync(string reportType, string reportId)
        {
            if (reportType == "exam")
            {
                // 查询检查表
                return await _context.t_CHECK_RECs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == reportId);
            }
            else
            {
                // 默认查询检验表
                return await _context.T_TEST_RECS
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == reportId);
            }
        }

        /// <summary>
        /// 查询此人所有的报告
        /// </summary>
        /// <param name="reportType">报告类型（exam/其他）</param>
        /// <param name="patientOrgNo">注册机构的患者编号</param>
        /// <returns>查询到的报告对象，如果未找到返回null</returns>
        private async Task<object> GetReportByTypeAllAsync(string reportType, string patientOrgNo)
        {
            if (reportType == "exam")
            {
                // 查询检查表
                return await _context.t_CHECK_RECs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PatientOrgNo == patientOrgNo);
            }
            else
            {
                // 默认查询检验表
                return await _context.T_TEST_RECS
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PatientOrgNo == patientOrgNo);
            }
        }
        // 封装的异步日志方法（解决并发写入问题）
        private async Task LogToFileAsync(string path, string message)
        {
            try
            {
                await using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                await using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteLineAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志写入失败: {ex.Message}\n原日志内容: {message}");
            }
        }
        /// <summary>
        /// 连接测试
        /// </summary>
        /// <returns></returns>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("OracleConnection");

                await using (var connection = new OracleConnection(connectionString))
                {
                    await connection.OpenAsync(); // 尝试打开连接
                    await connection.CloseAsync();
                    return Ok("Oracle 数据库连接成功！");
                }
            }
            catch (OracleException ex)
            {
                return BadRequest($"Oracle 错误: {ex.Message} (Error Code: {ex.ErrorCode})");
            }
            catch (Exception ex)
            {
                return BadRequest($"连接失败: {ex.Message}");
            }
        }
        // 封装的日志方法（解决文件占用问题）
        private void LogToFile(string path, string message)
        {
            try
            {
                // 使用FileStream解决并发写入问题
                using (var stream = new FileStream(
                    path,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                // 日志失败时至少输出到控制台
                Console.WriteLine($"日志写入失败: {ex.Message}\n原日志内容: {message}");
            }
        }
    }
    public class RecognitionRequest
    {
        public string? PatientId { get; set; }
        public string? GHId { get; set; }
        public string? ReportId { get; set; }
        public int recognise { get; set; }
        public string? ReportType { get; set; }
        public string? czlxValue { get; set; }
        public string? reasonCode { get; set; }
        public string? reasonName { get; set; }
        public string? diagName { get; set; }
        public string? DoctorId { get; set; }

    }

    public class RecognitionResponse
    {
        public string? request_id { get; set; }
        public string? code { get; set; }
        public string? msg { get; set; }
        public string? request_org { get; set; }
        public string? timestamp { get; set; }
        public string? ciphertext { get; set; }
        public string? sign { get; set; }
        public string? message { get; set; }
    }
    public class ReminderRecordRequest
    {
        public string? PatientId { get; set; }
        public string? GHId { get; set; }
        public string? ReportId { get; set; }
        public string? ReportType { get; set; }
        public string? OrgCode { get; set; }
        public string? OrgName { get; set; }
        public string? ReportName { get; set; }
        public string? ReportTime { get; set; }
        public string? SearchType { get; set; }
        public string? ReportNo { get; set; }
        public string? dataCode { get; set; }//检验项目大项代码
        public string? projCategoryName { get; set; }//检验项目大项名称
        public string? checkProjNameExp { get; set; }//医院检查项目名称
        public string? reasonCode { get; set; }
        public string? reasonName { get; set; }
        public string? diagName { get; set; }
        public string? idCardTypeCode { get; set; }
        public string? message { get; set; } 
        public string? msg { get; set; }
        public string? code { get; set; }
        public string? request_id { get; set; }
        public string? doctorId { get; set; }
        public string? OrderId { get; set; }
        public string? BusinessNo { get; set; }
        public string? CitemId { get; set; }//his项目ID
        public string? MicrobiaMark { get; set; }

    }

    public class ViewRecordRequest
    {
        public string? PatientId { get; set; }
        public string? GHId { get; set; }
        public string? ReportId { get; set; }
        public string? ReportType { get; set; }
        public string? OrgCode { get; set; }
        public string? OrgName { get; set; }
        public string? ReportName { get; set; }
        public string? ReportTime { get; set; }
        public string? ViewTime { get; set; }
        public string? SearchType { get; set; }
        public string? ReportNo { get; set; }
        public string? dataCode { get; set; }
        public string? idCardTypeCode { get; set; }
        public string? businessNo { get; set; }
        public string? DoctorId { get; set; }
        public string? OrderId { get; set; }  

    }

    // PI 响应模型
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
    }
    //3112医嘱发送模型
    public class OrderInfoRequest
    {
        public long? OrderId { get; set; }
        public string DataCode { get; set; }
        public long? OrderID { get; set; }
        public int PatientSource { get; set; }
        public long? PatientId { get; set; }
        public string HomePageId { get; set; }
        public string OperationType { get; set; }
        public string TreatmentCategory { get; set; }
        public int Sno { get; set; }
        public int OrderExpidateType { get; set; }
        public DateTime? ApplyTime { get; set; }
        public string PlacerName { get; set; }
        public int apply_dept_id { get; set; }
        [JsonConverter(typeof(FlexibleIntConverter))]
        public string citem_id { get; set; }
        public string citem_name { get; set; }
        public int exedept_id { get; set; }
        public int emg_sign { get; set; }
        public string order_drask { get; set; }
        public int fee_source { get; set; }
        public string citem_type { get; set; }
        public string parts_name { get; set; }
        public string rmethod_name { get; set; }
        public string lspcm_name { get; set; }
        public int CitemIdCj { get; set; }
        public string CitemNameCj { get; set; }
        public int ExedeptIdCj { get; set; } 
        public int RgstNo { get; set; }
    }
    //// S4010请求DTO
    //public class S4010Request
    //{
    //    [JsonPropertyName("reportData")]
    //    public List<LabReportItem> ReportData { get; set; }

    //    [JsonPropertyName("additionalInfo")]
    //    public object AdditionalInfo { get; set; }
    //}

    //// 检验报告项DTO
    //public class LabReportItem
    //{
    //    [JsonPropertyName("testProjNameExp")]
    //    public string TestProjectName { get; set; }

    //    [JsonPropertyName("testProjCodeExp")]
    //    public string TestProjectCode { get; set; }

    //    [JsonPropertyName("normalRefLimit")]
    //    public string NormalReferenceLimit { get; set; }

    //    [JsonPropertyName("testIndexUnit")]
    //    public string TestIndexUnit { get; set; }

    //    [JsonPropertyName("anomalyName")]
    //    public string AnomalyName { get; set; }

    //    [JsonPropertyName("testIndexResult")]
    //    public string TestIndexResult { get; set; }
    //}

}
