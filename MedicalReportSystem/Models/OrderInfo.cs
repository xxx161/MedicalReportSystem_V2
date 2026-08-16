using System.Text.Json;

namespace MedicalReportSystem.Models
{
    public class InspectionFindingRequest
    {
        public InputData input { get; set; } = new InputData();
    }

    public class InputData
    {
        public string aud_key { get; set; } = "";
        public string client_ip { get; set; } = "";
        public Pv1Info pv1_info { get; set; } = new Pv1Info();
        public List<ApplyInfo> apply_info { get; set; } = new List<ApplyInfo>();
        //public List<Dg1Info> dg1_info { get; set; } = new List<Dg1Info>();
        public Heads head { get; set; } = new Heads();
    }

    public class Pv1Info
    {
        /// <summary>
        /// 主页ID
        /// </summary>
        public string pid { get; set; } = "";
        public string pat_visit_type { get; set; } = "2";
        public string pvid { get; set; } = "";
        public string rgst_no { get; set; } = "";
    }

    public class ApplyInfo
    {
        public string order_class { get; set; }
        public string sno { get; set; }
        public string order_expidate_type { get; set; }
        public string apply_time { get; set; }
        public string order_exe_time_start { get; set; }
        public string placer_name { get; set; }
        public string apply_dept_id { get; set; }
        public string citem_id { get; set; }  // 移除 = ""
        public string citem_name { get; set; }  // 移除 = ""
        public string exedept_id { get; set; }  // 移除 = ""
        public string emg_sign { get; set; }
        public string order_drask { get; set; }
        public string baby_sno { get; set; }
        public string drug_freq_code { get; set; }
        public string order_once_qunt { get; set; }
        public string order_total_qunt { get; set; }
        public string decoction_method_id { get; set; }
        public string drug_freq_mth { get; set; }
        public string drop_number { get; set; }
        public string lspcm_name { get; set; }
        public string fee_source { get; set; }
        public string note { get; set; }
        public string bedside_surcharge { get; set; }
        public string exe_property { get; set; }
        public string qunt_times { get; set; }
        public string recipe_id { get; set; }
        public string high_value_sign { get; set; }
        public string batch { get; set; }
        public List<DrugItem> drug_item { get; set; } = new List<DrugItem>();
        public List<ApplyItem> apply_item { get; set; } = new List<ApplyItem>();
        public List<PartInfo> part_info { get; set; } = new List<PartInfo>();
        public BloodInfo blood_info { get; set; } = new BloodInfo();
        public SurgInfo surg_info { get; set; } = new SurgInfo();
        public List<AdditionItem> addition_item { get; set; } = new List<AdditionItem>();
    }


    public class ApplyInfoRequest
    {

        public long? order_class { get; set; }
        public long? orderId { get; set; }
        public string dataCode { get; set; }
        public long? OrderID { get; set; }
        public int? patientSource { get; set; }
        public long? patientId { get; set; }
        public string homePageId { get; set; }
        public string operationType { get; set; }
        public string treatmentCategory { get; set; }
        public int? sno { get; set; }
        public int? order_expidate_type { get; set; }
        public DateTime? apply_time { get; set; }
        public DateTime? order_exe_time_start { get; set; }
        public string placer_name { get; set; }
        public int? apply_dept_id { get; set; }
        public object citem_id { get; set; }
        public string citem_name { get; set; }
        public int? exedept_id { get; set; }
        public int? emg_sign { get; set; }
        public string order_drask { get; set; }
        public int? baby_sno { get; set; }
        public string drug_freq_code { get; set; }
        public decimal? order_once_qunt { get; set; }
        public decimal? order_total_qunt { get; set; }
        public string decoction_method_id { get; set; }
        public string drug_freq_mth { get; set; }
        public decimal? drop_number { get; set; }
        public string lspcm_name { get; set; }
        public int? fee_source { get; set; }
        public string note { get; set; }
        public decimal? bedside_surcharge { get; set; }
        public string exe_property { get; set; }
        public int? qunt_times { get; set; }
        public int? recipe_id { get; set; }
        public string high_value_sign { get; set; }
        public string batch { get; set; }
        public string citem_type { get; set; }
        public string parts_name { get; set; }
        public string rmethod_name { get; set; }
        public object? citem_id_cj { get; set; }
        public string citem_name_cj { get; set; }
        public object? exedept_id_cj { get; set; }
        public string? bodyPart { get; set; }
        public string? method { get; set; }

        // 原有的列表属性（如果不需要可以移除）
        public List<ApplyItem> apply_item { get; set; }
        public List<PartInfo> part_info { get; set; }
    }

    public class Dg1Info
    {
        public string dz_sno { get; set; }
        public string dz_type { get; set; }
        public string csd_code { get; set; }
        public string dz_content { get; set; }
        public string symptom_code { get; set; }
        public string symptom_name { get; set; }
    }
    public class DrugItem
    {
        /// <summary>
        /// 药品项目ID
        /// </summary>
        public string fitem_id { get; set; } = "";

        /// <summary>
        /// 药品名称
        /// </summary>
        public string fitem_name { get; set; } = "";

        /// <summary>
        /// 配送人ID
        /// </summary>
        public string dspry_id { get; set; } = "";

        /// <summary>
        /// 单次用量
        /// </summary>
        public string order_once_qunt { get; set; } = "";

        /// <summary>
        /// 总用量
        /// </summary>
        public string order_total_qunt { get; set; } = "";

        /// <summary>
        /// 用药目的
        /// </summary>
        public string drug_aim { get; set; } = "";

        /// <summary>
        /// 用药原因
        /// </summary>
        public string drug_reason { get; set; } = "";

        /// <summary>
        /// 滴速
        /// </summary>
        public string drop_number { get; set; } = "";

        /// <summary>
        /// 医嘱说明
        /// </summary>
        public string order_drask { get; set; } = "";

        /// <summary>
        /// 脚注
        /// </summary>
        public string foot { get; set; } = "";

        /// <summary>
        /// 煎法
        /// </summary>
        public string decoction { get; set; } = "";
    }
    public class ApplyItem
    {
        /// <summary>
        /// 项目类型（C表示检查项目）
        /// </summary>
        public string citem_type { get; set; } = "";

        /// <summary>
        /// 项目ID
        /// </summary>
        public string citem_id { get; set; } = "";

        /// <summary>
        /// 项目名称
        /// </summary>
        public string citem_name { get; set; } = "";

        /// <summary>
        /// 执行科室ID
        /// </summary>
        public string exedept_id { get; set; } = "";
    }
    public class PartInfo
    {
        /// <summary>
        /// 部位名称
        /// </summary>
        public string parts_name { get; set; } = "";

        /// <summary>
        /// 方法名称
        /// </summary>
        public string rmethod_name { get; set; } = "";
    }
    public class BloodInfo
    {
        /// <summary>
        /// 医嘱类型
        /// </summary>
        public string order_type { get; set; } = "";

        /// <summary>
        /// 采血方法ID
        /// </summary>
        public string lscmtd_id { get; set; } = "";

        /// <summary>
        /// 采血方法名称
        /// </summary>
        public string lscmtd_name { get; set; } = "";

        /// <summary>
        /// 执行科室ID
        /// </summary>
        public string exedept_id { get; set; } = "";

        /// <summary>
        /// ABO血型
        /// </summary>
        public string abo_blood { get; set; } = "";

        /// <summary>
        /// Rh血型
        /// </summary>
        public string rh_blood { get; set; } = "";
    }
    public class SurgInfo
    {
        /// <summary>
        /// 手术类型
        /// </summary>
        public string surg_type { get; set; } = "";

        /// <summary>
        /// 麻醉项目ID
        /// </summary>
        public string aneitem_id { get; set; } = "";

        /// <summary>
        /// 麻醉项目名称
        /// </summary>
        public string aneitem_name { get; set; } = "";

        /// <summary>
        /// 执行科室ID
        /// </summary>
        public string exedept_id { get; set; } = "";
    }
    public class AdditionItem
    {
        /// <summary>
        /// 附加项目标题/名称
        /// </summary>
        public string item_title { get; set; } = "";

        /// <summary>
        /// 附加项目值
        /// </summary>
        public string item_value { get; set; } = "";

        /// <summary>
        /// 元素ID（可能对应前端控件ID）
        /// </summary>
        public string element_id { get; set; } = "";

        /// <summary>
        /// 是否必填（"Y"/"N"或"true"/"false"等格式）
        /// </summary>
        public string required { get; set; } = "";
    }
    public class Heads
    {
        /// <summary>
        /// 业务流水号（示例值：S3112）
        /// </summary>
        public string bizno { get; set; } = "";

        /// <summary>
        /// 系统编号（示例值：JY-JYWS）
        /// </summary>
        public string sysno { get; set; } = "";

        /// <summary>
        /// 目标系统编号（示例值：ZLHIS）
        /// </summary>
        public string tarno { get; set; } = "";

        /// <summary>
        /// 时间戳（格式：yyyy-MM-dd HH:mm:ss）
        /// </summary>
        public string time { get; set; } = "";

        /// <summary>
        /// 操作流水号（UUID格式）
        /// </summary>
        public string action_no { get; set; } = "";
    }
}
