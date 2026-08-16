using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedicalReportSystem.Models
{
    /// <summary>
    /// 医学影像检查报告表
    /// </summary>
    [Table("t_check_rec", Schema = "sharedata")]
    public class T_CHECK_REC
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Column("TID")]
        public long Tid { get; set; }

        /// <summary>
        /// 申请时间
        /// </summary>
        [Column("apply_time")]
        public DateTime? ApplyTime { get; set; }

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
        /// 病床号
        /// </summary>
        [Column("bed_no")]
        public string? BedNo { get; set; }

        /// <summary>
        /// 活检部位
        /// </summary>
        [Column("biopsy_site")]
        public string? BiopsySite { get; set; }

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
        /// 检查申请科室名称
        /// </summary>
        [Column("check_apply_depart_name")]
        public string? CheckApplyDepartName { get; set; }

        /// <summary>
        /// 院内检查申请科室名称
        /// </summary>
        [Column("check_apply_depart_name_exp")]
        public string? CheckApplyDepartNameExp { get; set; }

        /// <summary>
        /// 检查申请科室编号
        /// </summary>
        [Column("check_apply_depart_no")]
        public string? CheckApplyDepartNo { get; set; }

        /// <summary>
        /// 院内检查申请科室编号
        /// </summary>
        [Column("check_apply_depart_no_exp")]
        public string? CheckApplyDepartNoExp { get; set; }

        /// <summary>
        /// 检查申请机构名称
        /// </summary>
        [Column("check_apply_org_name")]
        public string? CheckApplyOrgName { get; set; }

        /// <summary>
        /// 检查审核时间
        /// </summary>
        [Column("check_audit_time")]
        public DateTime? CheckAuditTime { get; set; }

        /// <summary>
        /// 检查医师签名
        /// </summary>
        [Column("check_doct_name")]
        public string? CheckDoctName { get; set; }

        /// <summary>
        /// 检查医师编号
        /// </summary>
        [Column("check_doct_no")]
        public string? CheckDoctNo { get; set; }

        /// <summary>
        /// 检查设备仪器型号
        /// </summary>
        [Column("check_equip_model")]
        public string? CheckEquipModel { get; set; }

        /// <summary>
        /// 检查仪器号
        /// </summary>
        [Column("check_equip_num")]
        public string? CheckEquipNum { get; set; }

        /// <summary>
        /// 检查定量结果
        /// </summary>
        [Column("check_index_result")]
        public string? CheckIndexResult { get; set; }

        /// <summary>
        /// 检查定量结果计量单位
        /// </summary>
        [Column("check_index_uint")]
        public string? CheckIndexUnit { get; set; }

        /// <summary>
        /// 检查方法名称
        /// </summary>
        [Column("check_methed_name")]
        public string? CheckMethodName { get; set; }

        /// <summary>
        /// 检查结果参考值（定性）
        /// </summary>
        [Column("check_normal_ref_qual")]
        public string? CheckNormalRefQual { get; set; }

        /// <summary>
        /// 检查部位代码
        /// </summary>
        [Column("check_part_code")]
        public string? CheckPartCode { get; set; }

        /// <summary>
        /// 检查部位名称
        /// </summary>
        [Column("check_part_name")]
        public string? CheckPartName { get; set; }

        /// <summary>
        /// 检查项目代码
        /// </summary>
        [Column("check_proj_code")]
        public string? CheckProjCode { get; set; }

        /// <summary>
        /// 医院检查项目编码
        /// </summary>
        [Column("check_proj_code_exp")]
        public string? CheckProjCodeExp { get; set; }

        /// <summary>
        /// 检查项目名称
        /// </summary>
        [Column("check_proj_name")]
        public string? CheckProjName { get; set; }

        /// <summary>
        /// 医院检查项目名称
        /// </summary>
        [Column("check_proj_name_exp")]
        public string? CheckProjNameExp { get; set; }

        /// <summary>
        /// 检查号
        /// </summary>
        [Column("check_rec_no")]
        public string? CheckRecNo { get; set; }

        /// <summary>
        /// 检查报告备注/建议
        /// </summary>
        [Column("check_report_comment")]
        public string? CheckReportComment { get; set; }

        /// <summary>
        /// 检查报告时间
        /// </summary>
        [Column("check_report_date")]
        public DateTime? CheckReportDate { get; set; }

        /// <summary>
        /// 检查报告科室编号
        /// </summary>
        [Column("check_report_depart_code")]
        public string? CheckReportDepartCode { get; set; }

        /// <summary>
        /// 院内检查报告科室编号
        /// </summary>
        [Column("check_report_depart_code_exp")]
        public string? CheckReportDepartCodeExp { get; set; }

        /// <summary>
        /// 检查报告科室名称
        /// </summary>
        [Column("check_report_depart_name")]
        public string? CheckReportDepartName { get; set; }

        /// <summary>
        /// 院内检查报告科室名称
        /// </summary>
        [Column("check_report_depart_name_exp")]
        public string? CheckReportDepartNameExp { get; set; }

        /// <summary>
        /// 检查报告单编号
        /// </summary>
        [Column("check_report_no")]
        public string? CheckReportNo { get; set; }

        /// <summary>
        /// 检查报告机构名称
        /// </summary>
        [Column("check_report_org_name")]
        public string? CheckReportOrgName { get; set; }

        /// <summary>
        /// 检查结果描述
        /// </summary>
        [Column("check_res")]
        public string? CheckRes { get; set; }

        /// <summary>
        /// 检查结果代码
        /// </summary>
        [Column("check_res_code")]
        public string? CheckResCode { get; set; }

        /// <summary>
        /// 检查结果名称
        /// </summary>
        [Column("check_res_name")]
        public string? CheckResName { get; set; }

        /// <summary>
        /// 检查所见
        /// </summary>
        [Column("check_res_obj")]
        public string? CheckResObj { get; set; }

        /// <summary>
        /// 检查结果（定性）
        /// </summary>
        [Column("check_res_qual")]
        public string? CheckResQual { get; set; }

        /// <summary>
        /// 检查结论
        /// </summary>
        [Column("check_res_sub")]
        public string? CheckResSub { get; set; }

        /// <summary>
        /// 检查类型代码
        /// </summary>
        [Column("check_type_code")]
        public string? CheckTypeCode { get; set; }

        /// <summary>
        /// 检查类型名称
        /// </summary>
        [Column("check_type_name")]
        public string? CheckTypeName { get; set; }

        /// <summary>
        /// 危急值状态
        /// </summary>
        [Column("critical_state")]
        public string? CriticalState { get; set; }

        /// <summary>
        /// 数据数量
        /// </summary>
        [Column("data_num")]
        public long? DataNum { get; set; }

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
        /// 冰冻号
        /// </summary>
        [Column("frozen")]
        public string? Frozen { get; set; }

        /// <summary>
        /// 前置机来源标识
        /// </summary>
        [Column("gateway_name")]
        [StringLength(50)]
        public string? GatewayName { get; set; }

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
        /// ID
        /// </summary>
        [Column("ID")]
        [StringLength(32)]
        public string? Id { get; set; }

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
        /// 是否有影像标记
        /// </summary>
        [Column("image_exist_mark")]
        public string? ImageExistMark { get; set; }

        /// <summary>
        /// 影像号
        /// </summary>
        [Column("image_no")]
        public string? ImageNo { get; set; }

        /// <summary>
        /// 影像UID地址
        /// </summary>
        [Column("image_uid_addr")]
        public string? ImageUidAddr { get; set; }

        /// <summary>
        /// 免疫号
        /// </summary>
        [Column("immu_number")]
        public string? ImmuNumber { get; set; }

        /// <summary>
        /// 数据插入datacenter库时间
        /// </summary>
        [Column("insert_datacenter_time")]
        public DateTime? InsertDatacenterTime { get; set; }

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
        /// 仪器名称
        /// </summary>
        [Column("instrument_name")]
        public string? InstrumentName { get; set; }

        /// <summary>
        /// 互认范围标识
        /// </summary>
        [Column("mtuRecLimitMark")]
        public string? MtuRecLimitMark { get; set; }

        /// <summary>
        /// 标识是否互认项目
        /// </summary>
        [Column("mtuRecMark")]
        public string? MtuRecMark { get; set; }

        /// <summary>
        /// 正常值参考下限
        /// </summary>
        [Column("normal_ref_lower_limit")]
        public string? NormalRefLowerLimit { get; set; }

        /// <summary>
        /// 正常值参考上限
        /// </summary>
        [Column("normal_ref_upper_limit")]
        public string? NormalRefUpperLimit { get; set; }

        /// <summary>
        /// 医嘱号/电子申请单编号
        /// </summary>
        [Column("order_rec_form_no")]
        public string? OrderRecFormNo { get; set; }

        /// <summary>
        /// 机构代码
        /// </summary>
        [Column("org_code")]
        public string? OrgCode { get; set; }

        /// <summary>
        /// 机构名称
        /// </summary>
        [Column("org_name")]
        public string? OrgName { get; set; }

        /// <summary>
        /// 门（急）诊号
        /// </summary>
        [Column("pat_no")]
        public string? PatNo { get; set; }

        /// <summary>
        /// 病理肉眼所见
        /// </summary>
        [Column("pathological_naked_eye")]
        public string? PathologicalNakedEye { get; set; }

        /// <summary>
        /// 患者id
        /// </summary>
        [Column("patient_id")]
        [StringLength(100)]
        public string? PatientId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        [Column("patient_name")]
        public string? PatientName { get; set; }

        /// <summary>
        /// 机构注册的患者编号
        /// </summary>
        [Column("patient_org_no")]
        public string? PatientOrgNo { get; set; }

        /// <summary>
        /// 临床所见
        /// </summary>
        [Column("report_clinial_diag")]
        public string? ReportClinicalDiag { get; set; }

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
        /// 病房号
        /// </summary>
        [Column("room_no")]
        public string? RoomNo { get; set; }

        /// <summary>
        /// 特殊检查标志
        /// </summary>
        [Column("spe_inspect_mark")]
        public string? SpeInspectMark { get; set; }

        /// <summary>
        /// 冰冻与石蜡病理诊断符合情况代码
        /// </summary>
        [Column("surgfr_pathdiag_code")]
        public string? SurgFrPathDiagCode { get; set; }

        /// <summary>
        /// 冰冻与石蜡病理诊断符合情况名称
        /// </summary>
        [Column("surgfr_pathdiag_name")]
        public string? SurgFrPathDiagName { get; set; }

        /// <summary>
        /// 症状代码
        /// </summary>
        [Column("symptom_code")]
        public string? SymptomCode { get; set; }

        /// <summary>
        /// 症状描述
        /// </summary>
        [Column("symptom_descri")]
        public string? SymptomDescription { get; set; }

        /// <summary>
        /// 症状名称
        /// </summary>
        [Column("symptom_name")]
        public string? SymptomName { get; set; }

        /// <summary>
        /// 症状开始日期时间
        /// </summary>
        [Column("symptom_start_time")]
        public DateTime? SymptomStartTime { get; set; }

        /// <summary>
        /// 症状停止日期时间
        /// </summary>
        [Column("symptom_stop_time")]
        public DateTime? SymptomStopTime { get; set; }

        /// <summary>
        /// 病理镜下所见
        /// </summary>
        [Column("under_pathol_see")]
        public string? UnderPatholSee { get; set; }

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
    }
}
