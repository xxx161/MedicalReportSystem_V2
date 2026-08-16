using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalReportSystem.Models
{
    public class T_MICROBE_BACTERIA_RES_oracle
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
        /// 细菌结果流水号
        /// </summary>
        [Column("bacteria_indicate_no")]
        public string? BacteriaIndicateNo { get; set; }

        /// <summary>
        /// 细菌名称
        /// </summary>
        [Column("bacteria_name")]
        public string? BacteriaName { get; set; }

        /// <summary>
        /// 报告结果描述
        /// </summary>
        [Column("bacteria_result_descr")]
        public string? BacteriaResultDescription { get; set; }

        /// <summary>
        /// 抑菌环直径
        /// </summary>
        [Column("bacter_ring_num")]
        public string? BacterRingNum { get; set; }

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
        /// 菌落计数
        /// </summary>
        [Column("colony_count")]
        public string? ColonyCount { get; set; }

        /// <summary>
        /// 菌落形态
        /// </summary>
        [Column("colony_form")]
        public string? ColonyForm { get; set; }

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
        /// 院内仪器设备名称
        /// </summary>
        [Column("device_name")]
        public string? DeviceName { get; set; }

        /// <summary>
        /// 院内仪器设备编号
        /// </summary>
        [Column("device_no")]
        public string? DeviceNo { get; set; }

        /// <summary>
        /// 发现方式
        /// </summary>
        [Column("found_way")]
        public string? FoundWay { get; set; }

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
        /// 培养条件
        /// </summary>
        [Column("incubation_condition")]
        public string? IncubationCondition { get; set; }

        /// <summary>
        /// 培养时长
        /// </summary>
        [Column("incubation_time")]
        public string? IncubationTime { get; set; }

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
        /// 入库时间
        /// </summary>
        [Column("INSTOCK_TIME")]
        public string? InstockTime { get; set; }

        /// <summary>
        /// 培养基
        /// </summary>
        [Column("medium")]
        public string? Medium { get; set; }

        /// <summary>
        /// 多耐菌标识
        /// </summary>
        [Column("multi_bacteria_mark")]
        public string? MultiBacteriaMark { get; set; }

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
        [Column("paper_contain_num")]
        public string? PaperContainNum { get; set; }

        /// <summary>
        /// 患者id
        /// </summary>
        [Column("patient_id")]
        [StringLength(100)]
        public string? PatientId { get; set; }

        /// <summary>
        /// 试验板名称
        /// </summary>
        [Column("test_board_name")]
        public string? TestBoardName { get; set; }

        /// <summary>
        /// 试验板序号
        /// </summary>
        [Column("test_board_number")]
        public string? TestBoardNumber { get; set; }

        /// <summary>
        /// 检验报告单编号
        /// </summary>
        [Column("test_report_no")]
        public string? TestReportNo { get; set; }

        /// <summary>
        /// 检测结果
        /// </summary>
        [Column("test_result")]
        public string? TestResult { get; set; }

        /// <summary>
        /// 检测结果文字描述
        /// </summary>
        [Column("test_result_description")]
        public string? TestResultDescription { get; set; }

        /// <summary>
        /// 业务数据更新时间
        /// </summary>
        [Column("update_time")]
        public string? UpdateTime { get; set; }

        // 导航属性
        public T_TEST_REC? TestRecord { get; set; }
    }
}
