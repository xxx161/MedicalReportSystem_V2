using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalReportSystem.Models
{
    
        /// <summary>
        /// 检验记录表(Oracle兼容版)
        /// </summary>
        //[Table("T_TEST_REC", Schema = "SHAREDATA")]

        public class T_TEST_REC_oracle
        {


            /// <summary>
            /// 主键ID
            /// </summary>
            [Key]
            [Column("TID")]
            public string? Tid { get; set; }

            /// <summary>
            /// 接收标本日期时间
            /// </summary>
            [Column("ACCEPT_SAMPLE_TIME")]
            public string? AcceptSampleTime { get; set; }

            /// <summary>
            /// 申请日期
            /// </summary>
            [Column("APPLICATION_TIME")]
            public string? ApplicationTime { get; set; }

            /// <summary>
            /// 申请医生签名
            /// </summary>
            [Column("APPL_DOCT_NAME")]
            public string? ApplDoctName { get; set; }

            /// <summary>
            /// 申请医生编号
            /// </summary>
            [Column("APPL_DOCT_NO")]
            public string? ApplDoctNo { get; set; }

            /// <summary>
            /// 审核医师签名
            /// </summary>
            [Column("AUDIT_DOCT_NAME")]
            public string? AuditDoctName { get; set; }

            /// <summary>
            /// 审核医师编号
            /// </summary>
            [Column("AUDIT_DOCT_NO")]
            public string? AuditDoctNo { get; set; }

            /// <summary>
            /// 出生日期
            /// </summary>
            [Column("BIRTH_DATE")]
            public string? BirthDate { get; set; }

            /// <summary>
            /// 业务数据产生时间
            /// </summary>
            [Column("BUSINESS_GENER_TIME")]
            public string? BusinessGenerTime { get; set; }

            /// <summary>
            /// 业务编号
            /// </summary>
            [Column("BUSINESS_NO")]
            [StringLength(64)]
            public string? BusinessNo { get; set; }

            /// <summary>
            /// 就诊流水号
            /// </summary>
            [Column("DIAG_NO")]
            public string? DiagNo { get; set; }

            /// <summary>
            /// 就诊类型代码
            /// </summary>
            [Column("DIAG_TYPE_CODE")]
            public string? DiagTypeCode { get; set; }

            /// <summary>
            /// 就诊类型名称
            /// </summary>
            [Column("DIAG_TYPE_NAME")]
            public string? DiagTypeName { get; set; }

            /// <summary>
            /// 性别代码
            /// </summary>
            [Column("GENDER_CODE")]
            public string? GenderCode { get; set; }

            /// <summary>
            /// 性别名称
            /// </summary>
            [Column("GENDER_NAME")]
            public string? GenderName { get; set; }

            /// <summary>
            /// 电子健康码
            /// </summary>
            [Column("HEALTH_E_CODE")]
            public string? HealthECode { get; set; }

            /// <summary>
            /// 住院号
            /// </summary>
            [Column("HOSPITAL_NO")]
            public string? HospitalNo { get; set; }

            /// <summary>
            /// 证件类别代码
            /// </summary>
            [Column("ID_CARD_TYPE_CODE")]
            public string? IdCardTypeCode { get; set; }

            /// <summary>
            /// 证件类别名称
            /// </summary>
            [Column("ID_CARD_TYPE_NAME")]
            public string? IdCardTypeName { get; set; }

            /// <summary>
            /// 证件号码
            /// </summary>
            [Column("ID_CARD_VALUE")]
            public string? IdCardValue { get; set; }

            /// <summary>
            /// 是否是微生物检验
            /// </summary>
            [Column("MICROBE_TEST_MARK")]
            public string? MicrobeTestMark { get; set; }

            /// <summary>
            /// 医嘱号/电子申请单编号
            /// </summary>
            [Column("ORDER_REC_FORM_NO")]
            public string? OrderRecFormNo { get; set; }

            /// <summary>
            /// 机构名称
            /// </summary>
            [Column("ORG_NAME")]
            public string? OrgName { get; set; }

            /// <summary>
            /// 机构代码
            /// </summary>
            [Column("ORG_CODE")]
            public string? OrgCode { get; set; }

            /// <summary>
            /// 患者姓名
            /// </summary>
            [Column("PATIENT_NAME")]
            public string? PatientName { get; set; }

            /// <summary>
            /// 注册机构的患者编号
            /// </summary>
            [Column("PATIENT_ORG_NO")]
            public string? PatientOrgNo { get; set; }

            /// <summary>
            /// 报告医师签名
            /// </summary>
            [Column("REPORT_DOCT_NAME")]
            public string? ReportDoctName { get; set; }

            /// <summary>
            /// 报告医师编号
            /// </summary>
            [Column("REPORT_DOCT_NO")]
            public string? ReportDoctNo { get; set; }

            /// <summary>
            /// 检验报告单类别代码
            /// </summary>
            [Column("REPORT_TYPE_CODE")]
            public string? ReportTypeCode { get; set; }

            /// <summary>
            /// 检验报告单类别名称
            /// </summary>
            [Column("REPORT_TYPE_NAME")]
            public string? ReportTypeName { get; set; }

            /// <summary>
            /// 采样医生签名
            /// </summary>
            [Column("SAMPLE_DOCT_NAME")]
            public string? SampleDoctName { get; set; }

            /// <summary>
            /// 标本采样日期时间
            /// </summary>
            [Column("SAMPLE_TIME")]
            public string? SampleTime { get; set; }

            /// <summary>
            /// 标本采集部位
            /// </summary>
            [Column("SPECIMEN_COLL_SITE")]
            public string? SpecimenCollSite { get; set; }

            /// <summary>
            /// 标本名称
            /// </summary>
            [Column("SPECIMEN_NAME")]
            public string? SpecimenName { get; set; }

            /// <summary>
            /// 检验标本号
            /// </summary>
            [Column("SPECIMEN_NO")]
            public string? SpecimenNo { get; set; }

            /// <summary>
            /// 标本状态
            /// </summary>
            [Column("SPECIMEN_STATUS")]
            public string? SpecimenStatus { get; set; }

            /// <summary>
            /// 特殊检查标志
            /// </summary>
            [Column("SPE_INSPECT_MARK")]
            public string? SpeInspectMark { get; set; }

            /// <summary>
            /// 检验申请科室编号
            /// </summary>
            [Column("TEST_APPLY_DEPART_CODE")]
            public string? TestApplyDepartCode { get; set; }

            /// <summary>
            /// 检验申请科室名称
            /// </summary>
            [Column("TEST_APPLY_DEPART_NAME")]
            public string? TestApplyDepartName { get; set; }

            /// <summary>
            /// 院内检验申请科室编号
            /// </summary>
            [Column("TEST_APPLY_DEPART_CODE_EXP")]
            public string? TestApplyDepartCodeExp { get; set; }

            /// <summary>
            /// 院内检验申请科室名称
            /// </summary>
            [Column("TEST_APPLY_DEPART_NAME_EXP")]
            public string? TestApplyDepartNameExp { get; set; }

            /// <summary>
            /// 检验申请机构名称
            /// </summary>
            [Column("TEST_APPLY_ORG_NAME")]
            public string? TestApplyOrgName { get; set; }

            /// <summary>
            /// 检验医师签名
            /// </summary>
            [Column("TEST_DOCT_NAME")]
            public string? TestDoctName { get; set; }

            /// <summary>
            /// 检验医师编号
            /// </summary>
            [Column("TEST_DOCT_NO")]
            public string? TestDoctNo { get; set; }

            /// <summary>
            /// 检验项目大项代码
            /// </summary>
            [Column("TEST_PROJ_CATEGORY_CODE")]
            public string? TestProjCategoryCode { get; set; }

            /// <summary>
            /// 检验项目大项名称
            /// </summary>
            [Column("TEST_PROJ_CATEGORY_NAME")]
            public string? TestProjCategoryName { get; set; }

            /// <summary>
            /// 检验日期
            /// </summary>
            [Column("TEST_REC_TIME")]
            public string? TestRecTime { get; set; }

            /// <summary>
            /// 检验报告备注
            /// </summary>
            [Column("TEST_REPORT_COMMENT")]
            public string? TestReportComment { get; set; }

            /// <summary>
            /// 检验报告日期
            /// </summary>
            [Column("TEST_REPORT_DATE")]
            public string? TestReportDate { get; set; }

            /// <summary>
            /// 检验报告科室编号
            /// </summary>
            [Column("TEST_REPORT_DEPART_CODE")]
            public string? TestReportDepartCode { get; set; }

            /// <summary>
            /// 检验报告科室名称
            /// </summary>
            [Column("TEST_REPORT_DEPART_NAME")]
            public string? TestReportDepartName { get; set; }

            /// <summary>
            /// 院内检验报告科室编号
            /// </summary>
            [Column("TEST_REPORT_DEPART_CODE_EXP")]
            public string? TestReportDepartCodeExp { get; set; }

            /// <summary>
            /// 院内检验报告科室名称
            /// </summary>
            [Column("TEST_REPORT_DEPART_NAME_EXP")]
            public string? TestReportDepartNameExp { get; set; }

            /// <summary>
            /// 检验报告单编号
            /// </summary>
            [Column("TEST_REPORT_NO")]
            public string? TestReportNo { get; set; }

            /// <summary>
            /// 检验报告机构名称
            /// </summary>
            [Column("TEST_REPORT_ORG_NAME")]
            public string? TestReportOrgName { get; set; }

            /// <summary>
            /// 检验类别描述
            /// </summary>
            [Column("TEST_TYPE")]
            public string? TestType { get; set; }

            /// <summary>
            /// 业务数据更新时间
            /// </summary>
            [Column("UPDATE_TIME")]
            public string? UpdateTime { get; set; }

            /// <summary>
            /// 数据上传标识
            /// </summary>
            [Column("UPLOAD_STATUS_MARK")]
            public string? UploadStatusMark { get; set; }

            /// <summary>
            /// 病区名称
            /// </summary>
            [Column("WARD_NAME")]
            public string? WardName { get; set; }

            /// <summary>
            /// 创建时间
            /// </summary>
            [Column("INSERT_DATETIME")]
            public string? InsertDatetime { get; set; }

            /// <summary>
            /// 入库时间
            /// </summary>
            [Column("INSTOCK_TIME")]
            public string? InstockTime { get; set; }

            /// <summary>
            /// 前置机来源标识
            /// </summary>
            [Column("GATEWAY_NAME")]
            [StringLength(50)]
            public string? GatewayName { get; set; }

            /// <summary>
            /// 数据数量
            /// </summary>
            [Column("DATA_NUM")]
            public long? DataNum { get; set; }

            /// <summary>
            /// 数据插入datacenter库时间
            /// </summary>
            [Column("INSERT_DATACENTER_TIME")]
            public string? InsertDatacenterTime { get; set; }

            /// <summary>
            /// ID
            /// </summary>
            [Column("ID")]
            [StringLength(32)]
            public string? Id { get; set; }

            /// <summary>
            /// 患者id
            /// </summary>
            [Column("PATIENT_ID")]
            [StringLength(100)]
            public string? PatientId { get; set; }

            /// <summary>
            /// 检验审核时间
            /// </summary>
            [Column("TEST_AUDIT_TIME")]
            public string? TestAuditTime { get; set; }

            /// <summary>
            /// 临床诊断
            /// </summary>
            [Column("REPORT_CLINIAL_DIAG")]
            public string? ReportClinialDiag { get; set; }

            /// <summary>
            /// 平台项目代码
            /// </summary>
            [Column("DATACODE")]
            public string? DataCode { get; set; }

            /// <summary>
            /// 平台项目名称
            /// </summary>
            [Column("DATANAME")]
            public string? DataName { get; set; }

            /// <summary>
            /// 标识是否互认项目
            /// </summary>
            [Column("MTURECMARK")]
            public string? MtuRecMark { get; set; }

            /// <summary>
            /// 互认范围标识
            /// </summary>
            [Column("MTURECLIMITMARK")]
            public string? MtuRecLimitMark { get; set; }

            /// <summary>
            /// 门（急）诊号
            /// </summary>
            [Column("PAT_NO")]
            public string? PatNo { get; set; }

            /// <summary>
            /// 病房号
            /// </summary>
            [Column("ROOM_NO")]
            public string? RoomNo { get; set; }

            /// <summary>
            /// 病床号
            /// </summary>
            [Column("BED_NO")]
            public string? BedNo { get; set; }

            // 导航属性：指向明细表
            public ICollection<T_testr_res_indicate>? Indicators { get; set; }
        }

}
