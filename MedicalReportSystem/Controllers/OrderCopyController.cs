using MedicalReportSystem.Models;
using MedicalReportSystem.Models.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace MedicalReportSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderCopyController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<OrderCopyController> _logger;
        private readonly IHostEnvironment _env;
        private readonly ReminderSettings _reminderSettings;

        public OrderCopyController(
            IConfiguration configuration,
            ILogger<OrderCopyController> logger,
            IHostEnvironment env,
            IOptions<ReminderSettings> reminderSettings)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection");
            _logger = logger;
            _env = env;
            _reminderSettings = reminderSettings.Value;
        }

        #region 日志辅助方法
        private async Task LogToFileAsync(string logPath, string message)
        {
            try
            {
                var logDir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                await System.IO.File.AppendAllTextAsync(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}");
            }
            catch { }
        }
        #endregion

        #region 检验报告相关接口

        /// <summary>
        /// 获取患者的检验报告列表
        /// </summary>
        [HttpGet("lab-reports/{idCard}")]
        public async Task<ActionResult<List<T_TEST_REC_oracle>>> GetLabReports(string idCard)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "OrderCopy");
            var logPath = Path.Combine(logDir, $"GetLabReports_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"【开始】获取检验报告列表, IdCard: {idCard}");

                var reports = new List<T_TEST_REC_oracle>();

                int days = _reminderSettings?.DefaultLookbackDays ?? 30;

                var sql = @"
SELECT * 
FROM (
    SELECT T.*, t.rowid
    FROM t_test_rec T ,T_REPORT_RECOGNITION A 
    WHERE 1=1  and T.id=a.report_id
        AND T.mtuRecMark = 1  and RECOGNITION_STATUS='1'
        AND (T.ID_CARD_VALUE = :IdCard OR T.PATIENT_ID = :IdCard)
        AND (
            (T.test_report_date LIKE '%T%Z' 
             AND TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS') >= SYSDATE - NUMTODSINTERVAL(" + days + @", 'DAY'))
            OR
            (T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' 
             AND TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS') >= SYSDATE - NUMTODSINTERVAL(" + days + @", 'DAY'))
        )
    ORDER BY 
        CASE 
            WHEN T.test_report_date LIKE '%T%Z' THEN 
                TO_DATE(REPLACE(REPLACE(T.test_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS')
            WHEN T.test_report_date LIKE '% %' AND T.test_report_date NOT LIKE '%T%' THEN
                TO_DATE(T.test_report_date, 'YYYY-MM-DD HH24:MI:SS')
            ELSE NULL
        END DESC
)";

                using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add(new OracleParameter("IdCard", idCard));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var report = new T_TEST_REC_oracle
                    {
                        Tid = reader["TID"] as string,
                        AcceptSampleTime = reader["ACCEPT_SAMPLE_TIME"] as string,
                        ApplicationTime = reader["APPLICATION_TIME"] as string,
                        ApplDoctName = reader["APPL_DOCT_NAME"] as string,
                        ApplDoctNo = reader["APPL_DOCT_NO"] as string,
                        AuditDoctName = reader["AUDIT_DOCT_NAME"] as string,
                        AuditDoctNo = reader["AUDIT_DOCT_NO"] as string,
                        BirthDate = reader["BIRTH_DATE"] as string,
                        BusinessGenerTime = reader["BUSINESS_GENER_TIME"] as string,
                        BusinessNo = reader["BUSINESS_NO"] as string,
                        DiagNo = reader["DIAG_NO"] as string,
                        DiagTypeCode = reader["DIAG_TYPE_CODE"] as string,
                        DiagTypeName = reader["DIAG_TYPE_NAME"] as string,
                        GenderCode = reader["GENDER_CODE"] as string,
                        GenderName = reader["GENDER_NAME"] as string,
                        HealthECode = reader["HEALTH_E_CODE"] as string,
                        HospitalNo = reader["HOSPITAL_NO"] as string,
                        IdCardTypeCode = reader["ID_CARD_TYPE_CODE"] as string,
                        IdCardTypeName = reader["ID_CARD_TYPE_NAME"] as string,
                        IdCardValue = reader["ID_CARD_VALUE"] as string,
                        MicrobeTestMark = reader["MICROBE_TEST_MARK"] as string,
                        OrderRecFormNo = reader["ORDER_REC_FORM_NO"] as string,
                        OrgName = reader["ORG_NAME"] as string,
                        OrgCode = reader["ORG_CODE"] as string,
                        PatientName = reader["PATIENT_NAME"] as string,
                        PatientOrgNo = reader["PATIENT_ORG_NO"] as string,
                        ReportDoctName = reader["REPORT_DOCT_NAME"] as string,
                        ReportDoctNo = reader["REPORT_DOCT_NO"] as string,
                        ReportTypeCode = reader["REPORT_TYPE_CODE"] as string,
                        ReportTypeName = reader["REPORT_TYPE_NAME"] as string,
                        SampleDoctName = reader["SAMPLE_DOCT_NAME"] as string,
                        SampleTime = reader["SAMPLE_TIME"] as string,
                        SpecimenCollSite = reader["SPECIMEN_COLL_SITE"] as string,
                        SpecimenName = reader["SPECIMEN_NAME"] as string,
                        SpecimenNo = reader["SPECIMEN_NO"] as string,
                        SpecimenStatus = reader["SPECIMEN_STATUS"] as string,
                        SpeInspectMark = reader["SPE_INSPECT_MARK"] as string,
                        TestApplyDepartCode = reader["TEST_APPLY_DEPART_CODE"] as string,
                        TestApplyDepartName = reader["TEST_APPLY_DEPART_NAME"] as string,
                        TestApplyDepartCodeExp = reader["TEST_APPLY_DEPART_CODE_EXP"] as string,
                        TestApplyDepartNameExp = reader["TEST_APPLY_DEPART_NAME_EXP"] as string,
                        TestApplyOrgName = reader["TEST_APPLY_ORG_NAME"] as string,
                        TestDoctName = reader["TEST_DOCT_NAME"] as string,
                        TestDoctNo = reader["TEST_DOCT_NO"] as string,
                        TestProjCategoryCode = reader["TEST_PROJ_CATEGORY_CODE"] as string,
                        TestProjCategoryName = reader["TEST_PROJ_CATEGORY_NAME"] as string,
                        TestRecTime = reader["TEST_REC_TIME"] as string,
                        TestReportComment = reader["TEST_REPORT_COMMENT"] as string,
                        TestReportDate = reader["TEST_REPORT_DATE"] as string,
                        TestReportDepartCode = reader["TEST_REPORT_DEPART_CODE"] as string,
                        TestReportDepartName = reader["TEST_REPORT_DEPART_NAME"] as string,
                        TestReportDepartCodeExp = reader["TEST_REPORT_DEPART_CODE_EXP"] as string,
                        TestReportDepartNameExp = reader["TEST_REPORT_DEPART_NAME_EXP"] as string,
                        TestReportNo = reader["TEST_REPORT_NO"] as string,
                        TestReportOrgName = reader["TEST_REPORT_ORG_NAME"] as string,
                        TestType = reader["TEST_TYPE"] as string,
                        UpdateTime = reader["UPDATE_TIME"] as string,
                        UploadStatusMark = reader["UPLOAD_STATUS_MARK"] as string,
                        WardName = reader["WARD_NAME"] as string,
                        InsertDatetime = reader["INSERT_DATETIME"] as string,
                        InstockTime = reader["INSTOCK_TIME"] as string,
                        GatewayName = reader["GATEWAY_NAME"] as string,
                        DataNum = reader["DATA_NUM"] as long?,
                        InsertDatacenterTime = reader["INSERT_DATACENTER_TIME"] as string,
                        Id = reader["ID"] as string,
                        PatientId = reader["PATIENT_ID"] as string,
                        TestAuditTime = reader["TEST_AUDIT_TIME"] as string,
                        ReportClinialDiag = reader["REPORT_CLINIAL_DIAG"] as string,
                        DataCode = reader["DATACODE"] as string,
                        DataName = reader["DATANAME"] as string,
                        MtuRecMark = reader["MTURECMARK"] as string,
                        MtuRecLimitMark = reader["MTURECLIMITMARK"] as string,
                        PatNo = reader["PAT_NO"] as string,
                        RoomNo = reader["ROOM_NO"] as string,
                        BedNo = reader["BED_NO"] as string
                    };
                    reports.Add(report);
                }

                await LogToFileAsync(logPath, $"【完成】获取到 {reports.Count} 条检验报告");
                return reports.Count > 0 ? Ok(reports) : NotFound("未找到检验报告");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取检验报告列表失败");
                await LogToFileAsync(logPath, $"【异常】{ex.GetType().Name}: {ex.Message}");
                return StatusCode(500, $"获取检验报告失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取检验明细（使用现有 Test_Detail_oracle 实体）
        /// </summary>
        [HttpGet("lab-details/{businessNo}/{reportId}")]
        public async Task<ActionResult<Test_Detail_oracle>> GetLabDetails(    string businessNo,    string reportId)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "OrderCopy");
            var logPath = Path.Combine(logDir, $"GetLabDetails_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"【开始】获取检验明细, BusinessNo: {businessNo}, ReportId: {reportId}");

                // 对参数进行解码（处理特殊字符）
                businessNo = System.Web.HttpUtility.UrlDecode(businessNo);
                reportId = System.Web.HttpUtility.UrlDecode(reportId);

                var detail = new Test_Detail_oracle();

                using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();

                detail.Report_testr_res_indicate = await GetTestIndicatorsAsync(conn, reportId, businessNo);
                detail.Report_TMICROBE_BACTERIA_RES = await GetMicrobeBacteriaResultsAsync(conn, reportId, businessNo);
                detail.Report_TMICROBE_SUSCEPT_RES = await GetMicrobeSusceptResultsAsync(conn, reportId, businessNo);

                await LogToFileAsync(logPath, $"【完成】获取检验明细 - 指标: {detail.Report_testr_res_indicate?.Count ?? 0}条");

                return (detail.Report_testr_res_indicate?.Count > 0 ||
                        detail.Report_TMICROBE_BACTERIA_RES?.Count > 0 ||
                        detail.Report_TMICROBE_SUSCEPT_RES?.Count > 0)
                    ? Ok(detail)
                    : NotFound("未找到检验明细");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取检验明细失败");
                await LogToFileAsync(logPath, $"【异常】{ex.GetType().Name}: {ex.Message}");
                return StatusCode(500, $"获取检验明细失败: {ex.Message}");
            }
        }

        #endregion

        #region 检查报告相关接口

        /// <summary>
        /// 获取患者的检查报告列表
        /// </summary>
        [HttpGet("exam-reports/{idCard}")]
        public async Task<ActionResult<List<T_CHECK_REC_oracle>>> GetExamReports(string idCard)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "OrderCopy");
            var logPath = Path.Combine(logDir, $"GetExamReports_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"【开始】获取检查报告列表, IdCard: {idCard}");

                var reports = new List<T_CHECK_REC_oracle>();
                int days = _reminderSettings?.DefaultLookbackDays ?? 15;

                var sql = @"
SELECT * 
FROM (
    SELECT T.*, t.rowid
    FROM t_check_rec T  ,T_REPORT_RECOGNITION A 
    WHERE 1=1 and T.id=a.report_id
        AND T.mtuRecMark = 1  and RECOGNITION_STATUS='1'
        AND T.id_card_value = :IdCard
        AND (
            (T.check_report_date LIKE '%T%Z' 
             AND TO_DATE(REPLACE(REPLACE(T.check_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS') >= SYSDATE - NUMTODSINTERVAL(" + days + @", 'DAY'))
            OR
            (T.check_report_date LIKE '% %' AND T.check_report_date NOT LIKE '%T%' 
             AND TO_DATE(T.check_report_date, 'YYYY-MM-DD HH24:MI:SS') >= SYSDATE - NUMTODSINTERVAL(" + days + @", 'DAY'))
        )
    ORDER BY 
        CASE 
            WHEN T.check_report_date LIKE '%T%Z' THEN 
                TO_DATE(REPLACE(REPLACE(T.check_report_date, 'T', ' '), 'Z', ''), 'YYYY-MM-DD HH24:MI:SS')
            WHEN T.check_report_date LIKE '% %' AND T.check_report_date NOT LIKE '%T%' THEN
                TO_DATE(T.check_report_date, 'YYYY-MM-DD HH24:MI:SS')
            ELSE NULL
        END DESC
)";

                using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add(new OracleParameter("IdCard", idCard));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var report = new T_CHECK_REC_oracle
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
                        WardName = reader["ward_name"] as string
                    };
                    reports.Add(report);
                }

                await LogToFileAsync(logPath, $"【完成】获取到 {reports.Count} 条检查报告");
                return reports.Count > 0 ? Ok(reports) : NotFound("未找到检查报告");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取检查报告列表失败");
                await LogToFileAsync(logPath, $"【异常】{ex.GetType().Name}: {ex.Message}");
                return StatusCode(500, $"获取检查报告失败: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 获取检查报告明细（使用现有 T_CHECK_REC_oracle 实体）
        /// </summary>
        [HttpGet("exam-details/{businessNo}/{reportId}")]
        public async Task<ActionResult<T_CHECK_REC_oracle>> GetExamDetails(    string businessNo,    string reportId)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "OrderCopy");
            var logPath = Path.Combine(logDir, $"GetExamDetails_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"【开始】获取检查报告明细, BusinessNo: {businessNo}, ReportId: {reportId}");
                // 检查参数
                if (string.IsNullOrEmpty(businessNo) || string.IsNullOrEmpty(reportId))
                {
                    return BadRequest("businessNo 和 reportId 参数不能为空");
                }

                // 对参数进行解码（处理特殊字符）
                businessNo = System.Web.HttpUtility.UrlDecode(businessNo);
                reportId = System.Web.HttpUtility.UrlDecode(reportId);
                var sql = @"
SELECT * FROM t_check_rec T 
WHERE T.BUSINESS_NO = :BusinessNo
  AND T.ID = :ReportId
  AND T.mtuRecMark = 1";

                await LogToFileAsync(logPath, $"执行SQL: {sql}, 参数BusinessNo: {businessNo}, ReportId: {reportId}");

                using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add(new OracleParameter("BusinessNo", businessNo));
                cmd.Parameters.Add(new OracleParameter("ReportId", reportId));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var detail = new T_CHECK_REC_oracle
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
                        WardName = reader["ward_name"] as string
                    };

                    await LogToFileAsync(logPath, $"【完成】获取检查报告明细成功, ReportId: {reportId}");
                    return Ok(detail);
                }

                await LogToFileAsync(logPath, $"【完成】未找到检查报告明细, BusinessNo: {businessNo}, ReportId: {reportId}");
                return NotFound($"未找到检查报告明细");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取检查报告明细失败");
                await LogToFileAsync(logPath, $"【异常】{ex.GetType().Name}: {ex.Message}");
                return StatusCode(500, $"获取检查报告明细失败: {ex.Message}");
            }
        }

        #region 私有查询方法（从原代码复制）

        private async Task<List<T_testr_res_indicate_oracle>> GetTestIndicatorsAsync(OracleConnection connection, string? reportId, string? businessNo)
        {
            var sql = @"
            SELECT * 
            FROM T_TESTR_RES_INDICATE 
            WHERE 1=1 ";

            if (!string.IsNullOrEmpty(businessNo))
            {
                sql += " AND business_no = :businessNo";
            }

            using var cmd = new OracleCommand(sql, connection);
            if (!string.IsNullOrEmpty(businessNo))
                cmd.Parameters.Add("businessNo", OracleDbType.Varchar2).Value = businessNo;

            var results = new List<T_testr_res_indicate_oracle>();
            using var reader = await cmd.ExecuteReaderAsync();

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
                results.Add(record);
            }
            return results;
        }

        private async Task<List<T_MICROBE_BACTERIA_RES_oracle>> GetMicrobeBacteriaResultsAsync(OracleConnection connection, string? reportId, string? businessNo)
        {
            var sql = @"
            SELECT * 
            FROM T_MICROBE_BACTERIA_RES 
            WHERE 1=1 ";

            if (!string.IsNullOrEmpty(businessNo))
                sql += " AND business_no = :businessNo";

            using var cmd = new OracleCommand(sql, connection);
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

            if (!string.IsNullOrEmpty(businessNo))
                sql += " AND business_no = :businessNo";

            using var cmd = new OracleCommand(sql, connection);
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

        #endregion
        /// <summary>
        /// 记录医嘱引用（复制病历时调用）- 存在则更新，不存在则跳过
        /// </summary>
        /// <param name="request">引用记录请求</param>
        [HttpPost("record-reference")]
        public async Task<ActionResult<ApiResponse<bool>>> RecordReference([FromBody] ReferenceRecordRequest request)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs2", "OrderCopy");
            var logPath = Path.Combine(logDir, $"RecordReference_{DateTime.Now:yyyyMMdd}.log");

            try
            {
                Directory.CreateDirectory(logDir);
                await LogToFileAsync(logPath, $"【开始】记录医嘱引用, PatientId: {request.PatientId}, DoctorId: {request.DoctorId}, ReportId: {request.ReportId}");

                // 先检查记录是否存在
                                var checkSql = @"
                SELECT COUNT(1) FROM T_REPORT_RECOGNITION 
                WHERE PATIENT_ID = :PatientId 
                  AND DOCTORID = :DoctorId 
                  AND REPORT_ID = :ReportId";

                using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();

                using var checkCmd = new OracleCommand(checkSql, conn);
                checkCmd.Parameters.Add(new OracleParameter("PatientId", request.PatientId));
                checkCmd.Parameters.Add(new OracleParameter("DoctorId", request.DoctorId));
                checkCmd.Parameters.Add(new OracleParameter("ReportId", request.ReportId));

                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                if (!exists)
                {
                    await LogToFileAsync(logPath, $"【跳过】记录不存在，不执行任何操作");
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "记录不存在，跳过更新",
                        Data = false
                    });
                }

                // 记录存在，执行更新
                var updateSql = @"
                UPDATE T_REPORT_RECOGNITION 
                SET REFERENCE_RECORD_ID = '1',
                    UPDATE_TIME = SYSDATE
                WHERE PATIENT_ID = :PatientId 
                  AND DOCTORID = :DoctorId 
                  AND REPORT_ID = :ReportId";

                using var updateCmd = new OracleCommand(updateSql, conn);
                updateCmd.Parameters.Add(new OracleParameter("PatientId", request.PatientId));
                updateCmd.Parameters.Add(new OracleParameter("DoctorId", request.DoctorId));
                updateCmd.Parameters.Add(new OracleParameter("ReportId", request.ReportId));

                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                await LogToFileAsync(logPath, $"【完成】更新记录成功, 影响行数: {rowsAffected}");

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "更新成功",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录医嘱引用失败");
                await LogToFileAsync(logPath, $"【异常】{ex.GetType().Name}: {ex.Message}");
                return Ok(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
        }

        /// <summary>
        /// 引用记录请求DTO
        /// </summary>
        public class ReferenceRecordRequest
        {
            /// <summary>
            /// 病人ID
            /// </summary>
            public string PatientId { get; set; }

            /// <summary>
            /// 医生ID
            /// </summary>
            public string DoctorId { get; set; }

            /// <summary>
            /// 报告ID
            /// </summary>
            public string ReportId { get; set; }
        }

        /// <summary>
        /// API响应通用类
        /// </summary>
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
        }
    }
}