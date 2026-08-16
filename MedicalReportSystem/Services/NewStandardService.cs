using com.sun.xml.@internal.bind.v2.schemagen.xmlschema;
using MedicalReportSystem.Controllers;
using MedicalReportSystem.Models;
using System.Text;
using System.Text.Json;
using static com.sun.tools.javac.main.Option;

namespace MedicalReportSystem.Services
{
    public class NewStandardService:INewStandardService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly PersonService _personService;

        public NewStandardService(HttpClient httpClient, IConfiguration configuration, IWebHostEnvironment env, PersonService personService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _env = env;
            _personService = personService;
        }
        public async Task<RecognitionResponse> ReportInspectionFinding(List<ViewRecordRequest> requests)
        {
            var apiUrl = _configuration["ThirdPartyApi:NewStandardService"];

            // 准备请求数据
            var requestData = requests.Select(request => new
            {
                patientId = request.PatientId,
                GHId = request.GHId,
                itemType = request.ReportType == "exam" ? "PACS" : "LIS",
                itemCode = request.dataCode,
                reportOrgCode = request.OrgCode,
                reportOrgName = request.OrgName,
                reportNo = request.ReportId,
                reportName = request.ReportName,
                reportTime = request.ReportTime,
                operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                idCardTypeCode = request.idCardTypeCode,
                SearchType = request.SearchType,
                businessNo = request.businessNo,
                reviewId = Guid.NewGuid().ToString("N"),
                crossFlag = "0",
                DoctorId = request.DoctorId,
                OrderId = request.OrderId
            }).ToList();

            var response = await _httpClient.PostAsJsonAsync(apiUrl, requestData);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"调用三方接口失败: {response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<RecognitionResponse>();
        }
        public async Task<ReminderRecordRequest> SubmitVerificationResult(List<ViewRecordRequest> requests)
        {
            var apiUrl = _configuration["ThirdPartyApi:NewStandardService"];

            // 准备请求数据
            var requestData = requests.Select(request => new
            {
                patientId = request.PatientId,
                GHId = request.GHId,
                itemType = request.ReportType == "exam" ? "PACS" : "LIS",
                itemCode = request.dataCode,
                reportOrgCode = request.OrgCode,
                reportOrgName = request.OrgName,
                reportNo = request.ReportId,
                reportName = request.ReportName,
                reportTime = request.ReportTime,
                operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                idCardTypeCode = request.idCardTypeCode,
                SearchType = request.SearchType,
                businessNo = request.businessNo,
                reviewId = Guid.NewGuid().ToString("N"),
                crossFlag = "0",
                DoctorId = request.DoctorId,
                OrderId = request.OrderId
            }).ToList();

            var response = await _httpClient.PostAsJsonAsync(apiUrl, requestData);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"调用三方接口失败: {response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<ReminderRecordRequest>();
        }

        /// <summary>
        /// 发送检验,检查医嘱 - 调用新标准接口
        /// </summary>
        public async Task<JsonElement> ReportInspectionFindingAsync(JsonElement requestJson, string serviceMethod)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S3112医嘱发送");
            var logPath = Path.Combine(logDir, $"inspection_finding_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理报告检查结果");
                //await LogToFileAsync(logPath, $"请求参数: {JsonSerializer.Serialize(requestJson, new JsonSerializerOptions { WriteIndented = true })}");
                await LogToFileAsync(logPath, $"请求参数: {requestJson.GetRawText()} ");

                // 1. 构建固定格式的请求数据
                var inspectionRequest = await BuildInspectionFindingRequest(requestJson, serviceMethod);

                // 序列化选项
                //var jsonOptions = new JsonSerializerOptions
                //{
                //    WriteIndented = true,
                //    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 关键：不转义中文字符
                //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // 可选：保持属性命名风格
                //};
                await LogToFileAsync(logPath, $"构建的检查结果请求: {JsonSerializer.Serialize(inspectionRequest, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(inspectionRequest);
                await LogToFileAsync(logPath, $"三方接口响应: {JsonSerializer.Serialize(result)}");

                // 3. 处理响应并保存记录
               // await ProcessInspectionFindingResponse(result, inspectionRequest);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"报告检查结果上传失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// S3112
        /// </summary>
        /// <param name="serviceMethod"></param>
        /// <param name="patientId"></param>
        /// <param name="patientVisitType"></param>
        /// <param name="homePageId"></param>
        /// <param name="sequenceNo"></param>
        /// <param name="placerName"></param>
        /// <param name="applyDeptId"></param>
        /// <param name="executeDeptId"></param>
        /// <param name="itemType"></param>
        /// <param name="itemId"></param>
        /// <param name="itemName"></param>
        /// <param name="itemExecuteDeptId"></param>
        /// <param name="partsName"></param>
        /// <param name="methodName"></param>
        /// <param name="registrationNo"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<JsonElement> S3112V2(
    string serviceMethod,
    string patientId,
    string patientVisitType,
    string homePageId,
    string sequenceNo,
    string placerName,
    string applyDeptId,
    string executeDeptId,
    string itemType,
    string itemId,
    string itemName,
    string itemExecuteDeptId,
    string partsName,
    string methodName,
    string registrationNo = "")
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S3112");
            var logPath = Path.Combine(logDir, $"s3112_v2_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始S3112二号接口操作");
                await LogToFileAsync(logPath, $"患者ID: {patientId}");
                await LogToFileAsync(logPath, $"患者来源: {patientVisitType}");
                await LogToFileAsync(logPath, $"主页ID: {homePageId}");
                await LogToFileAsync(logPath, $"序号: {sequenceNo}");
                await LogToFileAsync(logPath, $"开单人姓名: {placerName}");
                await LogToFileAsync(logPath, $"开单科室ID: {applyDeptId}");
                await LogToFileAsync(logPath, $"执行科室ID: {executeDeptId}");
                await LogToFileAsync(logPath, $"项目类型: {itemType}");
                await LogToFileAsync(logPath, $"项目ID: {itemId}");
                await LogToFileAsync(logPath, $"项目名称: {itemName}");
                await LogToFileAsync(logPath, $"项目执行科室ID: {itemExecuteDeptId}");
                await LogToFileAsync(logPath, $"检查部位名称: {partsName}");
                await LogToFileAsync(logPath, $"检查方法名称: {methodName}");
                await LogToFileAsync(logPath, $"挂号单号: {registrationNo}");

                // 1. 构建固定格式的请求数据
                var s3112Request = await S3112V2JsonRequest(
                    serviceMethod,
                    patientId,
                    patientVisitType,
                    homePageId,
                    sequenceNo,
                    placerName,
                    applyDeptId,
                    executeDeptId,
                    itemType,
                    itemId,
                    itemName,
                    itemExecuteDeptId,
                    partsName,
                    methodName,
                    registrationNo);

                await LogToFileAsync(logPath, $"构建的S3112二号接口请求: {JsonSerializer.Serialize(s3112Request, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(s3112Request);
                await LogToFileAsync(logPath, $"三方接口响应: {result.GetRawText()}");

                // 3. 处理响应并保存记录（根据业务需求实现）
                // await ProcessS3112V2Response(result, s3112Request);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"S3112二号接口操作失败: {ex.Message}", ex);
            }
        }
        public async Task<JsonElement> S4024(JsonElement requestJson,string serviceMethod, string patVisitType = "1", string operatorId = "", string operatorName = "", string outpno = "", string idno = "", string startDate = "", string endDate = "")
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4024生成检验条码");
            var logPath = Path.Combine(logDir, $"inspection_finding_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始生成检验条码");
                await LogToFileAsync(logPath, $"请求参数: {requestJson.GetRawText()} ");

                // 1. 构建固定格式的请求数据
                var inspectionRequest = await S4024JsonRequest(requestJson, serviceMethod, patVisitType, operatorId, operatorName);
                var requestJsonString = JsonSerializer.Serialize(inspectionRequest, JsonOptions.ChineseFriendly);   
                await LogToFileAsync(logPath, $"构建的检查结果请求: {inspectionRequest}");
                await LogToFileAsync(logPath, $"构建的检查结果请求: {JsonSerializer.Serialize(inspectionRequest, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口（自动推断泛型类型为 InspectionFindingRequestDto）
                var result = await CallInspectionFindingApi(inspectionRequest);
                await LogToFileAsync(logPath, $"三方接口响应: {JsonSerializer.Serialize(result)}");

                // 3. 处理响应并保存记录
                //await ProcessInspectionFindingResponse(result, inspectionRequest);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"报告检查结果上传失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceMethod"></param>
        /// <param name="patientId"></param>
        /// <param name="patientVisitType"></param>
        /// <param name="registrationNo"></param>
        /// <param name="applyId"></param>
        /// <param name="placerName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<JsonElement> S4008(string serviceMethod, string patientId, string patientVisitType, string registrationNo, string applyId, string placerName)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4008");
            var logPath = Path.Combine(logDir, $"s4008_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始S4008操作 - 作废申请单");
                await LogToFileAsync(logPath, $"患者ID: {patientId}");
                await LogToFileAsync(logPath, $"患者来源: {patientVisitType}");
                await LogToFileAsync(logPath, $"挂号单号: {registrationNo}");
                await LogToFileAsync(logPath, $"申请ID: {applyId}");
                await LogToFileAsync(logPath, $"作废人姓名: {placerName}");

                // 1. 构建固定格式的请求数据
                var s4008Request = await S4008JsonRequest(serviceMethod, patientId, patientVisitType, registrationNo, applyId, placerName);

                await LogToFileAsync(logPath, $"构建的S4008请求: {JsonSerializer.Serialize(s4008Request, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(s4008Request);
                await LogToFileAsync(logPath, $"三方接口响应: {result.GetRawText()}");

                // 3. 处理响应并保存记录（根据业务需求实现）
                // await ProcessS4008Response(result, s4008Request);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"S4008操作失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// S4009检验状态回写
        /// </summary>
        /// <param name="requestJson">请求数据</param>
        /// <param name="serviceMethod"></param>
        /// <param name="operatorType">操作类型</param>
        /// <param name="operatorName">操作员姓名</param>
        /// <param name="applyId">医嘱相关ID,来自S3112出参</param>
        /// <param name="organizationName">操作科室名称</param>
        /// <param name="equipmentId">仪器DI</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<JsonElement> S4009(JsonElement requestJson, string serviceMethod, string operatorType, string operatorName,string applyId, string organizationName = "", string equipmentId = "")
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4009检验状态回写");
            var logPath = Path.Combine(logDir, $"s4009_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始S4009操作");
                await LogToFileAsync(logPath, $"请求参数: {requestJson.GetRawText()} ");
                await LogToFileAsync(logPath, $"操作类型: {operatorType}, 操作员: {operatorName}");

                // 1. 构建固定格式的请求数据
                var s4009Request = await S4009JsonRequest(requestJson, serviceMethod, operatorType, operatorName, applyId, organizationName, equipmentId);

                await LogToFileAsync(logPath, $"构建的S4009请求: {JsonSerializer.Serialize(s4009Request, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(s4009Request);
                await LogToFileAsync(logPath, $"三方接口响应: {JsonSerializer.Serialize(result)}");

                // 3. 处理响应并保存记录（根据业务需求实现）
                // await ProcessS4009Response(result, s4009Request);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"S4009操作失败: {ex.Message}", ex);
            }
        }
        public async Task<JsonElement> S4009V2(string serviceMethod, string applyId, string operatorType, string specimenBarcodeNo, string operatorName, string organizationName = "", string equipmentId = "")
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4009(回退报告)");
            var logPath = Path.Combine(logDir, $"s4009_v2_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始S4009二号接口操作");
                await LogToFileAsync(logPath, $"申请单ID: {applyId}");
                await LogToFileAsync(logPath, $"操作类型: {operatorType}");
                await LogToFileAsync(logPath, $"样本条码: {specimenBarcodeNo}");
                await LogToFileAsync(logPath, $"操作员姓名: {operatorName}");

                // 1. 构建固定格式的请求数据
                var s4009Request = await S4009V2JsonRequest(serviceMethod, applyId, operatorType, specimenBarcodeNo, operatorName, organizationName, equipmentId);

                await LogToFileAsync(logPath, $"构建的S4009二号接口请求: {JsonSerializer.Serialize(s4009Request, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(s4009Request);
                await LogToFileAsync(logPath, $"三方接口响应: {result.GetRawText()}");

                // 3. 处理响应并保存记录（根据业务需求实现）
                // await ProcessS4009V2Response(result, s4009Request);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"S4009二号接口操作失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// HIS接收检验报告信息
        /// </summary>
        /// <param name="requestJson">三方检验明细</param>
        /// <param name="additionalInfoJson">病人信息,来源S4024接口</param>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="applyId">相关ID</param>
        /// <param name="checker">审核人姓名</param>
        /// <param name="reporter">报告人姓名</param>
        /// <param name="equipmentId">仪器id</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<JsonElement> S4010(List<T_testr_res_indicate_oracle> reportData, JsonElement additionalInfoJson, string serviceMethod, string applyId, string reportId, string doctorId, string patientId, string checker = "系统管理员", string reporter = "系统管理员", string equipmentId = "系统管理员")
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4010HIS接收检验报告信息");
            var logPath = Path.Combine(logDir, $"s4010_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始S4010操作");
                await LogToFileAsync(logPath, $"申请单ID: {applyId}");
                await LogToFileAsync(logPath, $"reportData条数: {reportData?.Count ?? 0}");
                await LogToFileAsync(logPath, $"附加信息JSON: {additionalInfoJson.GetRawText()}");

                // 1. 构建固定格式的请求数据
                var s4010Request = await S4010JsonRequest(reportData, additionalInfoJson, serviceMethod, applyId, checker, reporter, equipmentId);

                await LogToFileAsync(logPath, $"构建的S4010请求: {JsonSerializer.Serialize(s4010Request, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(s4010Request);
                await LogToFileAsync(logPath, $"三方接口响应: {JsonSerializer.Serialize(result)}");

                // 3. 处理响应并保存记录
                 await ProcessS4010Response(applyId, reportId, doctorId, patientId);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"S4010操作失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="applyId">申请ID</param>
        /// <param name="specimenBarcodeNo">样本条码</param>
        /// <param name="operatorName">操作员姓名</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<JsonElement> S4011(string serviceMethod, string applyId, string specimenBarcodeNo, string operatorName)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4011");
            var logPath = Path.Combine(logDir, $"s4011_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始S4011操作");
                await LogToFileAsync(logPath, $"申请单ID: {applyId}");
                await LogToFileAsync(logPath, $"样本条码: {specimenBarcodeNo}");
                await LogToFileAsync(logPath, $"操作员姓名: {operatorName}");

                // 1. 构建固定格式的请求数据
                var s4011Request = await S4011JsonRequest(serviceMethod, applyId, specimenBarcodeNo, operatorName);

                await LogToFileAsync(logPath, $"构建的S4011请求: {JsonSerializer.Serialize(s4011Request, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(s4011Request);
                await LogToFileAsync(logPath, $"三方接口响应: {result.GetRawText()}");

                // 3. 处理响应并保存记录（根据业务需求实现）
                // await ProcessS4011Response(result, s4011Request);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"S4011操作失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 检查申请状态回写
        /// </summary>
        /// <param name="requestJson"></param>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="operatorName">操作员姓名</param>
        /// <param name="applyStatus">执行状态|3-正在执行;1-执行完成;0-未执行</param>
        /// <param name="exeProcess">	执行过程|-1-驳回；0或1-已登记；2-已报到；3-已检查；4-已报告；5-已审核；6-已完成</param>
        /// <param name="exeRoom">	执行间</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<JsonElement> S5006(JsonElement requestJson, string serviceMethod, string operatorName, string applyStatus = "3", string exeProcess = "1", string exeRoom = "3")
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S5006检查申请状态回写");
            var logPath = Path.Combine(logDir, $"s5006_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始S5006操作");
                await LogToFileAsync(logPath, $"请求参数: {requestJson.GetRawText()} ");
                await LogToFileAsync(logPath, $"操作员姓名: {operatorName}, 执行状态: {applyStatus}, 执行过程: {exeProcess}");

                // 1. 构建固定格式的请求数据
                var s5006Request = await S5006JsonRequest(requestJson, serviceMethod, operatorName, applyStatus, exeProcess, exeRoom);

                await LogToFileAsync(logPath, $"构建的S5006请求: {JsonSerializer.Serialize(s5006Request, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(s5006Request);
                await LogToFileAsync(logPath, $"三方接口响应: {result.GetRawText()}");

                // 3. 处理响应并保存记录（根据业务需求实现）
                // await ProcessS5006Response(result, s5006Request);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"S5006操作失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceMethod"></param>
        /// <param name="applyId"></param>
        /// <param name="applyStatus"></param>
        /// <param name="reporterName"></param>
        /// <param name="checkerName"></param>
        /// <param name="reportResults"></param>
        /// <param name="reportFiles"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<JsonElement> S5008(string serviceMethod, string applyId, string applyStatus, string reporterName, string checkerName, string reportId, string doctorId, string patientId, List<ReportResultItem> reportResults, List<ReportFileItem> reportFiles = null)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S5008");
            var logPath = Path.Combine(logDir, $"s5008_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始S5008操作");
                await LogToFileAsync(logPath, $"申请单ID: {applyId}");
                await LogToFileAsync(logPath, $"报告人: {reporterName}, 审核人: {checkerName}");
                await LogToFileAsync(logPath, $"报告结果项数: {reportResults?.Count ?? 0}");
                await LogToFileAsync(logPath, $"文件项数: {reportFiles?.Count ?? 0}");

                // 1. 构建固定格式的请求数据
                var s5008Request = await S5008JsonRequest(serviceMethod, applyId, applyStatus, reporterName, checkerName, reportResults, reportFiles);

                await LogToFileAsync(logPath, $"构建的S5008请求: {JsonSerializer.Serialize(s5008Request, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(s5008Request);
                await LogToFileAsync(logPath, $"三方接口响应: {result.GetRawText()}");

                // 3. 处理响应并保存记录（根据业务需求实现）
                 await ProcessS5008Response(applyId, reportId, doctorId, patientId);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"S5008操作失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// HIS接收取消检查报告信息
        /// </summary>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="applyId">医嘱相关ID</param>
        /// <param name="operatorName">审核人</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<JsonElement> S5009(string serviceMethod, string applyId, string operatorName)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S5009HIS接收取消检查报告信息");
            var logPath = Path.Combine(logDir, $"s5009_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始S5009操作 - 取消检查报告");
                await LogToFileAsync(logPath, $"申请单ID: {applyId}");
                await LogToFileAsync(logPath, $"操作员姓名: {operatorName}");

                // 1. 构建固定格式的请求数据
                var s5009Request = await S5009JsonRequest(serviceMethod, applyId, operatorName);

                await LogToFileAsync(logPath, $"构建的S5009请求: {JsonSerializer.Serialize(s5009Request, JsonOptions.ChineseFriendly)}");

                // 2. 调用三方接口
                var result = await CallInspectionFindingApi(s5009Request);
                await LogToFileAsync(logPath, $"三方接口响应: {result.GetRawText()}");

                // 3. 处理响应并保存记录（根据业务需求实现）
                // await ProcessS5009Response(result, s5009Request);

                return result;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"错误: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"S5009操作失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestJson"></param>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="patVisitType">病人来源</param>
        /// <param name="operatorId">操作员ID</param>
        /// <param name="operatorName">操作员姓名</param>
        /// <param name="outpno">门诊号</param>
        /// <param name="idno">身份证号</param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private async Task<InspectionFindingRequestDto> S4024JsonRequest(JsonElement requestJson, string serviceMethod, string patVisitType = "1", string operatorId = "2355", string operatorName = "系统管理员", string outpno = "", string idno = "", string startDate = "", string endDate = "")
        {
            try
            {
                // 从请求JSON中提取必要字段
                var input = requestJson.GetProperty("input");

                string applyId = "";

                // 从apply_info数组中提取第一个apply_id
                if (input.TryGetProperty("apply_info", out var applyInfoArray) && applyInfoArray.ValueKind == JsonValueKind.Array)
                {
                    var firstApplyInfo = applyInfoArray.EnumerateArray().FirstOrDefault();
                    if (firstApplyInfo.ValueKind != JsonValueKind.Undefined)
                    {
                        applyId = firstApplyInfo.TryGetProperty("apply_id", out var applyIdElement) ?
                                 applyIdElement.GetString() ?? "" : "";
                    }
                }

                // 如果apply_info中没有找到，尝试从bill_list中提取
                if (string.IsNullOrEmpty(applyId) && input.TryGetProperty("bill_list", out var billListArray) && billListArray.ValueKind == JsonValueKind.Array)
                {
                    var firstBill = billListArray.EnumerateArray().FirstOrDefault();
                    if (firstBill.ValueKind != JsonValueKind.Undefined)
                    {
                        applyId = firstBill.TryGetProperty("apply_id", out var applyIdElement) ?
                                 applyIdElement.GetString() ?? "" : "";
                    }
                }

                // 构建apply_info
                var applyInfo = new InspectionFindingRequestDto.ApplyInfo
                {
                    apply_id = applyId,
                    pat_visit_type = patVisitType,
                    oprtr_id = operatorId,
                    oprtr_name = operatorName,
                    outpno = outpno,
                    idno = idno,
                    start_date = startDate,
                    end_date = endDate
                };

                // 构建head
                var head = new InspectionFindingRequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var inspectionRequest = new InspectionFindingRequestDto
                {
                    input = new InspectionFindingRequestDto.InputData
                    {
                        apply_info = applyInfo,
                        head = head
                    }
                };

                return inspectionRequest;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "生成检验条码");
                var logPath = Path.Combine(logDir, $"s4024_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S4024请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"请求JSON: {requestJson.GetRawText()}");
                throw;
            }
        }
        private async Task<S4008RequestDto> S4008JsonRequest(string serviceMethod, string patientId, string patientVisitType, string registrationNo, string applyId, string placerName)
        {
            try
            {
                // 构建pv1_info
                var patientVisitInfo = new S4008RequestDto.PatientVisitInfo
                {
                    pid = patientId ?? "",
                    pat_visit_type = patientVisitType ?? "1",
                    rgst_no = registrationNo ?? ""
                };

                // 构建apply_info列表
                var applyInfoList = new List<S4008RequestDto.ApplyInfo>
        {
            new S4008RequestDto.ApplyInfo
            {
                apply_id = applyId ?? "",
                apply_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                placer_name = placerName ?? "系统管理员"
            }
        };

                // 构建head
                var head = new S4008RequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var s4008Request = new S4008RequestDto
                {
                    input = new S4008RequestDto.InputData
                    {
                        pv1_info = patientVisitInfo,
                        apply_info = applyInfoList,
                        head = head
                    }
                };

                return s4008Request;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4008");
                var logPath = Path.Combine(logDir, $"s4008_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S4008请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"患者ID: {patientId}, 患者来源: {patientVisitType}, 挂号单号: {registrationNo}, 申请ID: {applyId}, 作废人: {placerName}");
                throw;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestJson">入参</param>
        /// <param name="serviceMethod">服务名S4009</param>
        /// <param name="operatorType">	操作类型|1-条码打印;2-条码打印和采集完成;3-取消条码打印;4-取消条码打印和采集完成;5-标本签收;6-标本核收;7-标本取消核收;10-标本送检;11-标本取消送检;12-标本拒收 ，临生免仅支持2-完成采集;4-取消采集6-标本核收;7-取消核收8-报告发布9-报告取消发布;</param>
        /// <param name="operatorName">操作员姓名</param>
        /// <param name="organizationName">操作科室名称</param>
        /// <param name="equipmentId">仪器id</param>
        /// <returns></returns>
        private async Task<S4009RequestDto> S4009JsonRequest(JsonElement requestJson, string serviceMethod, string operatorType, string operatorName, string applyId, string organizationName = "", string equipmentId = "")
        {
            try
            {
                // 从请求JSON中提取必要字段
                var input = requestJson.GetProperty("input");

                var applyInfoList = new List<S4009RequestDto.ApplyInfo>();

                // 处理apply_info数组
                if (input.TryGetProperty("apply_info", out var applyInfoArray) && applyInfoArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var applyItem in applyInfoArray.EnumerateArray())
                    {
                        var applyInfo = new S4009RequestDto.ApplyInfo
                        {
                            system = "",
                            //apply_id = applyItem.TryGetProperty("apply_id", out var applyIdElement) ? applyIdElement.GetString() ?? "" : "",
                            apply_id = applyId,
                            oprtr_type = operatorType,
                            spcm_bc_no = applyItem.TryGetProperty("spcm_bc_no", out var barcodeElement) ? barcodeElement.GetString() ?? "" : "",
                            oprtr_name = operatorName,
                            organization_id = applyItem.TryGetProperty("apply_dept_id", out var deptIdElement) ? deptIdElement.GetString() ?? "" : "",
                            organization_code = "",
                            organization_name = !string.IsNullOrEmpty(_configuration["InstrumentConfig:OrganizationName"]) ? _configuration["InstrumentConfig:OrganizationName"] :
                                             applyItem.TryGetProperty("apply_dept_name", out var deptNameElement) ? deptNameElement.GetString() ?? "" : "",
                            oprtr_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            rejection_no = "",
                            rejection_content = "",
                            rejection_note = "",
                            verify_chrg = "",
                            submission = "",
                            submission_time = "",
                            eq_id = _configuration["InstrumentConfig:EquipmentId"]
                        };

                        applyInfoList.Add(applyInfo);
                    }
                }

                // 构建head
                var head = new S4009RequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var s4009Request = new S4009RequestDto
                {
                    input = new S4009RequestDto.InputData
                    {
                        apply_info = applyInfoList,
                        head = head
                    }
                };

                return s4009Request;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4009");
                var logPath = Path.Combine(logDir, $"s4009_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S4009请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"请求JSON: {requestJson.GetRawText()}");
                throw;
            }
        }
        private async Task<S4009RequestDto> S4009V2JsonRequest(string serviceMethod, string applyId, string operatorType, string specimenBarcodeNo, string operatorName, string organizationName = "", string equipmentId = "")
        {
            try
            {
                // 构建apply_info列表
                var applyInfoList = new List<S4009RequestDto.ApplyInfo>
                {
                    new S4009RequestDto.ApplyInfo
                    {
                        system = "",
                        apply_id = applyId ?? "",
                        oprtr_type = operatorType ?? "",
                        spcm_bc_no = specimenBarcodeNo ?? "",
                        oprtr_name = operatorName ?? "系统管理员",
                        organization_id = "",
                        organization_code = "",
                        organization_name = organizationName ?? "",
                        oprtr_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        rejection_no = "",
                        rejection_content = "",
                        rejection_note = "",
                        verify_chrg = "",
                        submission = "",
                        submission_time = "",
                        eq_id =  _configuration["InstrumentConfig:EquipmentId"] ?? ""
                    }
                };

                // 构建head
                var head = new S4009RequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var s4009Request = new S4009RequestDto
                {
                    input = new S4009RequestDto.InputData
                    {
                        apply_info = applyInfoList,
                        head = head
                    }
                };

                return s4009Request;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4009");
                var logPath = Path.Combine(logDir, $"s4009_v2_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S4009二号接口请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"apply_id: {applyId}, 操作类型: {operatorType}, 样本条码: {specimenBarcodeNo}, 操作员: {operatorName}");
                throw;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="applyId">申请ID</param>
        /// <param name="specimenBarcodeNo">条码</param>
        /// <param name="operatorName">	操作员姓名</param>
        /// <returns></returns>
        private async Task<S4011RequestDto> S4011JsonRequest(string serviceMethod, string applyId, string specimenBarcodeNo, string operatorName)
        {
            try
            {
                // 构建apply_info
                var applyInfo = new S4011RequestDto.ApplyInfo
                {
                    apply_id = applyId ?? "",
                    spcm_bc_no = specimenBarcodeNo ?? "",
                    oprtr_name = operatorName ?? "系统管理员",
                    oprtr_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // 构建head
                var head = new S4011RequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "LYXZ-R97.3-LYXZJT（JMBWMB）",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var s4011Request = new S4011RequestDto
                {
                    input = new S4011RequestDto.InputData
                    {
                        apply_info = applyInfo,
                        head = head
                    }
                };

                return s4011Request;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4011");
                var logPath = Path.Combine(logDir, $"s4011_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S4011请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"apply_id: {applyId}, 样本条码: {specimenBarcodeNo}, 操作员: {operatorName}");
                throw;
            }
        }
        /// <summary>
        /// HIS接收检验报告信息,组合json入参
        /// </summary>
        /// <param name="requestJson">三方检验明细</param>
        /// <param name="additionalInfoJson">病人信息,来源S4024接口</param>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="applyId">相关ID</param>
        /// <param name="checker">审核人姓名</param>
        /// <param name="reporter">报告人姓名</param>
        /// <param name="equipmentId">仪器ID</param>
        /// <returns></returns>
        private async Task<S4010RequestDto> S4010JsonRequest(List<T_testr_res_indicate_oracle> reportData, JsonElement additionalInfoJson, string serviceMethod, string applyId, string checker = "系统管理员", string reporter = "系统管理员", string equipmentId = "")
        {
            try
            {
                // 从additionalInfoJson中提取病人信息和条码信息
                string patientName = ""; // 默认值
                string specimenBarcodeNo = ""; // 默认值

                if (additionalInfoJson.ValueKind == JsonValueKind.Object)
                {
                    if (additionalInfoJson.TryGetProperty("input", out var inputElement))
                    {
                        if (inputElement.TryGetProperty("apply_info", out var applyInfoArray) &&
                            applyInfoArray.ValueKind == JsonValueKind.Array)
                        {
                            var firstApplyInfo = applyInfoArray.EnumerateArray().FirstOrDefault();
                            if (firstApplyInfo.ValueKind != JsonValueKind.Undefined)
                            {
                                // 提取病人姓名
                                patientName = firstApplyInfo.TryGetProperty("pat_name", out var nameElement) ?
                                             nameElement.GetString() ?? "" : "";

                                // 提取条码号
                                specimenBarcodeNo = firstApplyInfo.TryGetProperty("spcm_bc_no", out var barcodeElement) ?
                                                   barcodeElement.GetString() ?? "" : "";
                            }
                        }
                    }
                }

                // 解析异常标志字典
                string GetAbnormalFlag(string anomalyName)
                {
                    return anomalyName?.ToLower() switch
                    {
                        "异常偏低" => "2",
                        "异常偏高" => "3",
                        "异常" => "4",
                        "正常" => "1",
                        _ => "1" // 默认正常
                    };
                }

                var reportInfoList = new List<S4010RequestDto.ReportInfo>();
                int seq = 1;

                // 处理reportData列表，循环解析数据到rpt_info
                if (reportData != null && reportData.Any())
                {
                    foreach (var item in reportData)
                    {
                        var reportInfo = new S4010RequestDto.ReportInfo
                        {
                            apply_id = applyId,
                            loitem_cname = item.TestProjNameExp ?? "",
                            loitem_id = item.TestProjCodeExp ?? "",
                            loitem_rv = item.NormalRefLimit ?? "",
                            loitem_unit = item.TestIndexUnit ?? "",
                            decimals = "2",
                            loitem_result_type="3",
                            oaflag = GetAbnormalFlag(item.AnomalyName),
                            order_rpt_result = item.TestIndexResult ?? "",
                            seq = seq.ToString()
                        };

                        reportInfoList.Add(reportInfo);
                        seq++;
                    }
                }

                // 构建apply_info
                var applyInfo = new S4010RequestDto.ApplyInfo
                {
                    apply_id = applyId,
                    chk_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    chkin_ps = patientName,
                    chkin_time = DateTime.Now.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss"),
                    chkr = checker,
                    eq_id = _configuration["InstrumentConfig:EquipmentId"],
                    report_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    rpt_type = "0",
                    pdf_only="0",
                    rptr = reporter,
                    spcm_bc_no = specimenBarcodeNo,
                    spcm_clct_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // 构建head
                var head = new S4010RequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var s4010Request = new S4010RequestDto
                {
                    input = new S4010RequestDto.InputData
                    {
                        apply_info = applyInfo,
                        head = head,
                        rpt_info = reportInfoList
                    }
                };

                return s4010Request;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4010");
                var logPath = Path.Combine(logDir, $"s4010_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S4010请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"reportData条数: {reportData?.Count ?? 0}");
                await LogToFileAsync(logPath, $"附加信息JSON: {additionalInfoJson.GetRawText()}");
                throw;
            }
        }
        /// <summary>
        /// 构建检查结果请求数据
        /// </summary>
        private async Task<InspectionFindingRequest> BuildInspectionFindingRequest(JsonElement requestJson, string serviceMethod)
        {
            var inspectionRequest = new InspectionFindingRequest
            {
                input = new InputData
                {
                    aud_key = "",
                    client_ip = "",
                    pv1_info = new Pv1Info(),
                    apply_info = new List<ApplyInfo>(),
                    //dg1_info = new List<Dg1Info>(),
                    head  = new Heads()
                }
            };

            try
            {
                // 解析输入数据 - 根据JSON结构
                if (requestJson.TryGetProperty("input", out var inputElement))
                {
                    // 解析患者信息 - 从req_info的第一个元素获取
                    if (inputElement.TryGetProperty("req_info", out var reqInfoElement) && reqInfoElement.ValueKind == JsonValueKind.Array && reqInfoElement.GetArrayLength() > 0)
                    {
                        var firstReqInfo = reqInfoElement.EnumerateArray().First(); // 获取第一个req_info元素

                        inspectionRequest.input.pv1_info = new Pv1Info
                        {
                            pid = firstReqInfo.TryGetProperty("patientId", out var pidElement) ? pidElement.GetInt64().ToString() : "",
                            pat_visit_type = firstReqInfo.TryGetProperty("fee_source", out var visitTypeElement) ? visitTypeElement.GetInt32().ToString() : "2",
                            pvid = firstReqInfo.TryGetProperty("homePageId", out var visitIdElement) ? visitIdElement.GetString() : "",
                            rgst_no = firstReqInfo.TryGetProperty("rgst_no", out var registerNoElement) ?(registerNoElement.ValueKind == JsonValueKind.Number ?registerNoElement.GetInt64().ToString() :registerNoElement.GetString()): ""
                        };

                        // 解析申请信息 - 从req_info数组
                        var applyInfoList = reqInfoElement.Deserialize<List<ApplyInfoRequest>>();
                        if (applyInfoList != null && applyInfoList.Any())
                        {
                            foreach (var applyItem in applyInfoList)
                            {
                                var applyInfo = new ApplyInfo
                                {
                                    order_class = applyItem.citem_type == "C" ? "4" :
                                                 (applyItem.citem_type == "D" ? "5" :applyItem.order_class?.ToString())??"",
                                    sno = applyItem.sno?.ToString() ?? "1",
                                    order_expidate_type = applyItem.order_expidate_type?.ToString() ?? "1",
                                    apply_time = FormatDateTime(applyItem.apply_time) ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    order_exe_time_start = FormatDateTime(applyItem.order_exe_time_start) ?? DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss"),
                                    placer_name = applyItem.placer_name ?? "系统管理员",
                                    apply_dept_id = applyItem.apply_dept_id?.ToString() ?? "",
                                    //citem_id = applyItem.citem_id_cj switch
                                    //{
                                    //    int i => i.ToString(),
                                    //    string s => s,
                                    //    null => "",  // 显式处理 null
                                    //    _ => applyItem.citem_id_cj.ToString() ?? ""  
                                    //},
                                    citem_id = applyItem.citem_type switch
                                    {
                                        "C" => applyItem.citem_id_cj switch
                                        {
                                            int i => i.ToString(),
                                            string s => s,
                                            null => "",
                                            _ => applyItem.citem_id_cj.ToString() ?? ""
                                        },
                                        _ => applyItem.citem_id switch  // 当 citem_type 不等于 "C" 时
                                        {
                                            int i => i.ToString(),
                                            string s => s,
                                            null => "",
                                            _ => applyItem.citem_id?.ToString() ?? ""
                                        }
                                    },
                                    //citem_id = "9",
                                    citem_name = applyItem.citem_type switch
                                    {
                                        "C" => applyItem.citem_name_cj ?? "",
                                        "D" => applyItem.citem_name ?? "",
                                        _ => applyItem.lspcm_name?.ToString() ?? ""
                                    },
                                    exedept_id = applyItem.exedept_id?.ToString() ?? "",
                                    //exedept_id = applyItem.exedept_id_cj switch
                                    //{
                                    //    int i => i.ToString(),
                                    //    string s => s,
                                    //    null => "",  // 显式处理 null
                                    //    _ => applyItem.exedept_id_cj.ToString() ?? ""
                                    //},
                                    emg_sign = applyItem.emg_sign?.ToString() ?? "0",
                                    order_drask = applyItem.order_drask ?? "互认",
                                    baby_sno = applyItem.baby_sno?.ToString() ?? "",
                                    drug_freq_code = applyItem.drug_freq_code ?? "",
                                    order_once_qunt = applyItem.order_once_qunt?.ToString() ?? "",
                                    order_total_qunt = applyItem.order_total_qunt?.ToString() ?? "",
                                    decoction_method_id = applyItem.decoction_method_id ?? "",
                                    drug_freq_mth = applyItem.drug_freq_mth ?? "",
                                    drop_number = applyItem.drop_number?.ToString() ?? "",
                                    lspcm_name = applyItem.citem_type switch
                                    {
                                        "C" => applyItem.lspcm_name??"",
                                        "D" => applyItem.bodyPart ?? "",
                                        _ => applyItem.lspcm_name?.ToString()??""
                                    },
                                    fee_source = applyItem.fee_source switch
                                    {
                                        1 => "2",
                                        2 => "1",
                                        _ => applyItem.fee_source?.ToString() ?? "2"
                                    },
                                    note = applyItem.note ?? "",
                                    bedside_surcharge = applyItem.bedside_surcharge?.ToString() ?? "",
                                    exe_property = applyItem.exe_property ?? "",
                                    qunt_times = applyItem.qunt_times?.ToString() ?? "",
                                    recipe_id = applyItem.recipe_id?.ToString() ?? "",
                                    high_value_sign = applyItem.high_value_sign?.ToString() ?? "",
                                    batch = applyItem.batch ?? "",
                                    drug_item = new List<DrugItem>(),
                                    apply_item = new List<ApplyItem>(),
                                    part_info = new List<PartInfo>(),
                                    blood_info = new BloodInfo(),
                                    surg_info = new SurgInfo(),
                                    addition_item = new List<AdditionItem>()
                                };

                                // 处理申请项目 - 从原始数据映射
                                var applyItems = new List<ApplyItem>();
                                //if (applyInfo.drug_item != null)
                                //{
                                    applyInfo.drug_item = new List<DrugItem>
                                    {
                                        new DrugItem
                                        {
                                            fitem_id = "",
                                            fitem_name = "",
                                            dspry_id = "",
                                            order_once_qunt = "",
                                            order_total_qunt = "",
                                            drug_aim = "",
                                            drug_reason = "",
                                            drop_number = "",
                                            order_drask = "",
                                            foot = "",
                                            decoction = "",
                                        }
                                    };
                                //}
                                // 添加检验主项目
                                if (!string.IsNullOrEmpty(applyItem.citem_id?.ToString()) && !string.IsNullOrEmpty(applyItem.citem_name) && applyItem.citem_type=="C")
                                {
                                    applyItems.Add(new ApplyItem
                                    {
                                        citem_type = applyItem.citem_type ?? "C",
                                        citem_id = applyItem.citem_id?.ToString() ?? "",
                                        citem_name = applyItem.citem_name,
                                        exedept_id = applyItem.exedept_id?.ToString() ?? ""
                                    });
                                }

                                // 添加采集项目（如果有）
                                //if (!string.IsNullOrEmpty(applyItem.citem_id_cj?.ToString()) && !string.IsNullOrEmpty(applyItem.citem_name_cj))
                                //{
                                //    applyItems.Add(new ApplyItem
                                //    {
                                //        citem_type = "C", // 采集项目类型
                                //        citem_id = applyItem.citem_id_cj?.ToString() ?? "",
                                //        citem_name = applyItem.citem_name_cj,
                                //        exedept_id = applyItem.exedept_id_cj?.ToString() ?? ""
                                //    });
                                //}
                                applyInfo.addition_item = new List<AdditionItem>
                                {
                                    new AdditionItem
                                    {
                                        item_title = "",
                                        item_value = "",
                                        element_id = "",
                                        required = ""
                                    }
                                };
                                applyInfo.apply_item = applyItems;

                                // 处理部位信息
                                if (!string.IsNullOrEmpty(applyItem.bodyPart) && !string.IsNullOrEmpty(applyItem.method) && applyItem.citem_type == "D")
                                {
                                    applyInfo.part_info = new List<PartInfo>
                                    {
                                        new PartInfo
                                        {
                                            //parts_name = applyItem.bodyPart,
                                            //rmethod_name = applyItem.method ?? ""
                                             parts_name = "",
                                            rmethod_name = ""
                                        }
                                    };
                                }

                                inspectionRequest.input.apply_info.Add(applyInfo);
                            }
                        }
                    }
                }

                // 如果没有诊断信息，添加一个空的诊断信息
                //if (inspectionRequest.input.dg1_info == null || !inspectionRequest.input.dg1_info.Any())
                //{
                //    inspectionRequest.input.dg1_info = new List<Dg1Info>
                //    {
                //        new Dg1Info
                //        {
                //            dz_sno = "1",
                //            dz_type = "",
                //            csd_code = "",
                //            dz_content = "",
                //            symptom_code = "",
                //            symptom_name = ""
                //        }
                //    };
                //}
                // 构建 head 节点
                inspectionRequest.input.head = new Heads
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString("N")
                };

                return inspectionRequest;
            }
            catch (Exception ex)
            {
                throw new Exception($"构建检查结果请求数据失败: {ex.Message}", ex);
            }
        }
        private async Task<S3112RequestDto> S3112V2JsonRequest(
            string serviceMethod, 
            string patientId, 
            string patientVisitType, 
            string homePageId, 
            string sequenceNo, 
            string placerName, 
            string applyDeptId, 
            string executeDeptId, 
            string itemType, 
            string itemId, 
            string itemName, 
            string itemExecuteDeptId, 
            string partsName, 
            string methodName,
            string registrationNo = "")
        {
            try
            {
                // 构建pv1_info
                var patientVisitInfo = new S3112RequestDto.PatientVisitInfo
                {
                    pid = patientId ?? "",
                    pat_visit_type = patientVisitType ?? "1",
                    pvid = homePageId ?? "",
                    rgst_no = registrationNo ?? ""
                };

                // 构建apply_info
                var applyInfoList = new List<S3112RequestDto.ApplyInfo>
                {
                    new S3112RequestDto.ApplyInfo
                    {
                        order_class = "5", // 固定值
                        sno = sequenceNo ?? "1",
                        order_expidate_type = "1", // 固定值
                        apply_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        order_exe_time_start = DateTime.Now.AddMinutes(2).ToString("yyyy-MM-dd HH:mm:ss"), // 示例：2分钟后开始执行
                        placer_name = placerName ?? "系统管理员",
                        apply_dept_id = applyDeptId ?? "",
                        citem_id = itemId ?? "",
                        citem_name = itemName ?? "",
                        exedept_id = executeDeptId ?? "",
                        emg_sign = "0", // 固定值，非急诊
                        order_drask = "嘱托",
                        baby_sno = "",
                        drug_freq_code = "",
                        order_once_qunt = "",
                        order_total_qunt = "",
                        decoction_method_id = "",
                        drug_freq_mth = "",
                        drop_number = "",
                        lspcm_name = partsName ?? "", // 使用检查部位名称
                        fee_source = "1", // 固定值
                        note = "",
                        bedside_surcharge = "",
                        exe_property = "",
                        qunt_times = "",
                        recipe_id = "",
                        high_value_sign = "",
                        batch = "",
                        drug_item = new List<S3112RequestDto.DrugItem>(), // 空数组
                        apply_item = new List<S3112RequestDto.ApplyItem>
                        {
                            new S3112RequestDto.ApplyItem
                            {
                                citem_type = itemType ?? "",
                                citem_id = itemId ?? "",
                                citem_name = itemName ?? "",
                                exedept_id = itemExecuteDeptId ?? ""
                            }
                        },
                        part_info = new List<S3112RequestDto.PartInfo>
                        {
                            new S3112RequestDto.PartInfo
                            {
                                parts_name = partsName ?? "",
                                rmethod_name = methodName ?? ""
                            }
                        },
                        blood_info = new S3112RequestDto.BloodInfo
                        {
                            order_type = "",
                            lscmtd_id = "",
                            lscmtd_name = "",
                            exedept_id = "",
                            abo_blood = "",
                            rh_blood = ""
                        },
                        surg_info = new S3112RequestDto.SurgInfo
                        {
                            surg_type = "",
                            aneitem_id = "",
                            aneitem_name = "",
                            exedept_id = ""
                        },
                        addition_item = new List<S3112RequestDto.AdditionItem>() // 空数组
                    }
                };

                // 构建head
                var head = new S3112RequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var s3112Request = new S3112RequestDto
                {
                    input = new S3112RequestDto.InputData
                    {
                        aud_key = "",
                        client_ip = "",
                        pv1_info = patientVisitInfo,
                        apply_info = applyInfoList,
                        head = head
                    }
                };

                return s3112Request;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S3112");
                var logPath = Path.Combine(logDir, $"s3112_v2_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S3112二号接口请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"患者ID: {patientId}, 患者来源: {patientVisitType}, 主页ID: {homePageId}, 序号: {sequenceNo}");
                await LogToFileAsync(logPath, $"开单人: {placerName}, 开单科室: {applyDeptId}, 执行科室: {executeDeptId}");
                await LogToFileAsync(logPath, $"项目ID: {itemId}, 项目名称: {itemName}, 执行科室: {itemExecuteDeptId}");
                await LogToFileAsync(logPath, $"检查部位: {partsName}, 检查方法: {methodName}");
                throw;
            }
        }
        /// <summary>
        /// 检查申请状态回写
        /// </summary>
        /// <param name="requestJson"></param>
        /// <param name="serviceMethod">服务名</param>
        /// <param name="operatorName">操作员姓名</param>
        /// <param name="applyStatus">执行状态|3-正在执行;1-执行完成;0-未执行</param>
        /// <param name="exeProcess">	执行过程|-1-驳回；0或1-已登记；2-已报到；3-已检查；4-已报告；5-已审核；6-已完成</param>
        /// <param name="exeRoom">	执行间</param>
        /// <returns></returns>
        private async Task<S5006RequestDto> S5006JsonRequest(JsonElement requestJson, string serviceMethod, string operatorName, string applyStatus = "3", string exeProcess = "1", string exeRoom = "3")
        {
            try
            {
                // 从请求JSON中提取apply_id
                var applyIdList = new List<string>();

                if (requestJson.TryGetProperty("input", out var inputElement))
                {
                    // 从apply_info数组中提取apply_id
                    if (inputElement.TryGetProperty("apply_info", out var applyInfoArray) &&
                        applyInfoArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var applyItem in applyInfoArray.EnumerateArray())
                        {
                            if (applyItem.TryGetProperty("apply_id", out var applyIdElement))
                            {
                                var applyId = applyIdElement.GetString();
                                if (!string.IsNullOrEmpty(applyId))
                                {
                                    applyIdList.Add(applyId);
                                }
                            }
                        }
                    }

                    // 如果apply_info中没有找到，尝试从bill_list中提取
                    if (!applyIdList.Any() && inputElement.TryGetProperty("bill_list", out var billListArray) &&
                        billListArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var billItem in billListArray.EnumerateArray())
                        {
                            if (billItem.TryGetProperty("apply_id", out var applyIdElement))
                            {
                                var applyId = applyIdElement.GetString();
                                if (!string.IsNullOrEmpty(applyId))
                                {
                                    applyIdList.Add(applyId);
                                }
                            }
                        }
                    }
                }

                // 如果没有找到apply_id，使用默认值
                if (!applyIdList.Any())
                {
                    applyIdList.Add("");
                }

                // 构建apply_info列表
                var applyInfoList = new List<S5006RequestDto.ApplyInfo>();

                foreach (var applyId in applyIdList.Distinct())
                {
                    var applyInfo = new S5006RequestDto.ApplyInfo
                    {
                        apply_id = applyId,
                        apply_status = applyStatus,
                        verify_chrg = "",
                        chk_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        chkin_ps = operatorName,
                        exe_process = exeProcess,
                        exe_room = exeRoom,
                        send_no = ""
                    };

                    applyInfoList.Add(applyInfo);
                }

                // 构建head
                var head = new S5006RequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var s5006Request = new S5006RequestDto
                {
                    input = new S5006RequestDto.InputData
                    {
                        apply_info = applyInfoList,
                        head = head
                    }
                };

                return s5006Request;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S5006");
                var logPath = Path.Combine(logDir, $"s5006_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S5006请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"请求JSON: {requestJson.GetRawText()}");
                throw;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceMethod"></param>
        /// <param name="applyId"></param>
        /// <param name="applyStatus"></param>
        /// <param name="reporterName"></param>
        /// <param name="checkerName"></param>
        /// <param name="reportResults"></param>
        /// <param name="reportFiles"></param>
        /// <returns></returns>
        private async Task<S5008RequestDto> S5008JsonRequest(string serviceMethod, string applyId, string applyStatus, string reporterName, string checkerName, List<ReportResultItem> reportResults, List<ReportFileItem> reportFiles = null)
        {
            try
            {
                // 构建apply_info
                var applyInfoList = new List<S5008RequestDto.ApplyInfo>
        {
            new S5008RequestDto.ApplyInfo
            {
                apply_id = applyId,
                apply_status = applyStatus,
                rpt_ps = reporterName,
                chkin_ps = checkerName,
                rpt_path = "",
                oaflag = "",
                rpt_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                chkin_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                check_no = applyId // 使用apply_id作为检查号，可根据业务需求调整
            }
        };

                // 构建rpt_info - 将报告结果分类转换为数组
                var reportInfoList = new List<S5008RequestDto.ReportInfo>();

                if (reportResults != null && reportResults.Any())
                {
                    foreach (var result in reportResults)
                    {
                        // 验证结果类别是否符合要求
                        if (IsValidCategory(result.Category))
                        {
                            var reportInfo = new S5008RequestDto.ReportInfo
                            {
                                title_type = "1",
                                loitem_cname = result.Category,
                                order_rpt_result = result.Content ?? ""
                            };
                            reportInfoList.Add(reportInfo);
                        }
                    }
                }

                // 构建rpt_file - 文件结果数组
                var reportFileList = new List<S5008RequestDto.ReportFile>();

                if (reportFiles != null && reportFiles.Any())
                {
                    foreach (var file in reportFiles)
                    {
                        var reportFile = new S5008RequestDto.ReportFile
                        {
                            file_format = file.FileFormat ?? "",
                            file_name = file.FileName ?? "",
                            file_content = file.FileContent ?? "",
                            note = file.Note ?? ""
                        };
                        reportFileList.Add(reportFile);
                    }
                }
                else
                {
                    // 如果没有文件，传空数组
                    reportFileList = new List<S5008RequestDto.ReportFile>();
                }

                // 构建head
                var head = new S5008RequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var s5008Request = new S5008RequestDto
                {
                    input = new S5008RequestDto.InputData
                    {
                        apply_info = applyInfoList,
                        rpt_info = reportInfoList,
                        rpt_file = reportFileList,
                        head = head
                    }
                };

                return s5008Request;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S5008");
                var logPath = Path.Combine(logDir, $"s5008_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S5008请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"apply_id: {applyId}, 报告人: {reporterName}, 审核人: {checkerName}");
                throw;
            }
        }
        private async Task<S5009RequestDto> S5009JsonRequest(string serviceMethod, string applyId, string operatorName)
        {
            try
            {
                // 构建apply_info
                var applyInfoList = new List<S5009RequestDto.ApplyInfo>
        {
            new S5009RequestDto.ApplyInfo
            {
                apply_id = applyId,
                chkin_ps = operatorName
            }
        };

                // 构建head
                var head = new S5009RequestDto.Head
                {
                    bizno = serviceMethod,
                    sysno = _configuration["NewStandardConfig:sysno"] ?? "FGN-FGN",
                    tarno = _configuration["NewStandardConfig:tarno"] ?? "ZLHIS",
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action_no = Guid.NewGuid().ToString()
                };

                // 构建完整的请求结构
                var s5009Request = new S5009RequestDto
                {
                    input = new S5009RequestDto.InputData
                    {
                        apply_info = applyInfoList,
                        head = head
                    }
                };

                return s5009Request;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S5009");
                var logPath = Path.Combine(logDir, $"s5009_json_request_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 构建S5009请求失败: {ex.Message}");
                await LogToFileAsync(logPath, $"apply_id: {applyId}, 操作员: {operatorName}");
                throw;
            }
        }
        // 验证结果类别是否有效
        private bool IsValidCategory(string category)
        {
            var validCategories = new List<string> { "检查所见", "诊断意见", "诊断建议" };
            return validCategories.Contains(category);
        }
        /// <summary>
        /// 格式化日期时间
        /// </summary>
        private string FormatDateTime(DateTime? dateTime)
        {
            return dateTime?.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 格式化日期时间（字符串输入）
        /// </summary>
        private string FormatDateTime(string dateTimeStr)
        {
            if (DateTime.TryParse(dateTimeStr, out DateTime dateTime))
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return null;
        }
        /// <summary>
        /// 调用检验结果三方接口
        /// </summary>
        //private async Task<RecognitionResponse> CallInspectionFindingApi(InspectionFindingRequest request)
        //{
        //    try
        //    {
        //        var apiUrl = _configuration["ThirdPartyApi:NewStandardService"]; // 在配置中设置接口地址
        //        if (string.IsNullOrEmpty(apiUrl))
        //        {
        //            throw new ArgumentException("ThirdPartyApi:NewStandardService 配置缺失");
        //        }

        //        var client = new HttpClient();
        //        var response = await client.PostAsJsonAsync(apiUrl, request);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var errorContent = await response.Content.ReadAsStringAsync();
        //            throw new HttpRequestException($"调用检验结果接口失败: {response.StatusCode}, 错误信息: {errorContent}");
        //        }

        //        return await response.Content.ReadFromJsonAsync<RecognitionResponse>();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"调用检查结果API失败: {ex.Message}", ex);
        //    }
        //}
        /// <summary>
        /// 调用检验结果三方接口（泛型版本，支持多种请求类型）
        /// </summary>
        //private async Task<RecognitionResponse> CallInspectionFindingApi<TRequest>(TRequest request)
        //{
        //    try
        //    {
        //        var apiUrl = _configuration["ThirdPartyApi:NewStandardService"];
        //        if (string.IsNullOrEmpty(apiUrl))
        //        {
        //            throw new ArgumentException("ThirdPartyApi:NewStandardService 配置缺失");
        //        }

        //        using var client = new HttpClient();
        //        var response = await client.PostAsJsonAsync(apiUrl, request);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var errorContent = await response.Content.ReadAsStringAsync();
        //            throw new HttpRequestException($"调用检验结果接口失败: {response.StatusCode}, 错误信息: {errorContent}");
        //        }

        //        return await response.Content.ReadFromJsonAsync<RecognitionResponse>();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"调用检查结果API失败: {ex.Message}", ex);
        //    }
        //}
        /// <summary>
        /// 调用检验结果三方接口（泛型版本，支持多种请求类型）
        /// </summary>
        private async Task<JsonElement> CallInspectionFindingApi<TRequest>(TRequest request)
        {
            try
            {
                // 添加调试信息
                //Console.WriteLine($"请求类型: {typeof(TRequest).Name}");
                //Console.WriteLine($"请求对象: {JsonSerializer.Serialize(request, JsonOptions.ChineseFriendly)}");
                var apiUrl = _configuration["ThirdPartyApi:NewStandardService"];
                if (string.IsNullOrEmpty(apiUrl))
                {
                    throw new ArgumentException("ThirdPartyApi:NewStandardService 配置缺失");
                }

                using var client = new HttpClient();
                // 手动序列化请求，使用您的自定义配置
                //var jsonString = JsonSerializer.Serialize(request, JsonOptions.ChineseFriendly);
                //var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                // 发送请求并获取响应
                var response = await client.PostAsJsonAsync(apiUrl, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"调用检验结果接口失败: {response.StatusCode}, 错误信息: {errorContent}");
                }

                // 读取响应内容为字符串
                var responseContent = await response.Content.ReadAsStringAsync();

                // 将响应内容解析为JsonElement
                using var document = JsonDocument.Parse(responseContent);
                return document.RootElement.Clone();
            }
            catch (Exception ex)
            {
                throw new Exception($"调用检查结果API失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 处理检查结果响应
        /// </summary>
        private async Task ProcessInspectionFindingResponse(RecognitionResponse result, InspectionFindingRequest request)
        {
            try
            {
                // 保存到本地记录表
                //var inspectionRecord = new InspectionFindingRecord
                //{
                //    RequestId = result.request_id,
                //    PatientId = request.input.pv1_info?.pid,
                //    VisitType = request.input.pv1_info?.pat_visit_type,
                //    ApplyCount = request.input.apply_info?.Count ?? 0,
                //    DiagnosisCount = request.input.dg1_info?.Count ?? 0,
                //    ExternalCode = result.code,
                //    ExternalMsg = result.msg ?? result.message,
                //    RequestData = JsonSerializer.Serialize(request),
                //    ResponseData = JsonSerializer.Serialize(result),
                //    CreateTime = DateTime.Now,
                //    UpdateTime = DateTime.Now
                //};

                //// 保存记录到数据库（需要创建对应的数据库表和Service方法）
                //await _personService.SaveInspectionFindingRecordAsync(inspectionRecord);
            }
            catch (Exception ex)
            {
                // 记录保存失败不影响主流程，但需要记录日志
                Console.WriteLine($"保存检查结果记录失败: {ex.Message}");
            }
        }/// <summary>
         /// 处理S5008响应 - 更新申请单ID到互认记录表
         /// </summary>
         /// <param name="applyId">申请单ID(医嘱相关ID)</param>
         /// <param name="reportId">报告ID</param>
         /// <param name="doctorId">医生ID</param>
         /// <param name="patientId">病人ID</param>
        private async Task ProcessS4010Response(string applyId, string reportId, string doctorId, string patientId)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S4010");
            var logPath = Path.Combine(logDir, $"S4010_response_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理S4010响应");
                await LogToFileAsync(logPath, $"申请ID: {applyId}, 报告ID: {reportId},医生ID:{doctorId},病人ID:{patientId}");

                // 前置检查：确认记录存在
                var recordExists = await _personService.CheckReportRecognitionExistsAsync(reportId);
                if (!recordExists)
                {
                    await LogToFileAsync(logPath, $"错误: 未找到报告ID为 {reportId} 的记录");
                    throw new Exception($"未找到报告ID为 {reportId} 的记录");
                }

                // 更新申请单ID
                var result = await _personService.UpdateApplyIdInRecognitionRecord(reportId, applyId, doctorId, patientId);

                if (result.success)
                {
                    await LogToFileAsync(logPath, $"成功更新申请单ID到互认记录表 | 报告ID: {reportId} | 申请ID: {applyId}|医生ID:{doctorId}|" +
                        $"病人ID:{patientId}");
                }
                else
                {
                    await LogToFileAsync(logPath, $"更新申请ID失败: {result.message}");
                    throw new Exception($"更新申请单ID失败: {result.message}");
                }
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"处理S4010响应失败: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"处理S5008响应失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 处理S5008响应 - 更新申请单ID到互认记录表
        /// </summary>
        /// <param name="applyId">申请单ID</param>
        /// <param name="reportId">报告ID</param>
        /// <param name="doctorId">医生ID</param>
        /// <param name="patientId">病人ID</param>
        private async Task ProcessS5008Response(string applyId, string reportId,string doctorId,string  patientId)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "S5008");
            var logPath = Path.Combine(logDir, $"s5008_response_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始处理S5008响应");
                await LogToFileAsync(logPath, $"申请ID: {applyId}, 报告ID: {reportId},医生ID:{doctorId},病人ID:{patientId}");

                // 前置检查：确认记录存在
                var recordExists = await _personService.CheckReportRecognitionExistsAsync(reportId);
                if (!recordExists)
                {
                    await LogToFileAsync(logPath, $"错误: 未找到报告ID为 {reportId} 的记录");
                    throw new Exception($"未找到报告ID为 {reportId} 的记录");
                }

                // 更新申请单ID
                var result = await _personService.UpdateApplyIdInRecognitionRecord(reportId, applyId, doctorId, patientId);

                if (result.success)
                {
                    await LogToFileAsync(logPath, $"成功更新申请ID到互认记录表 | 报告ID: {reportId} | 申请ID: {applyId}|医生ID:{doctorId}|病人ID:{patientId}");
                }
                else
                {
                    await LogToFileAsync(logPath, $"更新申请ID失败: {result.message}");
                    throw new Exception($"更新申请ID失败: {result.message}");
                }
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"处理S5008响应失败: {ex.Message}\n堆栈: {ex.StackTrace}");
                throw new Exception($"处理S5008响应失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 日志方法
        /// </summary>
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
        public static class JsonOptions
        {
            public static readonly JsonSerializerOptions ChineseFriendly = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }
        private string SafeGetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop)
                ? prop.ValueKind == JsonValueKind.Null || prop.ValueKind == JsonValueKind.Undefined
                    ? ""
                    : prop.GetRawText().Trim('"')  // 获取原始文本并去除引号
                : "";
        }
        public class ReportResultItem
        {
            /// <summary>
            /// 结果类别：检查所见;诊断意见;诊断建议
            /// </summary>
            public string Category { get; set; } = "";

            /// <summary>
            /// 文字结果内容
            /// </summary>
            public string Content { get; set; } = "";
        }

        public class ReportFileItem
        {
            /// <summary>
            /// 文件格式
            /// </summary>
            public string FileFormat { get; set; } = "";

            /// <summary>
            /// 文件名
            /// </summary>
            public string FileName { get; set; } = "";

            /// <summary>
            /// 文件内容（Base64编码）
            /// </summary>
            public string FileContent { get; set; } = "";

            /// <summary>
            /// 备注
            /// </summary>
            public string Note { get; set; } = "";
        }
    }
}
