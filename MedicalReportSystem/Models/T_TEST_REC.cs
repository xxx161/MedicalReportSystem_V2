using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedicalReportSystem.Models
{
    /// <summary>
    /// 检验记录表
    /// </summary>
    [Table("t_test_rec", Schema = "sharedata")]
    public class T_TEST_REC
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Column("TID")]
        public long Tid { get; set; }

        /// <summary>
        /// 接收标本日期时间
        /// </summary>
        [Column("accept_sample_time")]
        public DateTime? AcceptSampleTime { get; set; }

        /// <summary>
        /// 申请日期
        /// </summary>
        [Column("application_time")]
        public DateTime? ApplicationTime { get; set; }

        /// <summary>
        /// 申请医生签名
        /// </summary>
        [Column("appl_doct_name")]
        public string? ApplDoctName { get; set; }

        /// <summary>
        /// 申请医生编号
        /// </summary>
        [Column("appl_doct_no")]
        public string? ApplDoctNo { get; set; }

        /// <summary>
        /// 审核医师签名
        /// </summary>
        [Column("audit_doct_name")]
        public string? AuditDoctName { get; set; }

        /// <summary>
        /// 审核医师编号
        /// </summary>
        [Column("audit_doct_no")]
        public string? AuditDoctNo { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        [Column("birth_date")]
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// 业务数据产生时间
        /// </summary>
        [Column("business_gener_time")]
        public DateTime? BusinessGenerTime { get; set; }

        /// <summary>
        /// 业务编号
        /// </summary>
        [Column("business_no")]
        [StringLength(64)]
        public string? BusinessNo { get; set; }

        /// <summary>
        /// 就诊流水号
        /// </summary>
        [Column("diag_no")]
        public string? DiagNo { get; set; }

        /// <summary>
        /// 就诊类型代码
        /// </summary>
        [Column("diag_type_code")]
        public string? DiagTypeCode { get; set; }

        /// <summary>
        /// 就诊类型名称
        /// </summary>
        [Column("diag_type_name")]
        public string? DiagTypeName { get; set; }

        /// <summary>
        /// 性别代码
        /// </summary>
        [Column("gender_code")]
        public string? GenderCode { get; set; }

        /// <summary>
        /// 性别名称
        /// </summary>
        [Column("gender_name")]
        public string? GenderName { get; set; }

        /// <summary>
        /// 电子健康码
        /// </summary>
        [Column("health_e_code")]
        public string? HealthECode { get; set; }

        /// <summary>
        /// 住院号
        /// </summary>
        [Column("hospital_no")]
        public string? HospitalNo { get; set; }

        /// <summary>
        /// 证件类别代码
        /// </summary>
        [Column("id_card_type_code")]
        public string? IdCardTypeCode { get; set; }

        /// <summary>
        /// 证件类别名称
        /// </summary>
        [Column("id_card_type_name")]
        public string? IdCardTypeName { get; set; }

        /// <summary>
        /// 证件号码
        /// </summary>
        [Column("id_card_value")]
        public string? IdCardValue { get; set; }

        /// <summary>
        /// 是否是微生物检验
        /// </summary>
        [Column("microbe_test_mark")]
        public string? MicrobeTestMark { get; set; }

        /// <summary>
        /// 医嘱号/电子申请单编号
        /// </summary>
        [Column("order_rec_form_no")]
        public string? OrderRecFormNo { get; set; }

        /// <summary>
        /// 机构名称
        /// </summary>
        [Column("org_name")]
        public string? OrgName { get; set; }

        /// <summary>
        /// 机构代码
        /// </summary>
        [Column("org_code")]
        public string? OrgCode { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        [Column("patient_name")]
        public string? PatientName { get; set; }

        /// <summary>
        /// 注册机构的患者编号
        /// </summary>
        [Column("patient_org_no")]
        public string? PatientOrgNo { get; set; }

        /// <summary>
        /// 报告医师签名
        /// </summary>
        [Column("report_doct_name")]
        public string? ReportDoctName { get; set; }

        /// <summary>
        /// 报告医师编号
        /// </summary>
        [Column("report_doct_no")]
        public string? ReportDoctNo { get; set; }

        /// <summary>
        /// 检验报告单类别代码
        /// </summary>
        [Column("report_type_code")]
        public string? ReportTypeCode { get; set; }

        /// <summary>
        /// 检验报告单类别名称
        /// </summary>
        [Column("report_type_name")]
        public string? ReportTypeName { get; set; }

        /// <summary>
        /// 采样医生签名
        /// </summary>
        [Column("sample_doct_name")]
        public string? SampleDoctName { get; set; }

        /// <summary>
        /// 标本采样日期时间
        /// </summary>
        [Column("sample_time")]
        public DateTime? SampleTime { get; set; }

        /// <summary>
        /// 标本采集部位
        /// </summary>
        [Column("specimen_coll_site")]
        public string? SpecimenCollSite { get; set; }

        /// <summary>
        /// 标本名称
        /// </summary>
        [Column("specimen_name")]
        public string? SpecimenName { get; set; }

        /// <summary>
        /// 检验标本号
        /// </summary>
        [Column("specimen_no")]
        public string? SpecimenNo { get; set; }

        /// <summary>
        /// 标本状态
        /// </summary>
        [Column("specimen_status")]
        public string? SpecimenStatus { get; set; }

        /// <summary>
        /// 特殊检查标志
        /// </summary>
        [Column("spe_inspect_mark")]
        public string? SpeInspectMark { get; set; }

        /// <summary>
        /// 检验申请科室编号
        /// </summary>
        [Column("test_apply_depart_code")]
        public string? TestApplyDepartCode { get; set; }

        /// <summary>
        /// 检验申请科室名称
        /// </summary>
        [Column("test_apply_depart_name")]
        public string? TestApplyDepartName { get; set; }

        /// <summary>
        /// 院内检验申请科室编号
        /// </summary>
        [Column("test_apply_depart_code_exp")]
        public string? TestApplyDepartCodeExp { get; set; }

        /// <summary>
        /// 院内检验申请科室名称
        /// </summary>
        [Column("test_apply_depart_name_exp")]
        public string? TestApplyDepartNameExp { get; set; }

        /// <summary>
        /// 检验申请机构名称
        /// </summary>
        [Column("test_apply_org_name")]
        public string? TestApplyOrgName { get; set; }

        /// <summary>
        /// 检验医师签名
        /// </summary>
        [Column("test_doct_name")]
        public string? TestDoctName { get; set; }

        /// <summary>
        /// 检验医师编号
        /// </summary>
        [Column("test_doct_no")]
        public string? TestDoctNo { get; set; }

        /// <summary>
        /// 检验项目大项代码
        /// </summary>
        [Column("test_proj_category_code")]
        public string? TestProjCategoryCode { get; set; }

        /// <summary>
        /// 检验项目大项名称
        /// </summary>
        [Column("test_proj_category_name")]
        public string? TestProjCategoryName { get; set; }

        /// <summary>
        /// 检验日期
        /// </summary>
        [Column("test_rec_time")]
        public DateTime? TestRecTime { get; set; }

        /// <summary>
        /// 检验报告备注
        /// </summary>
        [Column("test_report_comment")]
        public string? TestReportComment { get; set; }

        /// <summary>
        /// 检验报告日期
        /// </summary>
        [Column("test_report_date")]
        public DateTime? TestReportDate { get; set; }

        /// <summary>
        /// 检验报告科室编号
        /// </summary>
        [Column("test_report_depart_code")]
        public string? TestReportDepartCode { get; set; }

        /// <summary>
        /// 检验报告科室名称
        /// </summary>
        [Column("test_report_depart_name")]
        public string? TestReportDepartName { get; set; }

        /// <summary>
        /// 院内检验报告科室编号
        /// </summary>
        [Column("test_report_depart_code_exp")]
        public string? TestReportDepartCodeExp { get; set; }

        /// <summary>
        /// 院内检验报告科室名称
        /// </summary>
        [Column("test_report_depart_name_exp")]
        public string? TestReportDepartNameExp { get; set; }

        /// <summary>
        /// 检验报告单编号
        /// </summary>
        [Column("test_report_no")]
        public string? TestReportNo { get; set; }

        /// <summary>
        /// 检验报告机构名称
        /// </summary>
        [Column("test_report_org_name")]
        public string? TestReportOrgName { get; set; }

        /// <summary>
        /// 检验类别描述
        /// </summary>
        [Column("test_type")]
        public string? TestType { get; set; }

        /// <summary>
        /// 业务数据更新时间
        /// </summary>
        [Column("update_time")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 数据上传标识
        /// </summary>
        [Column("upload_status_mark")]
        public string? UploadStatusMark { get; set; }

        /// <summary>
        /// 病区名称
        /// </summary>
        [Column("ward_name")]
        public string? WardName { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("INSERT_DATETIME")]
        public DateTime? InsertDatetime { get; set; }

        /// <summary>
        /// 入库时间
        /// </summary>
        [Column("INSTOCK_TIME")]
        public DateTime? InstockTime { get; set; }

        /// <summary>
        /// 前置机来源标识
        /// </summary>
        [Column("gateway_name")]
        [StringLength(50)]
        public string? GatewayName { get; set; }

        /// <summary>
        /// 数据数量
        /// </summary>
        [Column("data_num")]
        public long? DataNum { get; set; }

        /// <summary>
        /// 数据插入datacenter库时间
        /// </summary>
        [Column("insert_datacenter_time")]
        public DateTime? InsertDatacenterTime { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        [Column("ID")]
        [StringLength(32)]
        public string? Id { get; set; }

        /// <summary>
        /// 患者id
        /// </summary>
        [Column("patient_id")]
        [StringLength(100)]
        public string? PatientId { get; set; }

        /// <summary>
        /// 检验审核时间
        /// </summary>
        [Column("test_audit_time")]
        public DateTime? TestAuditTime { get; set; }

        /// <summary>
        /// 临床诊断
        /// </summary>
        [Column("report_clinial_diag")]
        public string? ReportClinialDiag { get; set; }

        /// <summary>
        /// 平台项目代码
        /// </summary>
        [Column("dataCode")]
        public string? DataCode { get; set; }

        /// <summary>
        /// 平台项目名称
        /// </summary>
        [Column("dataName")]
        public string? DataName { get; set; }

        /// <summary>
        /// 标识是否互认项目
        /// </summary>
        [Column("mtuRecMark")]
        public string? MtuRecMark { get; set; }

        /// <summary>
        /// 互认范围标识
        /// </summary>
        [Column("mtuRecLimitMark")]
        public string? MtuRecLimitMark { get; set; }

        /// <summary>
        /// 门（急）诊号
        /// </summary>
        [Column("pat_no")]
        public string? PatNo { get; set; }

        /// <summary>
        /// 病房号
        /// </summary>
        [Column("room_no")]
        public string? RoomNo { get; set; }

        /// <summary>
        /// 病床号
        /// </summary>
        [Column("bed_no")]
        public string? BedNo { get; set; }
        // 导航属性：指向明细表
        public ICollection<T_testr_res_indicate>? Indicators { get; set; }
    }

}
