using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedicalReportSystem.Models
{
    /// <summary>
    /// 检验记录_检验结果
    /// </summary>
    public class T_testr_res_indicate
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Column("TID")]
        public long Tid { get; set; }

        /// <summary>
        /// 检验结果异常标识代码
        /// </summary>
        [Column("anomaly_code")]
        public string? AnomalyCode { get; set; }

        /// <summary>
        /// 检验结果异常标识名称
        /// </summary>
        [Column("anomaly_name")]
        public string? AnomalyName { get; set; }

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
        /// 临床意义描述
        /// </summary>
        [Column("clinical_meaning_desc")]
        public string? ClinicalMeaningDesc { get; set; }

        /// <summary>
        /// 危急重值下限
        /// </summary>
        [Column("critical_lower_limit")]
        public string? CriticalLowerLimit { get; set; }

        /// <summary>
        /// 是否危急值
        /// </summary>
        [Column("critical_sign")]
        public string? CriticalSign { get; set; }

        /// <summary>
        /// 是否已及时处置危急值
        /// </summary>
        [Column("critical_timely_sign")]
        public string? CriticalTimelySign { get; set; }

        /// <summary>
        /// 危急重值上限
        /// </summary>
        [Column("critical_upper_limit")]
        public string? CriticalUpperLimit { get; set; }

        /// <summary>
        /// 危急重值描述
        /// </summary>
        [Column("critical_value_res")]
        public string? CriticalValueRes { get; set; }

        /// <summary>
        /// 数据主键
        /// </summary>
        [Column("data_id")]
        public string? DataId { get; set; }

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
        /// 设备编码
        /// </summary>
        [Column("equipment_code")]
        public string? EquipmentCode { get; set; }

        /// <summary>
        /// 检验指标代码
        /// </summary>
        [Column("exam_item_code")]
        public string? ExamItemCode { get; set; }

        /// <summary>
        /// 检验指标名称
        /// </summary>
        [Column("exam_item_name")]
        public string? ExamItemName { get; set; }

        /// <summary>
        /// 前置机来源标识
        /// </summary>
        [Column("gateway_name")]
        [StringLength(50)]
        public string? GatewayName { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        [Column("ID")]
        [StringLength(32)]
        public string? Id { get; set; }

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
        /// 检验方法
        /// </summary>
        [Column("inspection_methods")]
        public string? InspectionMethods { get; set; }

        /// <summary>
        /// 入库时间
        /// </summary>
        [Column("INSTOCK_TIME")]
        public DateTime? InstockTime { get; set; }

        /// <summary>
        /// 仪器编号
        /// </summary>
        [Column("instrument_code")]
        public string? InstrumentCode { get; set; }

        /// <summary>
        /// 仪器名称
        /// </summary>
        [Column("instrument_name")]
        public string? InstrumentName { get; set; }

        /// <summary>
        /// LOINC编码
        /// </summary>
        [Column("loinc_code")]
        public string? LoincCode { get; set; }

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
        /// 正常值参考值
        /// </summary>
        [Column("normal_ref_limit")]
        public string? NormalRefLimit { get; set; }

        /// <summary>
        /// 正常值参考描述
        /// </summary>
        [Column("normal_ref_res")]
        public string? NormalRefRes { get; set; }

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
        /// 患者id
        /// </summary>
        [Column("patient_id")]
        [StringLength(100)]
        public string? PatientId { get; set; }

        /// <summary>
        /// 检测指标结果
        /// </summary>
        [Column("test_index_result")]
        public string? TestIndexResult { get; set; }

        /// <summary>
        /// 检测指标结果计量单位
        /// </summary>
        [Column("test_index_uint")]
        public string? TestIndexUnit { get; set; }

        /// <summary>
        /// 医院检验明细项目编码
        /// </summary>
        [Column("test_proj_code_exp")]
        public string? TestProjCodeExp { get; set; }

        /// <summary>
        /// 医院检验明细项目名称
        /// </summary>
        [Column("test_proj_name_exp")]
        public string? TestProjNameExp { get; set; }

        /// <summary>
        /// 检验报告单编号
        /// </summary>
        [Column("test_report_no")]
        public string? TestReportNo { get; set; }

        /// <summary>
        /// 检验结果描述
        /// </summary>
        [Column("test_res_descri")]
        public string? TestResDescription { get; set; }

        /// <summary>
        /// 检验结果名称
        /// </summary>
        [Column("test_res_type_name")]
        public string? TestResTypeName { get; set; }

        /// <summary>
        /// 业务数据更新时间
        /// </summary>
        [Column("update_time")]
        public DateTime? UpdateTime { get; set; }

        // 导航属性（可选）
        public virtual T_TEST_REC? TestRecord { get; set; }
    }
}
