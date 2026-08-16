using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalReportSystem.Models
{
    public class T_MICROBE_SUSCEPT_RES_oracle
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Column("TID")]
        public string? Tid { get; set; }

        /// <summary>
        /// 细菌ID
        /// </summary>
        [Column("bacteria_id")]
        public string? BacteriaId { get; set; }

        /// <summary>
        /// 细菌名称
        /// </summary>
        [Column("bacteria_name")]
        public string? BacteriaName { get; set; }

        /// <summary>
        /// 抑菌环直径
        /// </summary>
        [Column("bacteriostat_ring")]
        public string? BacteriostatRing { get; set; }

        /// <summary>
        /// 抑菌浓度
        /// </summary>
        [Column("bacteriostatic_concentrate")]
        public string? BacteriostaticConcentrate { get; set; }

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
        /// 抗药结果代码
        /// </summary>
        [Column("drug_susceptibible_code")]
        public string? DrugSusceptibilityCode { get; set; }

        /// <summary>
        /// 抗药结果名称
        /// </summary>
        [Column("drug_susceptibible_name")]
        public string? DrugSusceptibilityName { get; set; }

        /// <summary>
        /// 药敏名称
        /// </summary>
        [Column("drug_susceptible_name")]
        public string? DrugSusceptibleName { get; set; }

        /// <summary>
        /// 药敏编码
        /// </summary>
        [Column("drug_susceptible_no")]
        public string? DrugSusceptibleNo { get; set; }

        /// <summary>
        /// 药敏结果项目号
        /// </summary>
        [Column("drug_test_no")]
        public string? DrugTestNo { get; set; }

        /// <summary>
        /// 专家规则
        /// </summary>
        [Column("expert_rule")]
        public string? ExpertRule { get; set; }

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
        public string? InsertDatacenterTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("INSERT_DATETIME")]
        public string? InsertDatetime { get; set; }

        /// <summary>
        /// 检验方法
        /// </summary>
        [Column("inspection_methods")]
        public string? InspectionMethods { get; set; }

        /// <summary>
        /// 入库时间
        /// </summary>
        [Column("INSTOCK_TIME")]
        public string? InstockTime { get; set; }

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
        /// 纸片含药量
        /// </summary>
        [Column("paper_drug_content")]
        public string? PaperDrugContent { get; set; }

        /// <summary>
        /// 纸片含药量单位
        /// </summary>
        [Column("paper_drug_unit")]
        public string? PaperDrugUnit { get; set; }

        /// <summary>
        /// 患者id
        /// </summary>
        [Column("patient_id")]
        [StringLength(100)]
        public string? PatientId { get; set; }

        /// <summary>
        /// 参考值
        /// </summary>
        [Column("reference_value")]
        public string? ReferenceValue { get; set; }

        /// <summary>
        /// 药敏检测结果描述
        /// </summary>
        [Column("suscept_result_descr")]
        public string? SusceptibilityResultDescription { get; set; }

        /// <summary>
        /// 检验报告单编号
        /// </summary>
        [Column("test_report_no")]
        public string? TestReportNo { get; set; }

        /// <summary>
        /// 业务数据更新时间
        /// </summary>
        [Column("update_time")]
        public string? UpdateTime { get; set; }
    }
}
