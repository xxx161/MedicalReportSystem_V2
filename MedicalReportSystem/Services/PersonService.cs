using com.sun.security.auth;
using Dapper;
using javax.xml.soap;
using MedicalReportSystem.Controllers;
using MedicalReportSystem.Models;
using MedicalReportSystem.Models.Config;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static com.sun.tools.@internal.xjc.reader.xmlschema.bindinfo.BIConversion;
using static com.sun.tools.javadoc.JavaScriptScanner;
using static MedicalReportSystem.Services.OracleSyncService;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace MedicalReportSystem.Services
{
    public class PersonService
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ReminderSettings _reminderSettings;
        public PersonService(IConfiguration configuration, IWebHostEnvironment env, IConfiguration configuration1)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection");
            _env = env;
            _configuration = configuration;
            _reminderSettings = new ReminderSettings();
            _configuration.GetSection("ReminderSettings").Bind(_reminderSettings);
        }
        /// <summary>
        /// 检查报告是否存在(Oracle版本)
        /// </summary>
        public async Task<bool> CheckReportExistsAsync(string userID, string type = "lab", string dataCodes = null)
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            string sql;
            if (type.ToLower() == "exam")
            {
                sql = "SELECT 1 FROM t_check_rec WHERE id_card_value = :userID";
            }
            else
            {
                sql = "SELECT 1 FROM t_test_rec WHERE id_card_value = :userID";
            }

            // 添加多个datacode的条件
            if (!string.IsNullOrEmpty(dataCodes))
            {
                var codeList = dataCodes.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (codeList.Length > 0)
                {
                    // 构建IN子句的参数
                    var parameters = new List<string>();
                    var dynamicParams = new DynamicParameters();
                    dynamicParams.Add("userID", userID);

                    for (int i = 0; i < codeList.Length; i++)
                    {
                        string paramName = $"dataCode{i}";
                        parameters.Add($":{paramName}");
                        dynamicParams.Add(paramName, codeList[i].Trim());
                    }

                    sql += $" AND datacode IN ({string.Join(",", parameters)})";

                    var result = await connection.ExecuteScalarAsync<int?>(sql, dynamicParams);
                    return result.HasValue;
                }
            }

            // 如果没有datacode参数，执行原始查询
            var resultWithoutCodes = await connection.ExecuteScalarAsync<int?>(sql, new { userID });
            return resultWithoutCodes.HasValue;
        }
        /// <summary>
        /// 查询GLHR_检查检验映射表 - 用于过滤检验和检查项目
        /// </summary>
        public async Task<(string mutualTreatmentId,string mutualTreatmentName, string part, string method,string execDeptId,string specimenSite,string collectionName)> QueryGLHRMappingAsync(
            string type,
            string mutualCode,
            string treatmentCode=null,
            string part = null,
            string method = null)
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            // 记录实际执行的SQL和参数
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "SQL调试");
            var logPath = Path.Combine(logDir, $"sql_debug_{DateTime.Now:yyyyMMdd}.log");
            Directory.CreateDirectory(logDir);

            string sql;
            object parameters;

            //if (string.IsNullOrEmpty(part) && string.IsNullOrEmpty(method))
            if (type=="lab")
            {
                // 检验项目：只需要互认编码和诊疗编码
                sql = @"
                SELECT T.互认诊疗项目id,A.名称 as 互认诊疗项目名称,A.标本部位,执行科室id,D.名称 采集名称
                FROM GLHR_检查检验映射 T, 诊疗项目目录 A ,诊疗执行科室 B,诊疗用法用量 C,诊疗项目目录 D 
                WHERE TRIM(T.互认诊疗项目id) = TRIM(A.ID) 
                AND A.ID=B.诊疗项目ID(+)
                AND A.ID=C.项目ID
                AND C.用法id=D.ID
                AND T.互认诊疗项目id IS NOT NULL
                AND LENGTH(TRIM(T.互认诊疗项目id)) > 0
                AND 互认编码 = :mutualCode 
                --AND 诊疗编码 = :treatmentCode 
                AND 部位 IS NULL 
                AND 方法 IS NULL
                AND ROWNUM = 1";

                //parameters = new { mutualCode, treatmentCode };
                parameters = new { mutualCode };
                await LogToFileAsync(logPath, $"检验项目查询SQL: {sql}");
                //await LogToFileAsync(logPath, $"参数: mutualCode='{mutualCode}', treatmentCode='{treatmentCode}'");
                await LogToFileAsync(logPath, $"参数: type:{type} ,mutualCode='{mutualCode}'");
            }
            else
            {
                // 检查项目：需要互认编码、诊疗编码、部位和方法  互认诊疗项目id, 部位, 方法 
                sql = @"
                SELECT t.互认诊疗项目id, t.部位, t.方法,A.名称 as 互认诊疗项目名称,标本部位,执行科室id
                FROM GLHR_检查检验映射 t, 诊疗项目目录 A ,诊疗执行科室 B
                WHERE TRIM(T.互认诊疗项目id) = TRIM(A.ID) 
                AND A.ID=B.诊疗项目ID
                AND T.互认诊疗项目id IS NOT NULL
                AND LENGTH(TRIM(T.互认诊疗项目id)) > 0
                and 互认编码 = :mutualCode 
                --AND 诊疗编码 = :treatmentCode 
                --AND (部位 = :part OR (:part IS NULL AND 部位 IS NULL))
                --AND (方法 = :method OR (:method IS NULL AND 方法 IS NULL))
                AND ROWNUM = 1";

                //parameters = new { mutualCode, treatmentCode, part, method };
                //parameters = new { mutualCode, part, method };
                parameters = new { mutualCode };
                await LogToFileAsync(logPath, $"检查项目查询SQL: {sql}");
                //await LogToFileAsync(logPath, $"参数: mutualCode='{mutualCode}', treatmentCode='{treatmentCode}', part='{part}', method='{method}'");
                //await LogToFileAsync(logPath, $"参数: mutualCode='{mutualCode}', part='{part}', method='{method}'");
                await LogToFileAsync(logPath, $"参数:type:'{type}' ,mutualCode='{mutualCode}'");
            }

            try
            {
                // 使用明确的类来映射
                var result = await connection.QueryFirstOrDefaultAsync<GLHRMapping>(sql, parameters);

                await LogToFileAsync(logPath, $"查询结果: {(result == null ? "null" : "有数据")}");

                if (result != null)
                {
                    string mutualTreatmentId = result.互认诊疗项目id;
                    string mutualTreatmentName = result.互认诊疗项目名称;
                    string resultPart = result.部位;
                    string resultMethod = result.方法;
                    string execDeptId = result.执行科室id;
                    string specimenSite = result.标本部位;
                    string collectionName = result.采集名称;

                    await LogToFileAsync(logPath, $"解析结果: mutualTreatmentId='{mutualTreatmentId}', part='{resultPart}', method='{resultMethod}'");

                    // 检查是否真的获取到了值
                    if (!string.IsNullOrEmpty(mutualTreatmentId))
                    {
                        return (mutualTreatmentId, mutualTreatmentName, resultPart, resultMethod, execDeptId, specimenSite, collectionName);
                    }
                    else
                    {
                        await LogToFileAsync(logPath, "警告: mutualTreatmentId 为空，但查询返回了数据");
                        return (null, null, null, null, null, null, null);
                    }
                }

                return (null, null,null, null, null, null, null);
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"查询异常: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        public async Task<(string DoctorName, string RegistrationNo, string PatientId, string OutpatientNo)> QueryPatientInfoAsync(string GHID)
        {
            try
            {
                var sql = "SELECT 执行人 as DoctorName, no as RegistrationNo, 病人ID as PatientId, 门诊号 as OutpatientNo FROM 病人挂号记录 WHERE ID = :GHID";

                using var connection = new OracleConnection(_connectionString);
                var result = await connection.QueryFirstOrDefaultAsync<(string, string, string, string)>(
                    sql,
                    new { GHID = GHID });

                return result;
            }
            catch (OracleException oracleEx)
            {
                throw new Exception($"Oracle数据库连接失败: {oracleEx.Message}. 请检查连接字符串和数据库服务状态", oracleEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"查询病人信息时发生错误: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 查询GLHR_检查检验映射表 - 重载版本，专门用于检验项目
        /// </summary>
        public async Task<string> QueryGLHRMappingForLabAsync(string type,string mutualCode, string treatmentCode)
        {
            var result = await QueryGLHRMappingAsync(type,mutualCode, treatmentCode);
            return result.mutualTreatmentId;
        }

        /// <summary>
        /// 查询GLHR_检查检验映射表 - 重载版本，专门用于检查项目
        /// </summary>
        public async Task<(string mutualTreatmentId, string mutualTreatmentName, string part, string method, string execDeptId, string specimenSite, string collectionName)> QueryGLHRMappingForExamAsync(
            string mutualCode,
            string treatmentCode,
            string part,
            string method)
        {
            return await QueryGLHRMappingAsync(mutualCode, treatmentCode, part, method);
        }

        /// <summary>
        /// 检查是否存在有效的互认映射记录
        /// </summary>
        public async Task<bool> CheckGLHRMappingExistsAsync(string mutualCode, string treatmentCode, string part = null, string method = null)
        {
            var result = await QueryGLHRMappingAsync(mutualCode, treatmentCode, part, method);
            return !string.IsNullOrEmpty(result.mutualTreatmentId);
        }
        /// <summary>
        /// 获取报告列表或表头(Oracle版本)
        /// </summary>
        public async Task<List<Test_Detail_oracle>> GetReportHeadersFixedAsync(
            string? reportId = null,
            string? userID = null,
            string? dataCode = null,
            string type = "lab")
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            var detail = new Test_Detail_oracle();
            var details = new List<Test_Detail_oracle>();

            if (type.ToLower() == "exam")
            {
                // 仅处理检查报告（T_CHECK_REC）
                detail.Report_CHECK_REC_oracle = await GetCheckRecDetailsAsync(connection, userID, reportId, null,dataCode,30);//默认30天的报告
            }
            else
            {
                // 仅处理检验报告（T_TEST_REC）
                detail.Reports_TEST_REC = await GetTestRecDetailsAsync(connection, userID, reportId, null, dataCode);
            }

            details.Add(detail);
            return details;
        }
        /// <summary>
         /// 获取报告列表或表头(Oracle版本)提醒记录过滤
         /// </summary>
        public async Task<List<Test_Detail_oracle>> GetReportHeadersFixedAsyncReminder(
            string? reportId = null,
            string? userID = null,
            string type = "lab",
            int? customDays = null)
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            var detail = new Test_Detail_oracle();
            var details = new List<Test_Detail_oracle>();

            if (type.ToLower() == "exam")
            {
                // 仅处理检查报告（T_CHECK_REC）
                detail.Report_CHECK_REC_oracle = await GetCheckRecDetailsAsyncReminder(connection, userID, reportId, null, customDays ?? _reminderSettings.DefaultLookbackDays);
            }
            else
            {
                // 仅处理检验报告（T_TEST_REC）
                detail.Reports_TEST_REC = await GetTestRecDetailsAsyncReminder(connection, userID, reportId, null, customDays ?? _reminderSettings.DefaultLookbackDays);
            }

            details.Add(detail);
            return details;
        }
        
        /// <summary>
        /// 查询报告明细(Oracle版本) - 适配Test_Detail结构
        /// </summary>
        public async Task<Test_Detail_oracle> GetReportDetailsFixedAsync(string? businessNo, string? reportId,string? currentMode)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询报告明细");
            var logPath = Path.Combine(logDir, $"查询报告明细{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"【开始】执行 GetReportDetailsFixedAsync 方法");
                await LogToFileAsync(logPath, $"参数: businessNo={businessNo}, reportId={reportId}, currentMode={currentMode}");

                var detail = new Test_Detail_oracle();
                await using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                if (currentMode == "recognize")
                {
                    await LogToFileAsync(logPath, $"进入GetTestRecDetailsAsync");
                    detail.Reports_TEST_REC = await GetTestRecDetailsAsync(connection, null, reportId, businessNo, null);
                }
                else
                {
                    await LogToFileAsync(logPath, $"进入GetTestRecDetailsAsyncReminder");
                    detail.Reports_TEST_REC = await GetTestRecDetailsAsyncReminder(connection, null, reportId, businessNo, 30);
                }
                // 2. 查询检验指标结果
                detail.Report_testr_res_indicate = await GetTestIndicatorsAsync(connection, reportId, businessNo);

                // 3. 查询微生物结果(可选)
                detail.Report_TMICROBE_BACTERIA_RES = await GetMicrobeBacteriaResultsAsync(connection, reportId,businessNo);

                // 4. 查询药敏结果(可选)
                detail.Report_TMICROBE_SUSCEPT_RES = await GetMicrobeSusceptResultsAsync(connection, reportId, businessNo);
                await LogToFileAsync(logPath, $"查询完成: {detail.Reports_TEST_REC?.Count ?? 0} 条记录");
                return detail;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"【异常】: {ex.GetType().Name}");
                await LogToFileAsync(logPath, $"消息: {ex.Message}");
                await LogToFileAsync(logPath, $"堆栈: {ex.StackTrace}");
                throw;
            }
        }
        public async Task<Test_Detail_oracle> GetReportDetailsFixedAsync2(string? businessNo, string? reportId, string? currentMode)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "查询报告明细");
            var logPath = Path.Combine(logDir, $"查询报告明细{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);

                await LogToFileAsync(logPath, $"【开始】执行 GetReportDetailsFixedAsync 方法");
                await LogToFileAsync(logPath, $"参数: businessNo={businessNo}, reportId={reportId}, currentMode={currentMode}");

                var detail = new Test_Detail_oracle();
                await using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                if (currentMode == "recognize")
                {
                    await LogToFileAsync(logPath, $"进入GetTestRecDetailsAsync");
                    detail.Reports_TEST_REC = await GetTestRecDetailsAsync(connection, null, reportId, businessNo, null);
                }
                else
                {
                    await LogToFileAsync(logPath, $"进入GetTestRecDetailsAsyncReminder");
                    detail.Reports_TEST_REC = await GetTestRecDetailsAsyncReminder(connection, null, reportId, businessNo, 30);
                }
                // 2. 查询检验指标结果
                //detail.Report_testr_res_indicate = await GetTestIndicatorsAsync(connection, reportId, businessNo);

                // 3. 查询微生物结果(可选)
                //detail.Report_TMICROBE_BACTERIA_RES = await GetMicrobeBacteriaResultsAsync(connection, reportId,businessNo);

                // 4. 查询药敏结果(可选)
                //detail.Report_TMICROBE_SUSCEPT_RES = await GetMicrobeSusceptResultsAsync(connection, reportId, businessNo);
                await LogToFileAsync(logPath, $"查询完成: {detail.Reports_TEST_REC?.Count ?? 0} 条记录");
                return detail;
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"【异常】: {ex.GetType().Name}");
                await LogToFileAsync(logPath, $"消息: {ex.Message}");
                await LogToFileAsync(logPath, $"堆栈: {ex.StackTrace}");
                throw;
            }
        }
        private async Task<List<T_TEST_REC_oracle>> GetTestRecDetailsAsync(
     OracleConnection connection,
     string? IdCard,
     string? reportId,
     string? businessNo,
     string? dataCode = null)  // 新增 dataCode 参数
        {
            // 记录实际执行的SQL和参数
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "检验SQL调试GetTestRecDetailsAsync");
            var logPath = Path.Combine(logDir, $"sql_debug_{DateTime.Now:yyyyMMdd}.log");
            await LogToFileAsync(logPath, $"=== 开始查询 ===");
            Directory.CreateDirectory(logDir);

            // 核心SQL，添加有效期控制
            var sql = @"
SELECT DISTINCT 
    T.*,
    a.互认时限,
    -- 计算是否在互认有效期内（处理两种日期格式）
    CASE 
        -- 格式1: ISO格式 (2025-06-22T09:06:26Z)
        WHEN T.test_report_date LIKE '%T%Z' THEN
            CASE 
                WHEN SYSDATE <= TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS') + a.互认时限 
                THEN '在互认有效期内'
                ELSE '已超出互认有效期'
            END
        -- 格式2: 空格分隔格式 (2024-10-23 15:49:54)
        WHEN T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' THEN
            CASE 
                WHEN SYSDATE <= TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS') + a.互认时限 
                THEN '在互认有效期内'
                ELSE '已超出互认有效期'
            END
        ELSE '日期格式异常'
    END as 互认状态,
    -- 计算有效期截止时间（处理两种日期格式）
    CASE 
        WHEN T.test_report_date LIKE '%T%Z' THEN
            TO_CHAR(TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS') + a.互认时限, 'YYYY-MM-DD HH24:MI:SS')
        WHEN T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' THEN
            TO_CHAR(TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS') + a.互认时限, 'YYYY-MM-DD HH24:MI:SS')
        ELSE NULL
    END as 有效期截止时间
FROM 
    t_test_rec T
    INNER JOIN GLHR_检查检验映射 a ON a.互认编码 = T.datacode
WHERE 
    T.mtuRecMark = 1 
    AND a.诊疗编码 IS NOT NULL
    -- 关键：添加互认有效期条件（处理两种日期格式）
    AND (
        -- ISO格式的有效期检查
        (T.test_report_date LIKE '%T%Z' AND SYSDATE <= TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS') + a.互认时限)
        OR
        -- 空格格式的有效期检查
        (T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' AND SYSDATE <= TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS') + a.互认时限)
    )";

            // 添加固定条件
            if (!string.IsNullOrEmpty(IdCard))
                sql += " AND T.ID_CARD_VALUE = :IdCard";

            if (!string.IsNullOrEmpty(reportId))
                sql += " AND T.ID = :reportId";

            if (!string.IsNullOrEmpty(businessNo))
                sql += " AND T.business_no = :businessNo";

            // 新增：处理 dataCode 参数
            if (!string.IsNullOrEmpty(dataCode))
            {
                // 方法1：使用 IN 子句（如果逗号分隔的值是干净的）
                if (!dataCode.Contains("'") && !dataCode.Contains(";")) // 安全检查
                {
                    // 移除空格，按逗号分割，并添加单引号
                    var codeArray = dataCode.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(x => $"'{x.Trim()}'")
                                           .ToArray();

                    if (codeArray.Length > 0)
                    {
                        var codesString = string.Join(",", codeArray);
                        sql += $" AND T.datacode IN ({codesString})";
                    }
                }
                else
                {
                    // 方法2：使用参数化查询（更安全）
                    // 注意：Oracle不支持直接传递数组参数，需要特殊处理
                    sql += " AND T.datacode IN (";
                    var codes = dataCode.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < codes.Length; i++)
                    {
                        sql += $":dataCode{i}";
                        if (i < codes.Length - 1)
                            sql += ",";
                    }
                    sql += ")";
                }
            }

            // 排序（处理两种日期格式）
            sql += @"
ORDER BY 
    CASE 
        -- ISO格式排序
        WHEN T.test_report_date LIKE '%T%Z' THEN 
            TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS')
        -- 空格格式排序
        WHEN T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' THEN
            TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS')
        ELSE NULL
    END DESC";

            // 记录SQL

            await LogToFileAsync(logPath, $"检查项目查询SQL: {sql},参数IdCard:{IdCard},参数businessNo:{businessNo},参数reportId:{reportId},参数dataCode:{dataCode}");
            using var cmd = new OracleCommand(sql, connection);

            // 绑定参数
            if (!string.IsNullOrEmpty(IdCard))
                cmd.Parameters.Add("IdCard", OracleDbType.Varchar2).Value = IdCard;

            if (!string.IsNullOrEmpty(reportId))
                cmd.Parameters.Add("reportId", OracleDbType.Varchar2).Value = reportId;

            if (!string.IsNullOrEmpty(businessNo))
                cmd.Parameters.Add("businessNo", OracleDbType.Varchar2).Value = businessNo;
            // 添加 dataCode 参数（如果使用参数化查询）
            if (!string.IsNullOrEmpty(dataCode) && dataCode.Contains("'"))
            {
                var codes = dataCode.Split(',', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < codes.Length; i++)
                {
                    cmd.Parameters.Add($"dataCode{i}", codes[i].Trim());
                }
            }

            // 在查询前记录完整的SQL（包括参数替换后的）
            try
            {
                // 构建包含实际参数值的SQL用于调试
                var debugSql = cmd.CommandText;
                foreach (OracleParameter param in cmd.Parameters)
                {
                    var paramName = param.ParameterName;
                    var paramValue = param.Value?.ToString() ?? "NULL";
                    debugSql = debugSql.Replace(":" + paramName, $"'{paramValue.Replace("'", "''")}'");
                }
                await LogToFileAsync(logPath, $"执行前完整SQL: {debugSql}");
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"构建调试SQL时出错: {ex.Message}");
            }
            var results = new List<T_TEST_REC_oracle>();
            int recordCount = 0;
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var record = new T_TEST_REC_oracle
                {
                    // 原有字段映射
                    Id = reader["ID"] as string,
                    Tid = reader["TID"] as string,
                    PatientId = reader["PATIENT_ID"] as string,
                    GatewayName = reader["GATEWAY_NAME"] as string,
                    BusinessNo = reader["BUSINESS_NO"] as string,
                    TestAuditTime = reader["TEST_AUDIT_TIME"] as string,
                    InsertDatacenterTime = reader["INSERT_DATACENTER_TIME"] as string,
                    DataNum = reader["DATA_NUM"] as long?,
                    InstockTime = reader["INSTOCK_TIME"] as string,
                    InsertDatetime = reader["INSERT_DATETIME"] as string,
                    UpdateTime = reader["UPDATE_TIME"] as string,
                    TestReportDate = reader["TEST_REPORT_DATE"] as string,
                    TestRecTime = reader["TEST_REC_TIME"] as string,
                    SampleTime = reader["SAMPLE_TIME"] as string,
                    BusinessGenerTime = reader["BUSINESS_GENER_TIME"] as string,
                    BirthDate = reader["BIRTH_DATE"] as string,
                    ApplicationTime = reader["APPLICATION_TIME"] as string,
                    AcceptSampleTime = reader["ACCEPT_SAMPLE_TIME"] as string,
                    BedNo = reader["BED_NO"] as string,
                    RoomNo = reader["ROOM_NO"] as string,
                    PatNo = reader["PAT_NO"] as string,
                    MtuRecLimitMark = reader["MTURECLIMITMARK"] as string,
                    MtuRecMark = reader["MTURECMARK"] as string,
                    DataName = reader["DATANAME"] as string,
                    DataCode = reader["DATACODE"] as string,
                    ReportClinialDiag = reader["REPORT_CLINIAL_DIAG"] as string,
                    WardName = reader["WARD_NAME"] as string,
                    UploadStatusMark = reader["UPLOAD_STATUS_MARK"] as string,
                    TestType = reader["TEST_TYPE"] as string,
                    TestReportOrgName = reader["TEST_REPORT_ORG_NAME"] as string,
                    TestReportNo = reader["TEST_REPORT_NO"] as string,
                    TestReportDepartNameExp = reader["TEST_REPORT_DEPART_NAME_EXP"] as string,
                    TestReportDepartCodeExp = reader["TEST_REPORT_DEPART_CODE_EXP"] as string,
                    TestReportDepartName = reader["TEST_REPORT_DEPART_NAME"] as string,
                    TestReportDepartCode = reader["TEST_REPORT_DEPART_CODE"] as string,
                    TestReportComment = reader["TEST_REPORT_COMMENT"] as string,
                    TestProjCategoryName = reader["TEST_PROJ_CATEGORY_NAME"] as string,
                    TestProjCategoryCode = reader["TEST_PROJ_CATEGORY_CODE"] as string,
                    TestDoctNo = reader["TEST_DOCT_NO"] as string,
                    TestDoctName = reader["TEST_DOCT_NAME"] as string,
                    TestApplyOrgName = reader["TEST_APPLY_ORG_NAME"] as string,
                    TestApplyDepartNameExp = reader["TEST_APPLY_DEPART_NAME_EXP"] as string,
                    TestApplyDepartCodeExp = reader["TEST_APPLY_DEPART_CODE_EXP"] as string,
                    TestApplyDepartName = reader["TEST_APPLY_DEPART_NAME"] as string,
                    TestApplyDepartCode = reader["TEST_APPLY_DEPART_CODE"] as string,
                    SpeInspectMark = reader["SPE_INSPECT_MARK"] as string,
                    SpecimenStatus = reader["SPECIMEN_STATUS"] as string,
                    SpecimenNo = reader["SPECIMEN_NO"] as string,
                    SpecimenName = reader["SPECIMEN_NAME"] as string,
                    SpecimenCollSite = reader["SPECIMEN_COLL_SITE"] as string,
                    SampleDoctName = reader["SAMPLE_DOCT_NAME"] as string,
                    ReportTypeName = reader["REPORT_TYPE_NAME"] as string,
                    ReportTypeCode = reader["REPORT_TYPE_CODE"] as string,
                    ReportDoctNo = reader["REPORT_DOCT_NO"] as string,
                    ReportDoctName = reader["REPORT_DOCT_NAME"] as string,
                    PatientOrgNo = reader["PATIENT_ORG_NO"] as string,
                    PatientName = reader["PATIENT_NAME"] as string,
                    OrgCode = reader["ORG_CODE"] as string,
                    OrgName = reader["ORG_NAME"] as string,
                    OrderRecFormNo = reader["ORDER_REC_FORM_NO"] as string,
                    MicrobeTestMark = reader["MICROBE_TEST_MARK"] as string,
                    IdCardValue = reader["ID_CARD_VALUE"] as string,
                    IdCardTypeName = reader["ID_CARD_TYPE_NAME"] as string,
                    IdCardTypeCode = reader["ID_CARD_TYPE_CODE"] as string,
                    HospitalNo = reader["HOSPITAL_NO"] as string,
                    HealthECode = reader["HEALTH_E_CODE"] as string,
                    GenderName = reader["GENDER_NAME"] as string,
                    GenderCode = reader["GENDER_CODE"] as string,
                    DiagTypeName = reader["DIAG_TYPE_NAME"] as string,
                    DiagTypeCode = reader["DIAG_TYPE_CODE"] as string,
                    DiagNo = reader["DIAG_NO"] as string,
                    AuditDoctNo = reader["AUDIT_DOCT_NO"] as string,
                    AuditDoctName = reader["AUDIT_DOCT_NAME"] as string,
                    ApplDoctNo = reader["APPL_DOCT_NO"] as string,
                    ApplDoctName = reader["APPL_DOCT_NAME"] as string,
                     
                };

                results.Add(record);
                recordCount++;
            }
             
            // 记录查询结果数量
            await LogToFileAsync(logPath, $"查询完成，共查询到 {recordCount} 条记录");
            await LogToFileAsync(logPath, $"=== 查询结束 ===");
            return results;
        }

        private async Task<List<T_TEST_REC_oracle>> GetTestRecDetailsAsyncReminder(OracleConnection connection, string? IdCard, string? reportId, string? businessNo, int? days = null)
        {
            int customDays;
            customDays =_reminderSettings.DefaultLookbackDays;
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "检验SQL调试GetTestRecDetailsAsyncReminder使用范围查阅模式提醒记录");
            var logPath = Path.Combine(logDir, $"sql_debug_{DateTime.Now:yyyyMMdd}.log");
            Directory.CreateDirectory(logDir);

            //  先记录所有传入参数
            await LogToFileAsync(logPath, $"=== 开始查询 ===");
            await LogToFileAsync(logPath, $"传入参数: customDays={customDays},days={days}, IdCard='{IdCard}', reportId='{reportId}', businessNo='{businessNo}'");

            // 核心SQL
            var sql = @"
SELECT DISTINCT 
    T.*,
    a.互认时限,
    -- 计算是否在互认有效期内（处理两种日期格式）
    CASE 
        -- 格式1: ISO格式 (2025-06-22T09:06:26Z)
        WHEN T.test_report_date LIKE '%T%Z' THEN
            CASE 
                WHEN SYSDATE <= TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS') + a.互认时限 
                THEN '在互认有效期内'
                ELSE '已超出互认有效期'
            END
        -- 格式2: 空格分隔格式 (2024-10-23 15:49:54)
        WHEN T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' THEN
            CASE 
                WHEN SYSDATE <= TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS') + a.互认时限 
                THEN '在互认有效期内'
                ELSE '已超出互认有效期'
            END
        ELSE '日期格式异常'
    END as 互认状态,
    -- 计算有效期截止时间（处理两种日期格式）
    CASE 
        WHEN T.test_report_date LIKE '%T%Z' THEN
            TO_CHAR(TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS') + a.互认时限, 'YYYY-MM-DD HH24:MI:SS')
        WHEN T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' THEN
            TO_CHAR(TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS') + a.互认时限, 'YYYY-MM-DD HH24:MI:SS')
        ELSE NULL
    END as 有效期截止时间
FROM 
    t_test_rec T
    INNER JOIN GLHR_检查检验映射 a ON a.互认编码 = T.datacode
WHERE 
    互认时限 is not null
    AND a.诊疗编码 IS NOT NULL
    -- 只保留检查报告时间在当前时间指定天数内条件
    AND (
        -- 格式1: ISO格式 (2025-06-22T09:06:26Z)
        (T.test_report_date LIKE '%T%Z' 
         AND TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS') >= SYSDATE - NUMTODSINTERVAL(" + customDays + @", 'DAY'))
        OR
        -- 格式2: 空格分隔格式 (2024-10-23 15:49:54)
        (T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' 
         AND TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS') >= SYSDATE - NUMTODSINTERVAL(" + customDays + @", 'DAY'))
      )";

            // 在这里动态添加其他条件
            if (!string.IsNullOrEmpty(IdCard))
            {
                sql += " AND T.ID_CARD_VALUE = :IdCard";
            }

            if (!string.IsNullOrEmpty(reportId))
            {
                sql += " AND T.ID = :reportId";
            }

            if (!string.IsNullOrEmpty(businessNo))
            {
                sql += " AND T.business_no = :businessNo";
            }

            // 添加排序
            sql += @"
ORDER BY 
    CASE 
        -- ISO格式排序
        WHEN T.test_report_date LIKE '%T%Z' THEN 
            TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS')
        -- 空格格式排序
        WHEN T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' THEN
            TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS')
        ELSE NULL
    END DESC";


            await LogToFileAsync(logPath, $"检查项目查询SQL: {sql}");

            using var cmd = new OracleCommand(sql, connection);
             

            // 或者方法2：使用AddWithValue
            // cmd.Parameters.Add(new OracleParameter("days", days));

            if (!string.IsNullOrEmpty(IdCard))
            {
                cmd.Parameters.Add("IdCard", OracleDbType.Varchar2).Value = IdCard;
                await LogToFileAsync(logPath, $"添加参数 IdCard: {IdCard}");
            }

            if (!string.IsNullOrEmpty(reportId))
            {
                cmd.Parameters.Add("reportId", OracleDbType.Varchar2).Value = reportId;
                await LogToFileAsync(logPath, $"添加参数 reportId: {reportId}");
            }

            if (!string.IsNullOrEmpty(businessNo))
            {
                cmd.Parameters.Add("businessNo", OracleDbType.Varchar2).Value = businessNo;
                await LogToFileAsync(logPath, $"添加参数 businessNo: {businessNo}");
            }

            //  调试：记录所有添加的参数
            await LogToFileAsync(logPath, $"总共添加了 {cmd.Parameters.Count} 个参数:");
            foreach (OracleParameter param in cmd.Parameters)
            {
                await LogToFileAsync(logPath, $"  参数名: {param.ParameterName}, 值: {param.Value}, 类型: {param.OracleDbType}");
            }
            try
            {
                // 构建包含实际参数值的SQL用于调试
                var debugSql = cmd.CommandText;
                foreach (OracleParameter param in cmd.Parameters)
                {
                    var paramName = param.ParameterName;
                    var paramValue = param.Value?.ToString() ?? "NULL";
                    debugSql = debugSql.Replace(":" + paramName, $"'{paramValue.Replace("'", "''")}'");
                }
                await LogToFileAsync(logPath, $"执行前完整SQL: {debugSql}");
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"构建调试SQL时出错: {ex.Message}");
            }
            var results = new List<T_TEST_REC_oracle>();
            int recordCount = 0;
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var record = new T_TEST_REC_oracle();

                // 主键字段（确保类型匹配）
                record.Tid = reader["TID"] as string;

                // 字符串字段映射
                record.AcceptSampleTime = reader["ACCEPT_SAMPLE_TIME"] as string;
                record.ApplicationTime = reader["APPLICATION_TIME"] as string;
                record.ApplDoctName = reader["APPL_DOCT_NAME"] as string;
                record.ApplDoctNo = reader["APPL_DOCT_NO"] as string;
                record.AuditDoctName = reader["AUDIT_DOCT_NAME"] as string;
                record.AuditDoctNo = reader["AUDIT_DOCT_NO"] as string;
                record.BirthDate = reader["BIRTH_DATE"] as string;
                record.BusinessGenerTime = reader["BUSINESS_GENER_TIME"] as string;
                record.BusinessNo = reader["BUSINESS_NO"] as string;
                record.DiagNo = reader["DIAG_NO"] as string;
                record.DiagTypeCode = reader["DIAG_TYPE_CODE"] as string;
                record.DiagTypeName = reader["DIAG_TYPE_NAME"] as string;
                record.GenderCode = reader["GENDER_CODE"] as string;
                record.GenderName = reader["GENDER_NAME"] as string;
                record.HealthECode = reader["HEALTH_E_CODE"] as string;
                record.HospitalNo = reader["HOSPITAL_NO"] as string;
                record.IdCardTypeCode = reader["ID_CARD_TYPE_CODE"] as string;
                record.IdCardTypeName = reader["ID_CARD_TYPE_NAME"] as string;
                record.IdCardValue = reader["ID_CARD_VALUE"] as string;
                record.MicrobeTestMark = reader["MICROBE_TEST_MARK"] as string;
                record.OrderRecFormNo = reader["ORDER_REC_FORM_NO"] as string;
                record.OrgName = reader["ORG_NAME"] as string;
                record.OrgCode = reader["ORG_CODE"] as string;
                record.PatientName = reader["PATIENT_NAME"] as string;
                record.PatientOrgNo = reader["PATIENT_ORG_NO"] as string;
                record.ReportDoctName = reader["REPORT_DOCT_NAME"] as string;
                record.ReportDoctNo = reader["REPORT_DOCT_NO"] as string;
                record.ReportTypeCode = reader["REPORT_TYPE_CODE"] as string;
                record.ReportTypeName = reader["REPORT_TYPE_NAME"] as string;
                record.SampleDoctName = reader["SAMPLE_DOCT_NAME"] as string;
                record.SampleTime = reader["SAMPLE_TIME"] as string;
                record.SpecimenCollSite = reader["SPECIMEN_COLL_SITE"] as string;
                record.SpecimenName = reader["SPECIMEN_NAME"] as string;
                record.SpecimenNo = reader["SPECIMEN_NO"] as string;
                record.SpecimenStatus = reader["SPECIMEN_STATUS"] as string;
                record.SpeInspectMark = reader["SPE_INSPECT_MARK"] as string;
                record.TestApplyDepartCode = reader["TEST_APPLY_DEPART_CODE"] as string;
                record.TestApplyDepartName = reader["TEST_APPLY_DEPART_NAME"] as string;
                record.TestApplyDepartCodeExp = reader["TEST_APPLY_DEPART_CODE_EXP"] as string;
                record.TestApplyDepartNameExp = reader["TEST_APPLY_DEPART_NAME_EXP"] as string;
                record.TestApplyOrgName = reader["TEST_APPLY_ORG_NAME"] as string;
                record.TestDoctName = reader["TEST_DOCT_NAME"] as string;
                record.TestDoctNo = reader["TEST_DOCT_NO"] as string;
                record.TestProjCategoryCode = reader["TEST_PROJ_CATEGORY_CODE"] as string;
                record.TestProjCategoryName = reader["TEST_PROJ_CATEGORY_NAME"] as string;
                record.TestRecTime = reader["TEST_REC_TIME"] as string;
                record.TestReportComment = reader["TEST_REPORT_COMMENT"] as string;
                record.TestReportDate = reader["TEST_REPORT_DATE"] as string;
                record.TestReportDepartCode = reader["TEST_REPORT_DEPART_CODE"] as string;
                record.TestReportDepartName = reader["TEST_REPORT_DEPART_NAME"] as string;
                record.TestReportDepartCodeExp = reader["TEST_REPORT_DEPART_CODE_EXP"] as string;
                record.TestReportDepartNameExp = reader["TEST_REPORT_DEPART_NAME_EXP"] as string;
                record.TestReportNo = reader["TEST_REPORT_NO"] as string;
                record.TestReportOrgName = reader["TEST_REPORT_ORG_NAME"] as string;
                record.TestType = reader["TEST_TYPE"] as string;
                record.UpdateTime = reader["UPDATE_TIME"] as string;
                record.UploadStatusMark = reader["UPLOAD_STATUS_MARK"] as string;
                record.WardName = reader["WARD_NAME"] as string;
                record.InsertDatetime = reader["INSERT_DATETIME"] as string;
                record.InstockTime = reader["INSTOCK_TIME"] as string;
                record.GatewayName = reader["GATEWAY_NAME"] as string;
                record.DataNum = reader["DATA_NUM"] as long?;
                record.InsertDatacenterTime = reader["INSERT_DATACENTER_TIME"] as string;
                record.Id = reader["ID"] as string;
                record.PatientId = reader["PATIENT_ID"] as string;
                record.TestAuditTime = reader["TEST_AUDIT_TIME"] as string;
                record.ReportClinialDiag = reader["REPORT_CLINIAL_DIAG"] as string;
                record.DataCode = reader["DATACODE"] as string;
                record.DataName = reader["DATANAME"] as string;
                record.MtuRecMark = reader["MTURECMARK"] as string;
                record.MtuRecLimitMark = reader["MTURECLIMITMARK"] as string;
                record.PatNo = reader["PAT_NO"] as string;
                record.RoomNo = reader["ROOM_NO"] as string;
                record.BedNo = reader["BED_NO"] as string;

                results.Add(record);
                recordCount++;
            }

            // 记录查询结果数量
            await LogToFileAsync(logPath, $"查询完成，共查询到 {recordCount} 条记录");
            await LogToFileAsync(logPath, $"=== 查询结束 ===");
            return results;
        }
        private async Task<Test_Detail_oracle> GetSimpleReportDetails(string? businessNo, string? reportId, string? currentMode)
        {
            var detail = new Test_Detail_oracle();

            try
            {
                await using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                // 最简化的查询，避免复杂逻辑
                var simpleSql = @"
SELECT ID, BUSINESS_NO, PATIENT_NAME, TEST_REPORT_DATE, DATACODE
FROM t_test_rec 
WHERE ID = :reportId 
    AND BUSINESS_NO = :businessNo
    AND ROWNUM = 1";

                using var cmd = new OracleCommand(simpleSql, connection);
                cmd.Parameters.Add("reportId", OracleDbType.Varchar2).Value = reportId;
                cmd.Parameters.Add("businessNo", OracleDbType.Varchar2).Value = businessNo;

                using var reader = await cmd.ExecuteReaderAsync();

                detail.Reports_TEST_REC = new List<T_TEST_REC_oracle>();

                if (await reader.ReadAsync())
                {
                    detail.Reports_TEST_REC.Add(new T_TEST_REC_oracle
                    {
                        Id = reader["ID"] as string,
                        BusinessNo = reader["BUSINESS_NO"] as string,
                        PatientName = reader["PATIENT_NAME"] as string,
                        TestReportDate = reader["TEST_REPORT_DATE"] as string,
                        DataCode = reader["DATACODE"] as string
                    });
                }

                return detail;
            }
            catch (Exception ex)
            {
                // 如果简化查询也失败，返回空数据
                return new Test_Detail_oracle
                {
                    Reports_TEST_REC = new List<T_TEST_REC_oracle>()
                };
            }
        }
        private async Task<List<T_testr_res_indicate_oracle>> GetTestIndicatorsAsync(OracleConnection connection, string? reportId, string? businessNo)
        {
            var  sql = @"
            SELECT * 
            FROM T_TESTR_RES_INDICATE 
            WHERE 1=1 ";
            //var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "DatabaseOperations");
            //var logPath = Path.Combine(logDir, $"查询报告明细{DateTime.Now:yyyyMMdd}.log");
            //Directory.CreateDirectory(logDir);
            // 只有当reportId不为空时才添加条件
            //if (!string.IsNullOrEmpty(reportId))
            //sql += " AND ID = :reportId";

            // 记录方法开始
            //await LogToFileAsync(logPath, $"【开始】执行 GetTestIndicatorsAsync 方法");
            //await LogToFileAsync(logPath, $"参数 - reportId: {reportId ?? "null"}, testReportNo: {testReportNo ?? "null"}");

            // 构建SQL查询 

            //if (!string.IsNullOrEmpty(reportId))
            //{
            //    sql += " AND ID = :reportId";
            //    await LogToFileAsync(logPath, $"添加查询条件: ID = {reportId}");
            //}

            if (!string.IsNullOrEmpty(businessNo))
            {
                sql += " AND business_no = :businessNo";
                //await LogToFileAsync(logPath, $"添加查询条件: TEST_REPORT_NO = {testReportNo}");
            }

            //await LogToFileAsync(logPath, $"最终SQL语句: {sql}");

            // 执行查询
            using var cmd = new OracleCommand(sql, connection);

            //if (!string.IsNullOrEmpty(reportId))
            //    cmd.Parameters.Add("reportId", OracleDbType.Varchar2).Value = reportId;

            if (!string.IsNullOrEmpty(businessNo))
                cmd.Parameters.Add("businessNo", OracleDbType.Varchar2).Value = businessNo;

            // 记录参数值
           // await LogToFileAsync(logPath, $"SQL参数设置完成 - reportId: {reportId}, testReportNo: {testReportNo}");

            var results = new List<T_testr_res_indicate_oracle>();
            var stopwatch = Stopwatch.StartNew();

            using var reader = await cmd.ExecuteReaderAsync();
            //await LogToFileAsync(logPath, $"数据库查询执行完成，耗时: {stopwatch.ElapsedMilliseconds}ms");

            int recordCount = 0;

            while (await reader.ReadAsync())
            {
                var record = new T_testr_res_indicate_oracle
                {
                    Tid = reader["TID"] as string,
                    AnomalyCode = reader["anomaly_code"] as string,
                    AnomalyName = reader["anomaly_name"] as string,
                    BusinessGenerTime = reader["business_gener_time"] as string,
                    BusinessNo = reader["business_no"] as string,
                    ClinicalMeaningDesc = reader["clinical_meaning_desc"] as string,
                    CriticalLowerLimit = reader["critical_lower_limit"] as string,
                    CriticalSign = reader["critical_sign"] as string,
                    CriticalTimelySign = reader["critical_timely_sign"] as string,
                    CriticalUpperLimit = reader["critical_upper_limit"] as string,
                    CriticalValueRes = reader["critical_value_res"] as string,
                    DataId = reader["data_id"] as string,
                    DataNum = reader["data_num"] as long?,
                    DataCode = reader["dataCode"] as string,
                    DataName = reader["dataName"] as string,
                    EquipmentCode = reader["equipment_code"] as string,
                    ExamItemCode = reader["exam_item_code"] as string,
                    ExamItemName = reader["exam_item_name"] as string,
                    GatewayName = reader["gateway_name"] as string,
                    Id = reader["ID"] as string,
                    InsertDatacenterTime = reader["insert_datacenter_time"] as string,
                    InsertDatetime = reader["INSERT_DATETIME"] as string,
                    InspectionMethods = reader["inspection_methods"] as string,
                    InstockTime = reader["INSTOCK_TIME"] as string,
                    InstrumentCode = reader["instrument_code"] as string,
                    InstrumentName = reader["instrument_name"] as string,
                    LoincCode = reader["loinc_code"] as string,
                    MtuRecLimitMark = reader["mtuRecLimitMark"] as string,
                    MtuRecMark = reader["mtuRecMark"] as string,
                    NormalRefLimit = reader["normal_ref_limit"] as string,
                    NormalRefRes = reader["normal_ref_res"] as string,
                    OrgCode = reader["org_code"] as string,
                    OrgName = reader["org_name"] as string,
                    PatientId = reader["patient_id"] as string,
                    TestIndexResult = reader["test_index_result"] as string,
                    TestIndexUnit = reader["test_index_uint"] as string,
                    TestProjCodeExp = reader["test_proj_code_exp"] as string,
                    TestProjNameExp = reader["TEST_PROJ_NAME_EXP"] as string,
                    TestReportNo = reader["test_report_no"] as string,
                    TestResDescription = reader["test_res_descri"] as string,
                    TestResTypeName = reader["test_res_type_name"] as string,
                    UpdateTime = reader["update_time"] as string
                };

                recordCount++;
                results.Add(record);
            }
            //await LogToFileAsync(logPath, $"【完成】成功处理 {recordCount} 条记录，总耗时: {stopwatch.ElapsedMilliseconds}ms");
            return results;
        }

        private async Task<List<T_MICROBE_BACTERIA_RES_oracle>> GetMicrobeBacteriaResultsAsync(OracleConnection connection, string? reportId,string? businessNo)
        {
            var sql = @"
            SELECT * 
            FROM T_MICROBE_BACTERIA_RES 
            WHERE 1=1 ";

            // 只有当reportId不为空时才添加条件
            //if (!string.IsNullOrEmpty(reportId))
                //sql += " AND ID = :reportId";

            if (!string.IsNullOrEmpty(businessNo))
                sql += " AND business_no = :businessNo";


            using var cmd = new OracleCommand(sql, connection);
            //f (!string.IsNullOrEmpty(reportId))
                //cmd.Parameters.Add("reportId", OracleDbType.Varchar2).Value = reportId;
            if (!string.IsNullOrEmpty(businessNo))
                cmd.Parameters.Add("businessNo", OracleDbType.Varchar2).Value = businessNo;

            var results = new List<T_MICROBE_BACTERIA_RES_oracle>();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var record = new T_MICROBE_BACTERIA_RES_oracle
                {
                    Tid = reader["TID"] as string,
                    BacteriaId = reader["BACTERIA_ID"] as string,
                    BacteriaIndicateNo = reader["BACTERIA_INDICATE_NO"] as string,
                    BacteriaName = reader["BACTERIA_NAME"] as string,
                    BacteriaResultDescription = reader["BACTERIA_RESULT_DESCR"] as string,
                    BacterRingNum = reader["BACTER_RING_NUM"] as string,
                    BusinessGenerTime = reader["BUSINESS_GENER_TIME"] as DateTime?,
                    BusinessNo = reader["BUSINESS_NO"] as string,
                    ColonyCount = reader["COLONY_COUNT"] as string,
                    ColonyForm = reader["COLONY_FORM"] as string,
                    DataId = reader["DATA_ID"] as string,
                    DataNum = reader["DATA_NUM"] as long?,
                    DeviceName = reader["DEVICE_NAME"] as string,
                    DeviceNo = reader["DEVICE_NO"] as string,
                    FoundWay = reader["FOUND_WAY"] as string,
                    GatewayName = reader["GATEWAY_NAME"] as string,
                    Id = reader["ID"] as string,
                    IncubationCondition = reader["INCUBATION_CONDITION"] as string,
                    IncubationTime = reader["INCUBATION_TIME"] as string,
                    InsertDatacenterTime = reader["INSERT_DATACENTER_TIME"] as string,
                    InsertDatetime = reader["INSERT_DATETIME"] as string,
                    InstockTime = reader["INSTOCK_TIME"] as string,
                    Medium = reader["MEDIUM"] as string,
                    MultiBacteriaMark = reader["MULTI_BACTERIA_MARK"] as string,
                    OrgCode = reader["ORG_CODE"] as string,
                    OrgName = reader["ORG_NAME"] as string,
                    PaperContainNum = reader["PAPER_CONTAIN_NUM"] as string,
                    PatientId = reader["PATIENT_ID"] as string,
                    TestBoardName = reader["TEST_BOARD_NAME"] as string,
                    TestBoardNumber = reader["TEST_BOARD_NUMBER"] as string,
                    TestReportNo = reader["TEST_REPORT_NO"] as string,
                    TestResult = reader["TEST_RESULT"] as string,
                    TestResultDescription = reader["TEST_RESULT_DESCRIPTION"] as string,
                    UpdateTime = reader["UPDATE_TIME"] as string
                };
                results.Add(record);
            }
            return results;
        }

        private async Task<List<T_MICROBE_SUSCEPT_RES_oracle>> GetMicrobeSusceptResultsAsync(OracleConnection connection, string? reportId, string? businessNo)
        {
            var sql = @"
            SELECT * 
            FROM T_MICROBE_SUSCEPT_RES 
            WHERE 1=1 ";

            // 只有当reportId不为空时才添加条件
            //if (!string.IsNullOrEmpty(reportId))
                //sql += " AND ID = :reportId";

            if (!string.IsNullOrEmpty(businessNo))
                sql += " AND business_no = :businessNo";


            using var cmd = new OracleCommand(sql, connection);
            //if (!string.IsNullOrEmpty(reportId))
                //cmd.Parameters.Add("reportId", OracleDbType.Varchar2).Value = reportId;
            if (!string.IsNullOrEmpty(businessNo))
                cmd.Parameters.Add("businessNo", OracleDbType.Varchar2).Value = businessNo;

            var results = new List<T_MICROBE_SUSCEPT_RES_oracle>();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var record = new T_MICROBE_SUSCEPT_RES_oracle
                {
                    Tid = reader["TID"] as string,
                    BacteriaId = reader["BACTERIA_ID"] as string,
                    BacteriaName = reader["BACTERIA_NAME"] as string,
                    BacteriostatRing = reader["BACTERIOSTAT_RING"] as string,
                    BacteriostaticConcentrate = reader["BACTERIOSTATIC_CONCENTRATE"] as string,
                    BusinessGenerTime = reader["BUSINESS_GENER_TIME"] as DateTime?,
                    BusinessNo = reader["BUSINESS_NO"] as string,
                    DataId = reader["DATA_ID"] as string,
                    DataNum = reader["DATA_NUM"] as long?,
                    DrugSusceptibilityCode = reader["DRUG_SUSCEPTIBIBLE_CODE"] as string,
                    DrugSusceptibilityName = reader["DRUG_SUSCEPTIBIBLE_NAME"] as string,
                    DrugSusceptibleName = reader["DRUG_SUSCEPTIBLE_NAME"] as string,
                    DrugSusceptibleNo = reader["DRUG_SUSCEPTIBLE_NO"] as string,
                    DrugTestNo = reader["DRUG_TEST_NO"] as string,
                    ExpertRule = reader["EXPERT_RULE"] as string,
                    GatewayName = reader["GATEWAY_NAME"] as string,
                    Id = reader["ID"] as string,
                    InsertDatacenterTime = reader["INSERT_DATACENTER_TIME"] as string,
                    InsertDatetime = reader["INSERT_DATETIME"] as string,
                    InspectionMethods = reader["INSPECTION_METHODS"] as string,
                    InstockTime = reader["INSTOCK_TIME"] as string,
                    OrgCode = reader["ORG_CODE"] as string,
                    OrgName = reader["ORG_NAME"] as string,
                    PaperDrugContent = reader["PAPER_DRUG_CONTENT"] as string,
                    PaperDrugUnit = reader["PAPER_DRUG_UNIT"] as string,
                    PatientId = reader["PATIENT_ID"] as string,
                    ReferenceValue = reader["REFERENCE_VALUE"] as string,
                    SusceptibilityResultDescription = reader["SUSCEPT_RESULT_DESCR"] as string,
                    TestReportNo = reader["TEST_REPORT_NO"] as string,
                    UpdateTime = reader["UPDATE_TIME"] as string
                };
                results.Add(record);
            }
            return results;
        }

        private async Task<List<T_CHECK_REC_oracle>> GetCheckRecDetailsAsync(OracleConnection connection, string? IdCard, string? reportId, string? checktReportNo, string? dataCode, int days = 15)
        {
            int  customDays = _reminderSettings.DefaultLookbackDays;
            // 记录实际执行的SQL和参数
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "检查SQL调试GetCheckRecDetailsAsync");
            var logPath = Path.Combine(logDir, $"sql_debug_{DateTime.Now:yyyyMMdd}.log");
            Directory.CreateDirectory(logDir);
            await LogToFileAsync(logPath, $"=== 开始查询 ===");
            var sql = @"
SELECT * 
FROM (
    SELECT T.*, t.rowid
    FROM t_check_rec T 
    WHERE 1=1 AND T.mtuRecMark = 1";

            if (!string.IsNullOrEmpty(IdCard))
                sql += " AND T.id_card_value = :IdCard";

            if (!string.IsNullOrEmpty(reportId))
                sql += " AND T.ID = :reportId";

            if (!string.IsNullOrEmpty(checktReportNo))
                sql += " AND T.check_report_no = :checktReportNo";

            // 处理 dataCode 参数
            List<string> dataCodeParams = new List<string>();
            if (!string.IsNullOrEmpty(dataCode))
            {
                // 方法1：使用 IN 子句（如果逗号分隔的值是干净的）
                if (!dataCode.Contains("'") && !dataCode.Contains(";")) // 安全检查
                {
                    var codeArray = dataCode.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(x => $"'{x.Trim()}'")
                                           .ToArray();

                    if (codeArray.Length > 0)
                    {
                        var codesString = string.Join(",", codeArray);
                        sql += $" AND T.datacode IN ({codesString})";
                    }
                }
                else
                {
                    // 方法2：使用参数化查询（更安全）
                    sql += " AND T.datacode IN (";
                    var codes = dataCode.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < codes.Length; i++)
                    {
                        string paramName = $":dataCode{i}";
                        sql += paramName;
                        if (i < codes.Length - 1)
                            sql += ",";
                        dataCodeParams.Add(paramName.Replace(":", ""));
                    }
                    sql += ")";
                }
            }

            // 修改：将 days 参数直接嵌入SQL中，而不是使用绑定参数
            sql += @"
      AND (
        -- 格式1: ISO格式 (2025-06-22T09:06:26Z)
        (T.check_report_date LIKE '%T%Z' 
         AND TO_DATE(REPLACE(REPLACE(T.check_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS') >= SYSDATE - NUMTODSINTERVAL(" + days + @", 'DAY'))
        OR
        -- 格式2: 空格分隔格式 (2024-10-23 15:49:54)
        (T.check_report_date LIKE '% %' AND T.check_report_date NOT LIKE '%T%' 
         AND TO_DATE(T.check_report_date, 'YYYY-MM-DD HH24:MI:SS') >= SYSDATE - NUMTODSINTERVAL(" + days + @", 'DAY'))
      )";

            // 排序
            sql += @"
    ORDER BY 
        CASE 
            -- ISO格式排序
            WHEN T.check_report_date LIKE '%T%Z' THEN 
                TO_DATE(REPLACE(REPLACE(T.check_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS')
            -- 空格格式排序
            WHEN T.check_report_date LIKE '% %' AND T.check_report_date NOT LIKE '%T%' THEN
                TO_DATE(T.check_report_date, 'YYYY-MM-DD HH24:MI:SS')
            ELSE NULL
        END DESC
)";

            await LogToFileAsync(logPath, $"检查项目查询SQL: {sql},参数IdCard:{IdCard},参数checktReportNo:{checktReportNo},参数reportId:{reportId},参数dataCode:{dataCode},参数days:{days},参数customDays:{customDays}");
            using var cmd = new OracleCommand(sql, connection);

            // 绑定参数
            //cmd.Parameters.Add("days", OracleDbType.Int32).Value = days;
            if (!string.IsNullOrEmpty(IdCard))
            {
                cmd.Parameters.Add("IdCard", OracleDbType.Varchar2).Value = IdCard;
                // 注意：由于在子查询中再次使用了PatientOrgNo，需要确保参数正确传递
                // Oracle通常会自动处理同名参数
            }

            if (!string.IsNullOrEmpty(reportId))
                cmd.Parameters.Add("reportId", OracleDbType.Varchar2).Value = reportId;

            if (!string.IsNullOrEmpty(checktReportNo))
                cmd.Parameters.Add("checktReportNo", OracleDbType.Varchar2).Value = checktReportNo; 
            // 添加 dataCode 参数（如果使用参数化查询）
            if (!string.IsNullOrEmpty(dataCode) && dataCode.Contains("'"))
            {
                var codes = dataCode.Split(',', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < codes.Length; i++)
                {
                    cmd.Parameters.Add($"dataCode{i}", codes[i].Trim());
                }
            }
            // 在查询前记录完整的SQL（包括参数替换后的）
            try
            {
                // 构建包含实际参数值的SQL用于调试
                var debugSql = cmd.CommandText;
                foreach (OracleParameter param in cmd.Parameters)
                {
                    var paramName = param.ParameterName;
                    var paramValue = param.Value?.ToString() ?? "NULL";
                    debugSql = debugSql.Replace(":" + paramName, $"'{paramValue.Replace("'", "''")}'");
                }
                await LogToFileAsync(logPath, $"执行前完整SQL: {debugSql}");
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"构建调试SQL时出错: {ex.Message}");
            }

            var results = new List<T_CHECK_REC_oracle>();
            int recordCount = 0;
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var record = new T_CHECK_REC_oracle
                {
                    Tid = reader["TID"] as string,
                    ApplyTime = reader["apply_time"] as string,
                    ApplDoctName = reader["appl_doct_name"] as string,
                    ApplDoctNo = reader["appl_doct_no"] as string,
                    AuditDoctName = reader["audit_doct_name"] as string,
                    AuditDoctNo = reader["audit_doct_no"] as string,
                    BedNo = reader["bed_no"] as string,
                    BiopsySite = reader["biopsy_site"] as string,
                    BirthDate = reader["birth_date"] as string,
                    BusinessGenerTime = reader["business_gener_time"] as string,
                    BusinessNo = reader["business_no"] as string,
                    CheckApplyDepartName = reader["check_apply_depart_name"] as string,
                    CheckApplyDepartNameExp = reader["check_apply_depart_name_exp"] as string,
                    CheckApplyDepartNo = reader["check_apply_depart_no"] as string,
                    CheckApplyDepartNoExp = reader["check_apply_depart_no_exp"] as string,
                    CheckApplyOrgName = reader["check_apply_org_name"] as string,
                    CheckAuditTime = reader["check_audit_time"] as string,
                    CheckDoctName = reader["check_doct_name"] as string,
                    CheckDoctNo = reader["check_doct_no"] as string,
                    CheckEquipModel = reader["check_equip_model"] as string,
                    CheckEquipNum = reader["check_equip_num"] as string,
                    CheckIndexResult = reader["check_index_result"] as string,
                    CheckIndexUnit = reader["check_index_uint"] as string,
                    CheckMethodName = reader["check_methed_name"] as string,
                    CheckNormalRefQual = reader["check_normal_ref_qual"] as string,
                    CheckPartCode = reader["check_part_code"] as string,
                    CheckPartName = reader["check_part_name"] as string,
                    CheckProjCode = reader["check_proj_code"] as string,
                    CheckProjCodeExp = reader["check_proj_code_exp"] as string,
                    CheckProjName = reader["check_proj_name"] as string,
                    CheckProjNameExp = reader["check_proj_name_exp"] as string,
                    CheckRecNo = reader["check_rec_no"] as string,
                    CheckReportComment = reader["check_report_comment"] as string,
                    CheckReportDate = reader["check_report_date"] as string,
                    CheckReportDepartCode = reader["check_report_depart_code"] as string,
                    CheckReportDepartCodeExp = reader["check_report_depart_code_exp"] as string,
                    CheckReportDepartName = reader["check_report_depart_name"] as string,
                    CheckReportDepartNameExp = reader["check_report_depart_name_exp"] as string,
                    CheckReportNo = reader["check_report_no"] as string,
                    CheckReportOrgName = reader["check_report_org_name"] as string,
                    CheckRes = reader["check_res"] as string,
                    CheckResCode = reader["check_res_code"] as string,
                    CheckResName = reader["check_res_name"] as string,
                    CheckResObj = reader["check_res_obj"] as string,
                    CheckResQual = reader["check_res_qual"] as string,
                    CheckResSub = reader["check_res_sub"] as string,
                    CheckTypeCode = reader["check_type_code"] as string,
                    CheckTypeName = reader["check_type_name"] as string,
                    CriticalState = reader["critical_state"] as string,
                    DataNum = reader["data_num"] as long?,
                    DataCode = reader["dataCode"] as string,
                    DataName = reader["dataName"] as string,
                    DiagNo = reader["diag_no"] as string,
                    DiagTypeCode = reader["diag_type_code"] as string,
                    DiagTypeName = reader["diag_type_name"] as string,
                    Frozen = reader["frozen"] as string,
                    GatewayName = reader["gateway_name"] as string,
                    GenderCode = reader["gender_code"] as string,
                    GenderName = reader["gender_name"] as string,
                    HealthECode = reader["health_e_code"] as string,
                    HospitalNo = reader["hospital_no"] as string,
                    Id = reader["ID"] as string,
                    IdCardTypeCode = reader["id_card_type_code"] as string,
                    IdCardTypeName = reader["id_card_type_name"] as string,
                    IdCardValue = reader["id_card_value"] as string,
                    ImageExistMark = reader["image_exist_mark"] as string,
                    ImageNo = reader["image_no"] as string,
                    ImageUidAddr = reader["image_uid_addr"] as string,
                    ImmuNumber = reader["immu_number"] as string,
                    InsertDatacenterTime = reader["insert_datacenter_time"] as string,
                    InsertDatetime = reader["INSERT_DATETIME"] as string,
                    InstockTime = reader["INSTOCK_TIME"] as string,
                    InstrumentName = reader["instrument_name"] as string,
                    MtuRecLimitMark = reader["mtuRecLimitMark"] as string,
                    MtuRecMark = reader["mtuRecMark"] as string,
                    NormalRefLowerLimit = reader["normal_ref_lower_limit"] as string,
                    NormalRefUpperLimit = reader["normal_ref_upper_limit"] as string,
                    OrderRecFormNo = reader["order_rec_form_no"] as string,
                    OrgCode = reader["org_code"] as string,
                    OrgName = reader["org_name"] as string,
                    PatNo = reader["pat_no"] as string,
                    PathologicalNakedEye = reader["pathological_naked_eye"] as string,
                    PatientId = reader["patient_id"] as string,
                    PatientName = reader["patient_name"] as string,
                    PatientOrgNo = reader["patient_org_no"] as string,
                    ReportClinicalDiag = reader["report_clinial_diag"] as string,
                    ReportDoctName = reader["report_doct_name"] as string,
                    ReportDoctNo = reader["report_doct_no"] as string,
                    RoomNo = reader["room_no"] as string,
                    SpeInspectMark = reader["spe_inspect_mark"] as string,
                    SurgFrPathDiagCode = reader["surgfr_pathdiag_code"] as string,
                    SurgFrPathDiagName = reader["surgfr_pathdiag_name"] as string,
                    SymptomCode = reader["symptom_code"] as string,
                    SymptomDescription = reader["symptom_descri"] as string,
                    SymptomName = reader["symptom_name"] as string,
                    SymptomStartTime = reader["symptom_start_time"] as string,
                    SymptomStopTime = reader["symptom_stop_time"] as string,
                    UnderPatholSee = reader["under_pathol_see"] as string,
                    UpdateTime = reader["update_time"] as string,
                    UploadStatusMark = reader["upload_status_mark"] as string,
                    WardName = reader["ward_name"] as string,
                };
                results.Add(record);
                recordCount++;
            }
            // 记录查询结果数量
            await LogToFileAsync(logPath, $"查询完成，共查询到 {recordCount} 条记录");
            await LogToFileAsync(logPath, $"=== 查询结束 ===");
            return results;
        }
        private async Task<List<T_CHECK_REC_oracle>> GetCheckRecDetailsAsyncReminder(
    OracleConnection connection,
    string? IdCard,
    string? reportId,
    string? checktReportNo,
    int days = 31)  // 新增days参数
        {

            // 记录实际执行的SQL和参数
    var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "检查SQL调试GetCheckRecDetailsAsyncReminder使用范围查阅模式上传提醒记录");
    var logPath = Path.Combine(logDir, $"sql_debug_{DateTime.Now:yyyyMMdd}.log");
    
    // 开始分隔线
    var separator = "=============================================";
    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    Directory.CreateDirectory(logDir);
    await LogToFileAsync(logPath, $"\n{separator}");
    await LogToFileAsync(logPath, $"查询开始时间: {timestamp}");
    await LogToFileAsync(logPath, $"{separator}");
    await LogToFileAsync(logPath, $"【函数】GetCheckRecDetailsAsyncReminder");
    await LogToFileAsync(logPath, $"【用途】查阅模式，上传提醒记录");
    await LogToFileAsync(logPath, $"{separator}");

    var sql = @"
SELECT * 
FROM (
    SELECT T.*, t.rowid
    FROM t_check_rec T 
    WHERE 1=1 ";

    if (!string.IsNullOrEmpty(IdCard))
        sql += " AND T.id_card_value = :IdCard";

    if (!string.IsNullOrEmpty(reportId))
        sql += " AND T.ID = :reportId";

    if (!string.IsNullOrEmpty(checktReportNo))
        sql += " AND T.check_report_no = :checktReportNo";

    // 优化后的日期条件 - 将days参数直接嵌入SQL中
    sql += @"
    AND T.check_report_date IS NOT NULL
    AND (
        -- 格式1: ISO格式 (2025-06-22T09:06:26Z)
        (T.check_report_date LIKE '%T%Z' 
         AND TO_DATE(
             REPLACE(REPLACE(T.check_report_date, 'T', ' '), 'Z', ''), 
             'YYYY-MM-DD HH24:MI:SS'
         ) >= SYSDATE - NUMTODSINTERVAL(" + days + @", 'DAY'))
        OR
        -- 格式2: 空格分隔格式
        (REGEXP_LIKE(T.check_report_date, '^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$')
         AND TO_DATE(T.check_report_date, 'YYYY-MM-DD HH24:MI:SS') 
             >= SYSDATE - NUMTODSINTERVAL(" + days + @", 'DAY'))
    )";

    // 排序
    sql += @"
    ORDER BY 
        CASE 
            WHEN T.check_report_date LIKE '%T%Z' THEN 
                TO_DATE(REPLACE(REPLACE(T.check_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS')
            WHEN REGEXP_LIKE(T.check_report_date, '^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$') THEN
                TO_DATE(T.check_report_date, 'YYYY-MM-DD HH24:MI:SS')
            ELSE NULL
        END DESC NULLS LAST
)";
    
    await LogToFileAsync(logPath, $"【查询SQL】: {sql}");
    await LogToFileAsync(logPath, $"【查询参数】");
    await LogToFileAsync(logPath, $"  - IdCard: {IdCard ?? "空"}");
    await LogToFileAsync(logPath, $"  - reportId: {reportId ?? "空"}");
    await LogToFileAsync(logPath, $"  - checktReportNo: {checktReportNo ?? "空"}");
    await LogToFileAsync(logPath, $"  - days: {days}");
    await LogToFileAsync(logPath, $"{separator}");
    
    using var cmd = new OracleCommand(sql, connection);

    // 绑定其他参数，但不绑定days（因为days已经嵌入SQL）
    if (!string.IsNullOrEmpty(IdCard))
        cmd.Parameters.Add("IdCard", OracleDbType.Varchar2).Value = IdCard;

    if (!string.IsNullOrEmpty(reportId))
        cmd.Parameters.Add("reportId", OracleDbType.Varchar2).Value = reportId;

    if (!string.IsNullOrEmpty(checktReportNo))
        cmd.Parameters.Add("checktReportNo", OracleDbType.Varchar2).Value = checktReportNo;

    try
    {
        // 构建包含实际参数值的SQL用于调试
        var debugSql = cmd.CommandText;
        foreach (OracleParameter param in cmd.Parameters)
        {
            var paramName = param.ParameterName;
            var paramValue = param.Value?.ToString() ?? "NULL";
            debugSql = debugSql.Replace(":" + paramName, $"'{paramValue.Replace("'", "''")}'");
        }
        await LogToFileAsync(logPath, $"【完整SQL（参数已替换）】");
        await LogToFileAsync(logPath, debugSql);
        await LogToFileAsync(logPath, $"{separator}");
    }
    catch (Exception ex)
    {
        await LogToFileAsync(logPath, $"【错误】构建调试SQL时出错: {ex.Message}");
        await LogToFileAsync(logPath, $"{separator}");
    }

    var results = new List<T_CHECK_REC_oracle>();
    int recordCount = 0;
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var record = new T_CHECK_REC_oracle
                {
                    Tid = reader["TID"] as string,
                    ApplyTime = reader["apply_time"] as string,
                    ApplDoctName = reader["appl_doct_name"] as string,
                    ApplDoctNo = reader["appl_doct_no"] as string,
                    AuditDoctName = reader["audit_doct_name"] as string,
                    AuditDoctNo = reader["audit_doct_no"] as string,
                    BedNo = reader["bed_no"] as string,
                    BiopsySite = reader["biopsy_site"] as string,
                    BirthDate = reader["birth_date"] as string,
                    BusinessGenerTime = reader["business_gener_time"] as string,
                    BusinessNo = reader["business_no"] as string,
                    CheckApplyDepartName = reader["check_apply_depart_name"] as string,
                    CheckApplyDepartNameExp = reader["check_apply_depart_name_exp"] as string,
                    CheckApplyDepartNo = reader["check_apply_depart_no"] as string,
                    CheckApplyDepartNoExp = reader["check_apply_depart_no_exp"] as string,
                    CheckApplyOrgName = reader["check_apply_org_name"] as string,
                    CheckAuditTime = reader["check_audit_time"] as string,
                    CheckDoctName = reader["check_doct_name"] as string,
                    CheckDoctNo = reader["check_doct_no"] as string,
                    CheckEquipModel = reader["check_equip_model"] as string,
                    CheckEquipNum = reader["check_equip_num"] as string,
                    CheckIndexResult = reader["check_index_result"] as string,
                    CheckIndexUnit = reader["check_index_uint"] as string,
                    CheckMethodName = reader["check_methed_name"] as string,
                    CheckNormalRefQual = reader["check_normal_ref_qual"] as string,
                    CheckPartCode = reader["check_part_code"] as string,
                    CheckPartName = reader["check_part_name"] as string,
                    CheckProjCode = reader["check_proj_code"] as string,
                    CheckProjCodeExp = reader["check_proj_code_exp"] as string,
                    CheckProjName = reader["check_proj_name"] as string,
                    CheckProjNameExp = reader["check_proj_name_exp"] as string,
                    CheckRecNo = reader["check_rec_no"] as string,
                    CheckReportComment = reader["check_report_comment"] as string,
                    CheckReportDate = reader["check_report_date"] as string,
                    CheckReportDepartCode = reader["check_report_depart_code"] as string,
                    CheckReportDepartCodeExp = reader["check_report_depart_code_exp"] as string,
                    CheckReportDepartName = reader["check_report_depart_name"] as string,
                    CheckReportDepartNameExp = reader["check_report_depart_name_exp"] as string,
                    CheckReportNo = reader["check_report_no"] as string,
                    CheckReportOrgName = reader["check_report_org_name"] as string,
                    CheckRes = reader["check_res"] as string,
                    CheckResCode = reader["check_res_code"] as string,
                    CheckResName = reader["check_res_name"] as string,
                    CheckResObj = reader["check_res_obj"] as string,
                    CheckResQual = reader["check_res_qual"] as string,
                    CheckResSub = reader["check_res_sub"] as string,
                    CheckTypeCode = reader["check_type_code"] as string,
                    CheckTypeName = reader["check_type_name"] as string,
                    CriticalState = reader["critical_state"] as string,
                    DataNum = reader["data_num"] as long?,
                    DataCode = reader["dataCode"] as string,
                    DataName = reader["dataName"] as string,
                    DiagNo = reader["diag_no"] as string,
                    DiagTypeCode = reader["diag_type_code"] as string,
                    DiagTypeName = reader["diag_type_name"] as string,
                    Frozen = reader["frozen"] as string,
                    GatewayName = reader["gateway_name"] as string,
                    GenderCode = reader["gender_code"] as string,
                    GenderName = reader["gender_name"] as string,
                    HealthECode = reader["health_e_code"] as string,
                    HospitalNo = reader["hospital_no"] as string,
                    Id = reader["ID"] as string,
                    IdCardTypeCode = reader["id_card_type_code"] as string,
                    IdCardTypeName = reader["id_card_type_name"] as string,
                    IdCardValue = reader["id_card_value"] as string,
                    ImageExistMark = reader["image_exist_mark"] as string,
                    ImageNo = reader["image_no"] as string,
                    ImageUidAddr = reader["image_uid_addr"] as string,
                    ImmuNumber = reader["immu_number"] as string,
                    InsertDatacenterTime = reader["insert_datacenter_time"] as string,
                    InsertDatetime = reader["INSERT_DATETIME"] as string,
                    InstockTime = reader["INSTOCK_TIME"] as string,
                    InstrumentName = reader["instrument_name"] as string,
                    MtuRecLimitMark = reader["mtuRecLimitMark"] as string,
                    MtuRecMark = reader["mtuRecMark"] as string,
                    NormalRefLowerLimit = reader["normal_ref_lower_limit"] as string,
                    NormalRefUpperLimit = reader["normal_ref_upper_limit"] as string,
                    OrderRecFormNo = reader["order_rec_form_no"] as string,
                    OrgCode = reader["org_code"] as string,
                    OrgName = reader["org_name"] as string,
                    PatNo = reader["pat_no"] as string,
                    PathologicalNakedEye = reader["pathological_naked_eye"] as string,
                    PatientId = reader["patient_id"] as string,
                    PatientName = reader["patient_name"] as string,
                    PatientOrgNo = reader["patient_org_no"] as string,
                    ReportClinicalDiag = reader["report_clinial_diag"] as string,
                    ReportDoctName = reader["report_doct_name"] as string,
                    ReportDoctNo = reader["report_doct_no"] as string,
                    RoomNo = reader["room_no"] as string,
                    SpeInspectMark = reader["spe_inspect_mark"] as string,
                    SurgFrPathDiagCode = reader["surgfr_pathdiag_code"] as string,
                    SurgFrPathDiagName = reader["surgfr_pathdiag_name"] as string,
                    SymptomCode = reader["symptom_code"] as string,
                    SymptomDescription = reader["symptom_descri"] as string,
                    SymptomName = reader["symptom_name"] as string,
                    SymptomStartTime = reader["symptom_start_time"] as string,
                    SymptomStopTime = reader["symptom_stop_time"] as string,
                    UnderPatholSee = reader["under_pathol_see"] as string,
                    UpdateTime = reader["update_time"] as string,
                    UploadStatusMark = reader["upload_status_mark"] as string,
                    WardName = reader["ward_name"] as string,
                };
                results.Add(record);
                recordCount++;
            }
            // 记录查询结果数量
            await LogToFileAsync(logPath, $"查询完成，共查询到 {recordCount} 条记录");
            await LogToFileAsync(logPath, $"=== 查询结束 ===");
            return results;
        }
        /// <summary>
        /// 获取微生物检验及药敏结果(Oracle版本)
        /// </summary>
        public async Task<Test_Detail_oracle> GetCombinedMicrobialReportAsync(string businessNo, string reportId)
        {
            var detail = new Test_Detail_oracle();
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            // 1. 查询报告基本信息
            detail.Reports_TEST_REC = await GetTestRecDetailsAsync(connection,null, reportId, businessNo);

            // 2. 查询微生物结果
            detail.Report_TMICROBE_BACTERIA_RES = await GetMicrobeBacteriaResultsAsync(connection,reportId, businessNo);

            // 3. 查询药敏结果
            detail.Report_TMICROBE_SUSCEPT_RES = await GetMicrobeSusceptResultsAsync(connection, reportId, businessNo);

            return detail;
        }

        

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<Person>> GetAllPersonsAsync()
        {
            var persons = new List<Person>();

            await using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand("SELECT * FROM 人员表", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            persons.Add(new Person
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                Name = reader["姓名"].ToString()
                            });
                        }
                    }
                }
            }

            return persons;
        }
        public async Task<List<Test_Detail>> GetDetailsAsync(string ReportID,string  ID)
        {
            var details = new List<Test_Detail>();
            await using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                var testRecs = new List<T_TEST_REC>();
                var testrResIndicates = new List<T_testr_res_indicate>();//TO_DATE(t.BIRTH_DATE, 'DD-MON-YY')
                var TMICROBEBACTERIARES = new List<T_MICROBE_BACTERIA_RES>();
                details.Add(new Test_Detail
                {
                    Reports_TEST_REC = await GetTestReportHead(ReportID, ID),
                    Report_testr_res_indicate = await GetTestReport(ReportID, ID),
                    Report_TMICROBE_BACTERIA_RES = await GetBacterialCultureResult(ReportID, ID),
                    Report_TMICROBE_SUSCEPT_RES = await GenerateDrugSensitivityReport(ReportID, ID)
                });
            }

            return details;
        }
        /// <summary>
        /// 获取检验列表
        /// </summary>
        /// <returns></returns>
        public async Task<List <T_TEST_REC>> GetAllPersonsListAsync(string UserId)
        {
            var persons = new List<T_TEST_REC>();

            await using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand("select   TO_DATE(t.Test_Rec_Time, 'DD-MON-RR') as Test_Rec_Time,t.*  from  t_test_rec t where t.ID_CARD_VALUE=:UserId", connection))
                {
                    command.Parameters.Add(":UserId", OracleDbType.Varchar2).Value = UserId;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            persons.Add(new T_TEST_REC
                            {
                                TestApplyOrgName = reader["Test_Apply_Org_Name"].ToString(),
                                //TestProjCategoryName = reader["Test_Proj_Category_Name"].ToString(),
                                TestRecTime = Convert.ToDateTime(reader["Test_Rec_Time"]),
                                TestReportNo = reader["TEST_REPORT_NO"].ToString(),
                                Id = reader["ID"].ToString(),
                        });
                        }
                    }
                }
            }

            return persons;
        }
        /// <summary>
        /// 获取病人信息
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public async Task<List<PatientInfo>> GetPatientDetailsAsync(string UserId)
        {
            var persons = new List<PatientInfo>();

            await using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand("select  *  from  病人信息 H where  H.病人ID=:UserId", connection))
                {
                    command.Parameters.Add(":UserId", OracleDbType.Varchar2).Value = UserId;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            persons.Add(new PatientInfo
                            {
                                Name = reader["姓名"].ToString(),
                                Gender = reader["性别"].ToString(),
                                IdNumber = reader["身份证号"].ToString(),
                                MobileNumber = reader["手机号"].ToString(),
                                HomeAddress = reader["家庭地址"].ToString(),
                                BirthDate = DataReaderExtensions.SafeGetDateTime(reader["出生日期"], DateTime.MinValue)
                            });
                        }
                    }
                }
            }

            return persons;
        }
        
        /// <summary>
        /// 获取检验明细
        /// </summary>
        /// <returns></returns>
        public async Task<List<T_testr_res_indicate>> GetAllPersonsDetailtAsync(string ReportID)
        {
            var persons = new List<T_testr_res_indicate>();

            await using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand("select   *  from  T_TESTR_RES_INDICATE ", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            persons.Add(new T_testr_res_indicate
                            {
                                TestProjNameExp = reader["Test_Proj_Name_Exp"].ToString(),
                                TestIndexResult = reader["test_index_result"].ToString(),
                                //TestIndexUint = reader["test_index_uint"].ToString(),
                                NormalRefLimit = reader["normal_ref_limit"].ToString(),
                                AnomalyCode = reader["anomaly_code"].ToString()
                            });
                        }
                    }
                }
            }

            return persons;
        }
        /// <summary>
        /// 获取微生物检验报告
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public async Task<List<T_MICROBE_BACTERIA_RES>> GetBacterialCultureResult(string ReportID, string ID)
        {
            var TMICROBE_BACTERIA = new List<T_MICROBE_BACTERIA_RES>();
            string strSql = " select   t.ORG_NAME, T.TEST_PROJ_CATEGORY_NAME, t.PATIENT_NAME, t.GENDER_NAME, TO_DATE(t.BIRTH_DATE, 'DD-MON-YY') as BIRTH_DATE, t.PAT_NO, t.TEST_APPLY_DEPART_NAME_EXP, t.bed_no, t.report_clinial_diag," +
                                " a.test_board_name,a.test_board_number,a.bacteria_name, a.test_result,a.INCUBATION_TIME, '', '', '', '', 1 as ASTID   from t_test_rec T , t_microbe_bacteria_res A where   t.test_report_no = A.test_report_no and   A.test_report_no = :ReportID and t.id = :ID";
            await using (var connection =new  OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                using ( var command = new OracleCommand(strSql, connection)) 
                {
                    command.Parameters.Add(":reportNo", OracleDbType.Varchar2).Value = ReportID;
                    command.Parameters.Add(":ID", OracleDbType.Varchar2).Value = ID;
                    using ( var reader = await command.ExecuteReaderAsync())
                    { 
                        while (await reader.ReadAsync())
                        {
                            TMICROBE_BACTERIA.Add(new T_MICROBE_BACTERIA_RES()
                            {
                                BacteriaName = reader["bacteria_name"].ToString(),
                                TestResult = reader["test_result"].ToString(),
                                IncubationTime = reader["INCUBATION_TIME"].ToString(),
                                TestBoardNumber = reader["test_board_number"].ToString(),
                                TestBoardName = reader["test_board_name"].ToString()
                                //ASTID = reader["ASTID"].ToString()
                                //BacteriaId = reader["BACTERIA_ID"].ToString(),
                                //BacteriaResultDescr = reader["BACTERIA_RESULT_DESCR"].ToString(),
                                //AnomalyCode = reader["anomaly_code"].ToString()
                            });
                        }
                    }
                }
            }
            return TMICROBE_BACTERIA;
        }
        /// <summary>
        /// 获取检验报告头
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public async Task<List<T_TEST_REC>> GetTestReportHead(string ReportID, string ID)
        {
            var TMICROBE_BACTERIA = new List<T_TEST_REC>();
            string StrSql = "select   TO_DATE(t.sample_time, 'DD-MON-YY') as sample_time,t.AUDIT_DOCT_NAME,TO_DATE(t.TEST_AUDIT_TIME, 'DD-MON-YY') as TEST_AUDIT_TIME,TO_DATE(t.TEST_REPORT_DATE, 'DD-MON-YY') as TEST_REPORT_DATE,t.TEST_DOCT_NAME,t.ORG_NAME,T.TEST_PROJ_CATEGORY_NAME,t.PATIENT_NAME,t.GENDER_NAME,TO_DATE(t.BIRTH_DATE, 'DD-MON-YY') as BIRTH_DATE,t.PAT_NO,t.TEST_APPLY_DEPART_NAME_EXP,t.bed_no,t.report_clinial_diag," +
" 0 as ASTID   from t_test_rec T  where  t.id = :ID";
            await using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new OracleCommand(StrSql, connection))
                {
                    //command.Parameters.Add(":reportNo", OracleDbType.Varchar2).Value = ReportID;
                    command.Parameters.Add(":ID", OracleDbType.Varchar2).Value = ID;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            TMICROBE_BACTERIA.Add(new T_TEST_REC()
                            {
                                OrgName = reader["ORG_NAME"].ToString(),//机构名称
                                //TestProjCategoryName = reader["TEST_PROJ_CATEGORY_NAME"].ToString(),//检验项目大项代码
                                PatientName = reader["PATIENT_NAME"].ToString(),
                                GenderName = reader["GENDER_NAME"].ToString(),
                                BirthDate = Convert.ToDateTime(reader["BIRTH_DATE"].ToString()),
                                PatNo = reader["PAT_NO"].ToString(),
                                TestApplyDepartNameExp = reader["TEST_APPLY_DEPART_NAME_EXP"].ToString(),
                                BedNo = reader["bed_no"].ToString(),
                                ReportClinialDiag = reader["report_clinial_diag"].ToString(),
                                TestDoctName= reader["TEST_DOCT_NAME"].ToString(),
                                AuditDoctName = reader["AUDIT_DOCT_NAME"] == DBNull.Value? string.Empty: reader["AUDIT_DOCT_NAME"].ToString(),
                                //TestAuditTime = reader["TEST_AUDIT_TIME"] == DBNull.Value ? string.Empty : reader["TEST_AUDIT_TIME"].ToString(),
                                SampleTime = DataReaderExtensions.SafeGetDateTime(reader["sample_time"], DateTime.MinValue),
                                TestAuditTime = DataReaderExtensions.SafeGetDateTime(reader["test_audit_time"], DateTime.MinValue),
                                TestReportDate = DataReaderExtensions.SafeGetDateTime(reader["TEST_REPORT_DATE"], DateTime.MinValue)
                            });
                        }
                    }
                }
            }
            return TMICROBE_BACTERIA;
        }
        /// <summary>
        /// 获取普通检验报告
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public async Task<List<T_testr_res_indicate>> GetTestReport(string ReportID, string ID)
        {
            var TMICROBE_BACTERIA = new List<T_testr_res_indicate>();
            string StrSql = "select  t.ORG_NAME,T.TEST_PROJ_CATEGORY_NAME,t.PATIENT_NAME,t.GENDER_NAME,TO_DATE(t.BIRTH_DATE, 'DD-MON-YY') as BIRTH_DATE,t.PAT_NO,t.TEST_APPLY_DEPART_NAME_EXP,t.bed_no,t.report_clinial_diag," +
"a.Test_Proj_Name_Exp, a.test_index_result, a.test_index_uint, a.normal_ref_limit, a.anomaly_code, '', 0 as ASTID   from t_test_rec T , T_TESTR_RES_INDICATE A where   t.test_report_no = A.test_report_no and   A.test_report_no = :ReportID and  t.id = :ID";
            await using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new OracleCommand(StrSql, connection))
                {
                    command.Parameters.Add(":reportNo", OracleDbType.Varchar2).Value = ReportID;
                    command.Parameters.Add(":ID", OracleDbType.Varchar2).Value = ID;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            TMICROBE_BACTERIA.Add(new T_testr_res_indicate()
                            {
                                TestProjNameExp = reader["Test_Proj_Name_Exp"].ToString(),
                                                                TestIndexResult = reader["test_index_result"].ToString(),
                                                                //TestIndexUint = reader["test_index_uint"].ToString(),
                                                                NormalRefLimit = reader["normal_ref_limit"].ToString(),
                                                                AnomalyCode = reader["anomaly_code"].ToString(),
                                                                 //ASTID = reader["ASTID"].ToString()
                            });
                        }
                    }
                }
            }
            return TMICROBE_BACTERIA;
        }
        /// <summary>
        /// 获取微生物检验药敏报告 
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public async Task<List<T_MICROBE_SUSCEPT_RES>> GenerateDrugSensitivityReport(string ReportID, string ID)
        {
            var TMICROBE_BACTERIA = new List<T_MICROBE_SUSCEPT_RES>();
            string StrSql = "select   t.ORG_NAME, T.TEST_PROJ_CATEGORY_NAME, t.PATIENT_NAME, t.GENDER_NAME, TO_DATE(t.BIRTH_DATE, 'DD-MON-YY') as BIRTH_DATE, t.PAT_NO, t.TEST_APPLY_DEPART_NAME_EXP, t.bed_no, t.report_clinial_diag," +
" a.bacteria_name,a.SUSCEPT_RESULT_DESCR,a.DRUG_SUSCEPTIBIBLE_CODE,a.bacteriostatic_concentrate, a.DRUG_SUSCEPTIBIBLE_NAME, a.DRUG_SUSCEPTIBLE_NAME, a.expert_rule, a.suscept_result_descr, 2 as ASTID from t_test_rec T , t_microbe_suscept_res A where   t.test_report_no = A.test_report_no and   A.test_report_no = :ReportID and t.id = :ID";
            await using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new OracleCommand(StrSql, connection))
                {
                    command.Parameters.Add(":reportNo", OracleDbType.Varchar2).Value = ReportID;
                    command.Parameters.Add(":ID", OracleDbType.Varchar2).Value = ID;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            TMICROBE_BACTERIA.Add(new T_MICROBE_SUSCEPT_RES()
                            {
                                BacteriaName = reader["bacteria_name"].ToString(),
                                //SusceptResultDescr = reader["SUSCEPT_RESULT_DESCR"].ToString(),
                                BacteriostaticConcentrate = reader["bacteriostatic_concentrate"].ToString(),
                                //DrugSusceptibibleCode = reader["DRUG_SUSCEPTIBIBLE_CODE"].ToString(),
                                //DrugSusceptibibleName=reader["DRUG_SUSCEPTIBIBLE_NAME"].ToString(),
                                DrugSusceptibleName = reader["DRUG_SUSCEPTIBLE_NAME"].ToString()
                            });
                        }
                    }
                }
            }
            return TMICROBE_BACTERIA;
        }
        public async Task<RecognitionRecord> GetRecognitionStatusAsync(string patientId, string reportId,string doctorId)
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "SELECT * FROM T_REPORT_RECOGNITION WHERE PATIENT_ID = :patientId AND REPORT_ID = :reportId and doctorId=:doctorId";

            return await connection.QueryFirstOrDefaultAsync<RecognitionRecord>(sql, new
            {
                patientId,
                reportId,
                doctorId
            });
        }
        public async Task<RecognitionRecord> GetSpcmbcNo(int ApplyID)
        {
            // 记录实际执行的SQL和参数
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "SQL调试");
            var logPath = Path.Combine(logDir, $"sql_debug_{DateTime.Now:yyyyMMdd}.log");
            Directory.CreateDirectory(logDir);
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = "SELECT T.样本条码 as SpcmbcNo FROM 检验申请组合 T WHERE T.申请ID = :ApplyID";

            await LogToFileAsync(logPath, $"检验项目查询SQL: {sql}");
            //await LogToFileAsync(logPath, $"参数: mutualCode='{mutualCode}', treatmentCode='{treatmentCode}'");
            await LogToFileAsync(logPath, $"参数: ApplyID:{ApplyID} ");
            return await connection.QueryFirstOrDefaultAsync<RecognitionRecord>(sql, new
            {
                ApplyID
            });
        }

        public async Task<int> SaveRecognitionRecordAsync(RecognitionRecord record)
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
    MERGE INTO T_REPORT_RECOGNITION t
    USING (SELECT :REPORT_ID AS REPORT_ID, :PATIENT_ID AS PATIENT_ID,:DoctorId as DoctorId FROM dual) s
    ON (t.REPORT_ID = s.REPORT_ID AND t.PATIENT_ID = s.PATIENT_ID AND t.doctorId=s.DoctorId)
    WHEN MATCHED THEN
        UPDATE SET 
            t.GH_ID = :GH_ID,
            t.RECOGNITION_STATUS = :RECOGNITION_STATUS,
            t.RECOGNITION_TIME = SYSDATE,
            t.REQUEST_ID = :REQUEST_ID,
            t.EXTERNAL_CODE = :EXTERNAL_CODE,
            t.EXTERNAL_MSG = :EXTERNAL_MSG,
            t.RECOGNITION_RECORD_ID = NVL(:RECOGNITION_RECORD_ID, t.RECOGNITION_RECORD_ID),
            t.UPDATE_TIME = SYSDATE,
            t.REPORT_TYPE = :REPORT_TYPE,
            t.OrderId=OrderId
    WHEN NOT MATCHED THEN
        INSERT (ID, REPORT_ID, PATIENT_ID, GH_ID, RECOGNITION_STATUS, RECOGNITION_TIME, 
                REQUEST_ID, EXTERNAL_CODE, EXTERNAL_MSG, RECOGNITION_RECORD_ID, 
                CREATE_TIME, UPDATE_TIME, REPORT_TYPE,doctorId,OrderId)
        VALUES (SYS_GUID(), :REPORT_ID, :PATIENT_ID, :GH_ID, :RECOGNITION_STATUS, SYSDATE, 
                :REQUEST_ID, :EXTERNAL_CODE, :EXTERNAL_MSG, :RECOGNITION_RECORD_ID, 
                SYSDATE, SYSDATE, :REPORT_TYPE,:DoctorId,:OrderId)";

            return await connection.ExecuteAsync(sql, record);
        }
        //    public async Task<int> SaveRecognitionRecordAsyncUploadQuoteLog(RecognitionRecord record)
        //    {
        //        await using var connection = new OracleConnection(_connectionString);
        //        await connection.OpenAsync();

        //        const string sql = @"
        //MERGE INTO T_REPORT_RECOGNITION t
        //USING (SELECT :REPORT_ID AS REPORT_ID, :PATIENT_ID AS PATIENT_ID,:DoctorId as DoctorId FROM dual) s
        //ON (t.REPORT_ID = s.REPORT_ID AND t.PATIENT_ID = s.PATIENT_ID AND t.doctorId=s.DoctorId)
        //WHEN MATCHED THEN
        //    UPDATE SET 
        //        t.GH_ID = :GH_ID,
        //        t.RECOGNITION_TIME = SYSDATE,
        //        t.REQUEST_ID = :REQUEST_ID,
        //        t.EXTERNAL_CODE = :EXTERNAL_CODE,
        //        t.EXTERNAL_MSG = :EXTERNAL_MSG,
        //        t.RECOGNITION_RECORD_ID = NVL(:RECOGNITION_RECORD_ID, t.RECOGNITION_RECORD_ID),
        //        t.UPDATE_TIME = SYSDATE,
        //        t.REPORT_TYPE = :REPORT_TYPE,
        //        t.OrderId=OrderId
        //WHEN NOT MATCHED THEN
        //    INSERT (ID, REPORT_ID, PATIENT_ID, GH_ID, RECOGNITION_TIME, 
        //            REQUEST_ID, EXTERNAL_CODE, EXTERNAL_MSG, RECOGNITION_RECORD_ID, 
        //            CREATE_TIME, UPDATE_TIME, REPORT_TYPE,doctorId,OrderId)
        //    VALUES (SYS_GUID(), :REPORT_ID, :PATIENT_ID, :GH_ID, SYSDATE, 
        //            :REQUEST_ID, :EXTERNAL_CODE, :EXTERNAL_MSG, :RECOGNITION_RECORD_ID, 
        //            SYSDATE, SYSDATE, :REPORT_TYPE,:DoctorId,:OrderId)";

        //        return await connection.ExecuteAsync(sql, record);
        //    }
        public async Task<int> SaveRecognitionRecordAsyncUploadQuoteLog(RecognitionRecord record)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "保存引用记录日志");
            var logPath = Path.Combine(logDir, $"save_recognition_{DateTime.Now:yyyyMMdd}.log");
            Directory.CreateDirectory(logDir);

            await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始保存单条识别记录");
            await LogToFileAsync(logPath, $"记录详情: REPORT_ID={record.REPORT_ID}, PATIENT_ID={record.PATIENT_ID}, DoctorId={record.DoctorId}");

            try
            {
                await using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                await LogToFileAsync(logPath, $"数据库连接成功 | 状态: {connection.State} | 数据源: {connection.DataSource}");

                const string sql = @"
MERGE INTO T_REPORT_RECOGNITION t
USING (SELECT :REPORT_ID AS REPORT_ID, :PATIENT_ID AS PATIENT_ID,:DoctorId as DoctorId FROM dual) s
ON (t.REPORT_ID = s.REPORT_ID AND t.PATIENT_ID = s.PATIENT_ID AND t.doctorId=s.DoctorId)
WHEN MATCHED THEN
    UPDATE SET 
        t.GH_ID = :GH_ID,
        t.RECOGNITION_TIME = SYSDATE,
        t.REQUEST_ID = :REQUEST_ID,
        t.EXTERNAL_CODE = :EXTERNAL_CODE,
        t.EXTERNAL_MSG = :EXTERNAL_MSG,
        t.REFERENCE_RECORD_ID = NVL(:REFERENCE_RECORD_ID, t.REFERENCE_RECORD_ID),
        t.UPDATE_TIME = SYSDATE,
        t.REPORT_TYPE = :REPORT_TYPE,
        t.OrderId=:OrderId
WHEN NOT MATCHED THEN
    INSERT (ID, REPORT_ID, PATIENT_ID, GH_ID, RECOGNITION_TIME, 
            REQUEST_ID, EXTERNAL_CODE, EXTERNAL_MSG, REFERENCE_RECORD_ID, 
            CREATE_TIME, UPDATE_TIME, REPORT_TYPE,doctorId,OrderId)
    VALUES (SYS_GUID(), :REPORT_ID, :PATIENT_ID, :GH_ID, SYSDATE, 
            :REQUEST_ID, :EXTERNAL_CODE, :EXTERNAL_MSG, :REFERENCE_RECORD_ID, 
            SYSDATE, SYSDATE, :REPORT_TYPE,:DoctorId,:OrderId)";

                // 记录SQL语句
                await LogToFileAsync(logPath, $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                await LogToFileAsync(logPath, $"执行SQL语句:\n{sql}");

                // 记录参数详情
                await LogToFileAsync(logPath, $"参数详情:");
                await LogToFileAsync(logPath, $"  - REPORT_ID: {record.REPORT_ID}");
                await LogToFileAsync(logPath, $"  - PATIENT_ID: {record.PATIENT_ID}");
                await LogToFileAsync(logPath, $"  - DoctorId: {record.DoctorId}");
                await LogToFileAsync(logPath, $"  - GH_ID: {record.GH_ID}");
                await LogToFileAsync(logPath, $"  - REQUEST_ID: {record.REQUEST_ID}");
                await LogToFileAsync(logPath, $"  - EXTERNAL_CODE: {record.EXTERNAL_CODE}");
                await LogToFileAsync(logPath, $"  - EXTERNAL_MSG: {record.EXTERNAL_MSG}");
                await LogToFileAsync(logPath, $"  - REFERENCE_RECORD_ID: {record.REFERENCE_RECORD_ID}");
                await LogToFileAsync(logPath, $"  - REPORT_TYPE: {record.REPORT_TYPE}");
                await LogToFileAsync(logPath, $"  - OrderId: {record.OrderId}");

                try
                {
                    // 执行SQL
                    var affectedRows = await connection.ExecuteAsync(sql, record);
                    await LogToFileAsync(logPath, $"SQL执行成功 | 影响行数: {affectedRows}");

                    if (affectedRows > 0)
                    {
                        await LogToFileAsync(logPath, $"操作类型: {(affectedRows == 1 ? "新增记录" : "更新记录")}");
                    }
                    else
                    {
                        await LogToFileAsync(logPath, $"警告: 未影响任何行，可能数据未变化");
                    }

                    await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 保存完成");
                    return affectedRows;
                }
                catch (OracleException oracleEx)
                {
                    await LogToFileAsync(logPath, $"Oracle数据库错误: {oracleEx.Message}");
                    await LogToFileAsync(logPath, $"错误代码: {oracleEx.ErrorCode}");
                    await LogToFileAsync(logPath, $"堆栈跟踪: {oracleEx.StackTrace}");
                    throw;
                }
                catch (Exception ex)
                {
                    await LogToFileAsync(logPath, $"SQL执行错误: {ex.Message}");
                    await LogToFileAsync(logPath, $"堆栈跟踪: {ex.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"数据库连接或操作失败: {ex.Message}");
                await LogToFileAsync(logPath, $"堆栈跟踪: {ex.StackTrace}");
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 保存失败");
                throw;
            }
        }

        public async Task<int> BulkSaveRecognitionRecordsAsync(List<RecognitionRecord> records)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "DatabaseOperations");
            var logPath = Path.Combine(logDir, $"bulk_save_{DateTime.Now:yyyyMMdd}.log");
            Directory.CreateDirectory(logDir);

            await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始批量保存记录 | 总记录数: {records.Count}");

            try
            {
                await using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                await LogToFileAsync(logPath, $"数据库连接成功 | 状态: {connection.State} | 数据源: {connection.DataSource}");

                var affectedRows = 0;
                await using var transaction = await connection.BeginTransactionAsync();
                await LogToFileAsync(logPath, "事务已开启");

                try
                {
                    foreach (var record in records)
                    {
                        // 1. 构建动态参数和SQL
                        var (fields, updates, insertValues, parameters) = BuildDynamicParameters(record);
                        var sql = BuildMergeSql(fields, updates, insertValues);

                        // 记录当前记录的SQL和参数
                        await LogToFileAsync(logPath, $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        await LogToFileAsync(logPath, $"正在处理记录: REPORT_ID={record.REPORT_ID}, PATIENT_ID={record.PATIENT_ID}");
                        await LogToFileAsync(logPath, $"生成的SQL:\n{sql}");
                        await LogToFileAsync(logPath, $"参数值: {FormatParameters(parameters)}");
                        // 在执行SQL前添加参数验证日志
                        await LogToFileAsync(logPath, $"完整参数列表: {string.Join(", ", parameters.ParameterNames)}");
                        // 2. 执行SQL
                        var rowCount = await connection.ExecuteAsync(sql, parameters, transaction);
                        affectedRows += rowCount;

                        await LogToFileAsync(logPath, $"影响行数: {rowCount} | 累计影响行数: {affectedRows}");
                    }

                    await transaction.CommitAsync();
                    await LogToFileAsync(logPath, $"事务已提交 | 总影响行数: {affectedRows}");

                    return affectedRows;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await LogToFileAsync(logPath, $"事务已回滚 | 错误: {ex.Message}\n{ex.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"数据库操作失败: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private string BuildMergeSql(string fields, string updates, string insertValues)
        {
            return $@"
MERGE INTO T_REPORT_RECOGNITION t
USING (
    SELECT 
        :REPORT_ID AS REPORT_ID,
        :PATIENT_ID AS PATIENT_ID,
        :DoctorId as DoctorId
        {fields}
    FROM dual
) s
ON (t.REPORT_ID = s.REPORT_ID AND t.PATIENT_ID = s.PATIENT_ID AND t.DoctorId=s.DoctorId)
WHEN MATCHED THEN
    UPDATE SET 
        {updates}
        t.UPDATE_TIME = SYSDATE
WHEN NOT MATCHED THEN
    INSERT (
        ID, REPORT_ID, PATIENT_ID, GH_ID, 
        RECOGNITION_STATUS, REMINDER_RECORD_ID,RECOGNITION_RECORD_ID,VIEW_RECORD_ID,
        REQUEST_ID, EXTERNAL_CODE, EXTERNAL_MSG,
        CREATE_TIME, UPDATE_TIME, REPORT_TYPE,DoctorId,SearchType,OrderId,VIEW_STATUS
    )
    VALUES (
        SYS_GUID(), s.REPORT_ID, s.PATIENT_ID, s.GH_ID,
        s.RECOGNITION_STATUS, s.REMINDER_RECORD_ID,s.RECOGNITION_RECORD_ID,s.VIEW_RECORD_ID,
        NULL, NULL, NULL,  -- 硬编码的 NULL 值需与列名对应
        SYSDATE, SYSDATE, s.REPORT_TYPE,s.DoctorId,s.SearchType,s.OrderId,s.VIEW_STATUS
    )";
        }

        private (string fields, string updates, string insertValues, DynamicParameters parameters)
    BuildDynamicParameters(RecognitionRecord record)
        {
            var fields = new StringBuilder();
            var updates = new StringBuilder();
            var insertValues = new StringBuilder();
            var parameters = new DynamicParameters();

            // 固定参数（必须绑定的参数）
            parameters.Add("REPORT_ID", record.REPORT_ID, DbType.String);
            parameters.Add("PATIENT_ID", record.PATIENT_ID, DbType.String);
            parameters.Add("DoctorId", record.DoctorId, DbType.String);

            // 动态处理字段（确保所有SQL中引用的参数都被绑定）
            void ProcessField(string fieldName, object value, string dbType, bool includeInUpdate = true)
            {
                // 确保字段出现在USING子句
                fields.Append($", NVL(:{fieldName}, NULL) AS {fieldName}");

                // 关键修改：即使值为null也绑定参数
                parameters.Add(fieldName, value ?? (object)DBNull.Value, GetDbType(dbType));

                if (value != null && includeInUpdate)
                {
                    updates.Append($"t.{fieldName} = s.{fieldName},");
                }

                insertValues.Append(value != null ? $"s.{fieldName}," : "NULL,");
            }

            // 生成INSERT固定部分
            insertValues.Append("SYS_GUID(), s.REPORT_ID, s.PATIENT_ID,s.DoctorId,");

            // 处理所有可能为null的字段
            ProcessField("GH_ID", record.GH_ID, "String");
            // 关键修改：根据SearchType决定是否更新RECOGNITION_STATUS
            bool shouldUpdateRecognitionStatus = record.SearchType != "1";
            ProcessField("RECOGNITION_STATUS", record.RECOGNITION_STATUS, "String", shouldUpdateRecognitionStatus);

            //ProcessField("RECOGNITION_STATUS", record.RECOGNITION_STATUS, "String");
            ProcessField("REMINDER_RECORD_ID", record.REMINDER_RECORD_ID, "String");
            ProcessField("RECOGNITION_RECORD_ID", record.RECOGNITION_RECORD_ID, "String");
            ProcessField("VIEW_RECORD_ID", record.VIEW_RECORD_ID, "String");
            insertValues.Append("NULL, NULL, NULL,");  // 硬编码字段
            insertValues.Append("SYSDATE, SYSDATE,");
            ProcessField("REPORT_TYPE", record.REPORT_TYPE, "String");
            ProcessField("SearchType", record.SearchType, "String");
            ProcessField("OrderId", record.OrderId, "String"); 
            ProcessField("VIEW_STATUS", record.VIEW_STATUS, "String");
            return (fields.ToString(), updates.ToString(), insertValues.ToString(), parameters);
        }

        private DbType GetDbType(string typeName) => typeName switch
        {
            "Int32" => DbType.Int32,
            "DateTime" => DbType.DateTime2,
            _ => DbType.String
        };

        private string FormatParameters(DynamicParameters parameters)
        {
            var sb = new StringBuilder();
            foreach (var name in parameters.ParameterNames)
            {
                sb.AppendLine($"{name} = {parameters.Get<object>(name) ?? "NULL"}");
            }
            return sb.ToString();
        }

        //private async Task LogToFileAsync(string path, string content)
        //{
        //    try
        //    {
        //        await File.AppendAllTextAsync(path, content + Environment.NewLine);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"无法写入日志文件: {ex.Message}");
        //    }
        //}
        public async Task<(bool success, string message)> SaveRecognitionRecord(
    string logPath,
    ViewRecordRequest request,
    RecognitionResponse? result,
    string finalStatus)
        {
            try
            {
                var recognitionRecord = new RecognitionRecord
                {
                    REPORT_ID = request.ReportId,
                    PATIENT_ID = request.PatientId,
                    GH_ID = request.GHId,
                    RECOGNITION_STATUS = finalStatus,
                    REQUEST_ID = result?.request_id ?? string.Empty,
                    EXTERNAL_CODE = result?.code ?? string.Empty,
                    EXTERNAL_MSG = result?.msg ?? string.Empty
                };

                // 关键修改：获取并验证数据库操作结果
                var rowsAffected = await SaveRecognitionRecordAsync(recognitionRecord);

                // 假设返回受影响行数，需根据实际实现调整
                if (rowsAffected <= 0) // 数据库写入失败
                {
                    await LogToFileAsync(logPath, $"数据库写入失败，受影响行数: {rowsAffected}");
                    return (false, "数据库操作未影响任何行");
                }

                await LogToFileAsync(logPath, $"本地记录保存成功，ID: {recognitionRecord.REPORT_ID}");
                return (true, "保存成功");
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"本地记录保存失败: {ex.Message}");
                return (false, $"保存失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 更新互认记录表中的申请单ID
        /// </summary>
        /// <param name="reportId">报告ID</param>
        /// <param name="applyId">申请单ID</param>
        public async Task<(bool success, string message)> UpdateApplyIdInRecognitionRecord(string reportId, string applyId,string doctorId, string patientId)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs", "DatabaseOperations更新申请ID");
            var logPath = Path.Combine(logDir, $"update_apply_id_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始更新申请ID");
                await LogToFileAsync(logPath, $"报告ID: {reportId}, 申请ID: {applyId},医生ID:{doctorId},病人ID:{patientId}");

                await using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
            UPDATE T_REPORT_RECOGNITION 
            SET apply_id = :applyId, 
                update_time = SYSDATE
            WHERE report_id = :reportId and DOCTORID=:doctorId and PATIENT_ID=:patientId";

                var parameters = new
                {
                    applyId,
                    reportId,
                    doctorId,
                    patientId
                };

                await LogToFileAsync(logPath, $"执行SQL: {sql}");
                await LogToFileAsync(logPath, $"参数: applyId='{applyId}', reportId='{reportId}',doctorId='{doctorId}',patientId='{patientId}'");

                var affectedRows = await connection.ExecuteAsync(sql, parameters);

                await LogToFileAsync(logPath, $"更新完成，影响行数: {affectedRows}");

                if (affectedRows > 0)
                {
                    return (true, "更新成功");
                }
                else
                {
                    await LogToFileAsync(logPath, $"警告: 未找到匹配的记录，报告ID: {reportId}");
                    return (false, $"未找到报告ID为 {reportId} 的记录");
                }
            }
            catch (Exception ex)
            {
                await LogToFileAsync(logPath, $"更新申请ID异常: {ex.Message}\n堆栈: {ex.StackTrace}");
                return (false, $"数据库操作失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 检查报告记录是否存在
        /// </summary>
        public async Task<bool> CheckReportRecognitionExistsAsync(string reportId)
        {
            try
            {
                await using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = "SELECT COUNT(1) FROM T_REPORT_RECOGNITION WHERE report_id = :reportId";

                var count = await connection.ExecuteScalarAsync<int>(sql, new { reportId });
                return count > 0;
            }
            catch (Exception ex)
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs", "DatabaseOperations");
                var logPath = Path.Combine(logDir, $"check_record_{DateTime.Now:yyyyMMdd}.log");
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"检查记录存在性失败: {ex.Message}");
                return false;
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
        /// 

    }

    public static class DataReaderExtensions
    {
        /// <summary>
        /// 时间处理函数
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static DateTime SafeGetDateTime(this object value, DateTime defaultValue = default)
        {
            try
            {
                if (value == null || value == DBNull.Value)
                    return defaultValue;

                if (value is DateTime dateTime)
                    return dateTime;

                if (value is string strValue && DateTime.TryParse(strValue, out var parsedDate))
                    return parsedDate;

                return Convert.ToDateTime(value);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
    public class RecognitionRecord
    {
        public string? ID { get; set; }
        /// <summary>
        /// 报告ID
        /// </summary>
        public string? REPORT_ID { get; set; }
        /// <summary>
        /// 病人ID
        /// </summary>
        public string? PATIENT_ID { get; set; }
        /// <summary>
        /// 挂号id
        /// </summary>
        public string? GH_ID { get; set; }
        /// <summary>
        /// 互认状态：0不互认，1已互认，2未处理
        /// </summary>
        public string? RECOGNITION_STATUS { get; set; }
        /// <summary>
        /// 互认时间
        /// </summary>
        public DateTime? RECOGNITION_TIME { get; set; }
        /// <summary>
        /// 平台系统返回的request_id
        /// </summary>
        public string? REQUEST_ID { get; set; }
        /// <summary>
        /// 平台方系统返回的code
        /// </summary>
        public string? EXTERNAL_CODE { get; set; }
        /// <summary>
        /// 平台方系统返回的msg
        /// </summary>
        public string? EXTERNAL_MSG { get; set; }
        /// <summary>
        /// 提醒记录上传ID
        /// </summary>
        public string? REMINDER_RECORD_ID { get; set; }  // 提醒记录上传ID
        /// <summary>
        /// 不互认记录上传ID
        /// </summary>
        public string? RECOGNITION_RECORD_ID { get; set; }  // 不互认记录上传ID
        /// <summary>
        /// 调阅记录上传ID
        /// </summary>
        public string? VIEW_RECORD_ID { get; set; }  // 调阅记录上传ID
        /// <summary>
        /// 引用记录上传ID
        /// </summary>
        public string? REFERENCE_RECORD_ID { get; set; }  // 引用记录上传ID
        public DateTime CREATE_TIME { get; set; }
        public DateTime UPDATE_TIME { get; set; }
        /// <summary>
        /// 报告类型
        /// </summary>
        public string? REPORT_TYPE { get; set; }
        /// <summary>
        /// 医生ID
        /// </summary>
        public string? DoctorId { get; set; }
        /// <summary>
        /// 调阅类型
        /// </summary>
        public string? SearchType { get; set; }
        /// <summary>
        /// 医嘱ID
        /// </summary>
        public string? OrderId {  get; set; }
        /// <summary>
        /// 查阅状态：0未查阅，1已查阅
        /// </summary>
        public string? VIEW_STATUS {  get; set; }
        
        public string? APPLY_ID { get; set; }

        public string? SpcmbcNo { get; set; }

    }
    public class GLHRMapping
    {
        public string 互认诊疗项目id { get; set; }
        public string 互认诊疗项目名称 { get; set; }
        public string 部位 { get; set; }
        public string 方法 { get; set; }
        public string 标本部位 { get; set; }
        public string 执行科室id { get; set; }
        public string 采集名称 { get; set; }
        
    }
}
