using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public class T_testr_res_indicate_oracle
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Column("TID")]
        [JsonPropertyName("TID")]
        public string? Tid { get; set; }

        /// <summary>
        /// 检验结果异常标识代码
        /// </summary>
        [Column("anomaly_code")]
        [JsonPropertyName("anomaly_code")]
        public string? AnomalyCode { get; set; }

        /// <summary>
        /// 检验结果异常标识名称
        /// </summary>
        [Column("anomaly_name")]
        [JsonPropertyName("anomaly_name")]
        public string? AnomalyName { get; set; }

        /// <summary>
        /// 业务数据产生时间
        /// </summary>
        [Column("business_gener_time")]
        [JsonPropertyName("business_gener_time")]
        public string? BusinessGenerTime { get; set; }

        /// <summary>
        /// 业务编号
        /// </summary>
        [Column("business_no")]
        [StringLength(64)]
        [JsonPropertyName("business_no")]
        public string? BusinessNo { get; set; }

        /// <summary>
        /// 临床意义描述
        /// </summary>
        [Column("clinical_meaning_desc")]
        [JsonPropertyName("clinical_meaning_desc")]
        public string? ClinicalMeaningDesc { get; set; }

        /// <summary>
        /// 危急重值下限
        /// </summary>
        [Column("critical_lower_limit")]
        [JsonPropertyName("critical_lower_limit")]
        public string? CriticalLowerLimit { get; set; }

        /// <summary>
        /// 是否危急值
        /// </summary>
        [Column("critical_sign")]
        [JsonPropertyName("critical_sign")]
        public string? CriticalSign { get; set; }

        /// <summary>
        /// 是否已及时处置危急值
        /// </summary>
        [Column("critical_timely_sign")]
        [JsonPropertyName("critical_timely_sign")]
        public string? CriticalTimelySign { get; set; }

        /// <summary>
        /// 危急重值上限
        /// </summary>
        [Column("critical_upper_limit")]
        [JsonPropertyName("critical_upper_limit")]
        public string? CriticalUpperLimit { get; set; }

        /// <summary>
        /// 危急重值描述
        /// </summary>
        [Column("critical_value_res")]
        [JsonPropertyName("critical_value_res")]
        public string? CriticalValueRes { get; set; }

        /// <summary>
        /// 数据主键
        /// </summary>
        [Column("data_id")]
        [JsonPropertyName("data_id")]
        public string? DataId { get; set; }

        /// <summary>
        /// 数据数量
        /// </summary>
        [Column("data_num")]
        [JsonPropertyName("data_num")]
        public long? DataNum { get; set; }

        /// <summary>
        /// 平台项目代码
        /// </summary>
        [Column("dataCode")]
        [JsonPropertyName("dataCode")]
        public string? DataCode { get; set; }

        /// <summary>
        /// 平台项目名称
        /// </summary>
        [Column("dataName")]
        [JsonPropertyName("dataName")]
        public string? DataName { get; set; }

        /// <summary>
        /// 设备编码
        /// </summary>
        [Column("equipment_code")]
        [JsonPropertyName("equipment_code")]
        public string? EquipmentCode { get; set; }

        /// <summary>
        /// 检验指标代码
        /// </summary>
        [Column("exam_item_code")]
        [JsonPropertyName("exam_item_code")]
        public string? ExamItemCode { get; set; }

        /// <summary>
        /// 检验指标名称
        /// </summary>
        [Column("exam_item_name")]
        [JsonPropertyName("exam_item_name")]
        public string? ExamItemName { get; set; }

        /// <summary>
        /// 前置机来源标识
        /// </summary>
        [Column("gateway_name")]
        [StringLength(50)]
        [JsonPropertyName("gateway_name")]
        public string? GatewayName { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        [Column("ID")]
        [StringLength(32)]
        [JsonPropertyName("ID")]
        public string? Id { get; set; }

        /// <summary>
        /// 数据插入datacenter库时间
        /// </summary>
        [Column("insert_datacenter_time")]
        [JsonPropertyName("insert_datacenter_time")]
        public string? InsertDatacenterTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("INSERT_DATETIME")]
        [JsonPropertyName("INSERT_DATETIME")]
        public string? InsertDatetime { get; set; }

        /// <summary>
        /// 检验方法
        /// </summary>
        [Column("inspection_methods")]
        [JsonPropertyName("inspection_methods")]
        public string? InspectionMethods { get; set; }

        /// <summary>
        /// 入库时间
        /// </summary>
        [Column("INSTOCK_TIME")]
        [JsonPropertyName("INSTOCK_TIME")]
        public string? InstockTime { get; set; }

        /// <summary>
        /// 仪器编号
        /// </summary>
        [Column("instrument_code")]
        [JsonPropertyName("instrument_code")]
        public string? InstrumentCode { get; set; }

        /// <summary>
        /// 仪器名称
        /// </summary>
        [Column("instrument_name")]
        [JsonPropertyName("instrument_name")]
        public string? InstrumentName { get; set; }

        /// <summary>
        /// LOINC编码
        /// </summary>
        [Column("loinc_code")]
        [JsonPropertyName("loinc_code")]
        public string? LoincCode { get; set; }

        /// <summary>
        /// 互认范围标识
        /// </summary>
        [Column("mtuRecLimitMark")]
        [JsonPropertyName("mtuRecLimitMark")]
        public string? MtuRecLimitMark { get; set; }

        /// <summary>
        /// 标识是否互认项目
        /// </summary>
        [Column("mtuRecMark")]
        [JsonPropertyName("mtuRecMark")]
        public string? MtuRecMark { get; set; }

        /// <summary>
        /// 正常值参考值
        /// </summary>
        [Column("normal_ref_limit")]
        [JsonPropertyName("normal_ref_limit")]
        public string? NormalRefLimit { get; set; }

        /// <summary>
        /// 正常值参考描述
        /// </summary>
        [Column("normal_ref_res")]
        [JsonPropertyName("normal_ref_res")]
        public string? NormalRefRes { get; set; }

        /// <summary>
        /// 机构代码
        /// </summary>
        [Column("org_code")]
        [JsonPropertyName("org_code")]
        public string? OrgCode { get; set; }

        /// <summary>
        /// 机构名称
        /// </summary>
        [Column("org_name")]
        [JsonPropertyName("org_name")]
        public string? OrgName { get; set; }

        /// <summary>
        /// 患者id
        /// </summary>
        [Column("patient_id")]
        [StringLength(100)]
        [JsonPropertyName("patient_id")]
        public string? PatientId { get; set; }

        /// <summary>
        /// 检测指标结果
        /// </summary>
        [Column("test_index_result")]
        [JsonPropertyName("test_index_result")]
        public string? TestIndexResult { get; set; }

        /// <summary>
        /// 检测指标结果计量单位
        /// </summary>
        [Column("test_index_uint")]
        [JsonPropertyName("test_index_uint")]
        public string? TestIndexUnit { get; set; }

        /// <summary>
        /// 医院检验明细项目编码
        /// </summary>
        [Column("test_proj_code_exp")]
        [JsonPropertyName("test_proj_code_exp")]
        public string? TestProjCodeExp { get; set; }

        /// <summary>
        /// 医院检验明细项目名称
        /// </summary>
        [Column("test_proj_name_exp")]
        [JsonPropertyName("test_proj_name_exp")]
        public string? TestProjNameExp { get; set; }

        /// <summary>
        /// 检验报告单编号
        /// </summary>
        [Column("test_report_no")]
        [JsonPropertyName("test_report_no")]
        public string? TestReportNo { get; set; }

        /// <summary>
        /// 检验结果描述
        /// </summary>
        [Column("test_res_descri")]
        [JsonPropertyName("test_res_descri")]
        public string? TestResDescription { get; set; }

        /// <summary>
        /// 检验结果名称
        /// </summary>
        [Column("test_res_type_name")]
        [JsonPropertyName("test_res_type_name")]
        public string? TestResTypeName { get; set; }

        /// <summary>
        /// 业务数据更新时间
        /// </summary>
        [Column("update_time")]
        [JsonPropertyName("update_time")]
        public string? UpdateTime { get; set; }

        // 导航属性（可选）
        public virtual T_TEST_REC? TestRecord { get; set; }
    }
}