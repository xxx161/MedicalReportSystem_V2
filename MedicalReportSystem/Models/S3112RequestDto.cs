using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public class S3112RequestDto
    {
        [JsonPropertyName("input")]
        public InputData input { get; set; }

        public class InputData
        {
            [JsonPropertyName("aud_key")]
            public string aud_key { get; set; } = "";

            [JsonPropertyName("client_ip")]
            public string client_ip { get; set; } = "";

            [JsonPropertyName("pv1_info")]
            public PatientVisitInfo pv1_info { get; set; }

            [JsonPropertyName("apply_info")]
            public List<ApplyInfo> apply_info { get; set; }

            [JsonPropertyName("head")]
            public Head head { get; set; }
        }

        public class PatientVisitInfo
        {
            [JsonPropertyName("pid")]
            public string pid { get; set; } = "";

            [JsonPropertyName("pat_visit_type")]
            public string pat_visit_type { get; set; } = "";

            [JsonPropertyName("pvid")]
            public string pvid { get; set; } = "";

            [JsonPropertyName("rgst_no")]
            public string rgst_no { get; set; } = "";
        }

        public class ApplyInfo
        {
            [JsonPropertyName("order_class")]
            public string order_class { get; set; } = "";

            [JsonPropertyName("sno")]
            public string sno { get; set; } = "";

            [JsonPropertyName("order_expidate_type")]
            public string order_expidate_type { get; set; } = "";

            [JsonPropertyName("apply_time")]
            public string apply_time { get; set; } = "";

            [JsonPropertyName("order_exe_time_start")]
            public string order_exe_time_start { get; set; } = "";

            [JsonPropertyName("placer_name")]
            public string placer_name { get; set; } = "";

            [JsonPropertyName("apply_dept_id")]
            public string apply_dept_id { get; set; } = "";

            [JsonPropertyName("citem_id")]
            public string citem_id { get; set; } = "";

            [JsonPropertyName("citem_name")]
            public string citem_name { get; set; } = "";

            [JsonPropertyName("exedept_id")]
            public string exedept_id { get; set; } = "";

            [JsonPropertyName("emg_sign")]
            public string emg_sign { get; set; } = "";

            [JsonPropertyName("order_drask")]
            public string order_drask { get; set; } = "";

            [JsonPropertyName("baby_sno")]
            public string baby_sno { get; set; } = "";

            [JsonPropertyName("drug_freq_code")]
            public string drug_freq_code { get; set; } = "";

            [JsonPropertyName("order_once_qunt")]
            public string order_once_qunt { get; set; } = "";

            [JsonPropertyName("order_total_qunt")]
            public string order_total_qunt { get; set; } = "";

            [JsonPropertyName("decoction_method_id")]
            public string decoction_method_id { get; set; } = "";

            [JsonPropertyName("drug_freq_mth")]
            public string drug_freq_mth { get; set; } = "";

            [JsonPropertyName("drop_number")]
            public string drop_number { get; set; } = "";

            [JsonPropertyName("lspcm_name")]
            public string lspcm_name { get; set; } = "";

            [JsonPropertyName("fee_source")]
            public string fee_source { get; set; } = "";

            [JsonPropertyName("note")]
            public string note { get; set; } = "";

            [JsonPropertyName("bedside_surcharge")]
            public string bedside_surcharge { get; set; } = "";

            [JsonPropertyName("exe_property")]
            public string exe_property { get; set; } = "";

            [JsonPropertyName("qunt_times")]
            public string qunt_times { get; set; } = "";

            [JsonPropertyName("recipe_id")]
            public string recipe_id { get; set; } = "";

            [JsonPropertyName("high_value_sign")]
            public string high_value_sign { get; set; } = "";

            [JsonPropertyName("batch")]
            public string batch { get; set; } = "";

            [JsonPropertyName("drug_item")]
            public List<DrugItem> drug_item { get; set; } = new List<DrugItem>();

            [JsonPropertyName("apply_item")]
            public List<ApplyItem> apply_item { get; set; } = new List<ApplyItem>();

            [JsonPropertyName("part_info")]
            public List<PartInfo> part_info { get; set; } = new List<PartInfo>();

            [JsonPropertyName("blood_info")]
            public BloodInfo blood_info { get; set; } = new BloodInfo();

            [JsonPropertyName("surg_info")]
            public SurgInfo surg_info { get; set; } = new SurgInfo();

            [JsonPropertyName("addition_item")]
            public List<AdditionItem> addition_item { get; set; } = new List<AdditionItem>();
        }

        public class DrugItem
        {
            [JsonPropertyName("fitem_id")]
            public string fitem_id { get; set; } = "";

            [JsonPropertyName("fitem_name")]
            public string fitem_name { get; set; } = "";

            [JsonPropertyName("dspry_id")]
            public string dspry_id { get; set; } = "";

            [JsonPropertyName("order_once_qunt")]
            public string order_once_qunt { get; set; } = "";

            [JsonPropertyName("order_total_qunt")]
            public string order_total_qunt { get; set; } = "";

            [JsonPropertyName("drug_aim")]
            public string drug_aim { get; set; } = "";

            [JsonPropertyName("drug_reason")]
            public string drug_reason { get; set; } = "";

            [JsonPropertyName("drop_number")]
            public string drop_number { get; set; } = "";

            [JsonPropertyName("order_drask")]
            public string order_drask { get; set; } = "";

            [JsonPropertyName("foot")]
            public string foot { get; set; } = "";

            [JsonPropertyName("decoction")]
            public string decoction { get; set; } = "";
        }

        public class ApplyItem
        {
            [JsonPropertyName("citem_type")]
            public string citem_type { get; set; } = "";

            [JsonPropertyName("citem_id")]
            public string citem_id { get; set; } = "";

            [JsonPropertyName("citem_name")]
            public string citem_name { get; set; } = "";

            [JsonPropertyName("exedept_id")]
            public string exedept_id { get; set; } = "";
        }

        public class PartInfo
        {
            [JsonPropertyName("parts_name")]
            public string parts_name { get; set; } = "";

            [JsonPropertyName("rmethod_name")]
            public string rmethod_name { get; set; } = "";
        }

        public class BloodInfo
        {
            [JsonPropertyName("order_type")]
            public string order_type { get; set; } = "";

            [JsonPropertyName("lscmtd_id")]
            public string lscmtd_id { get; set; } = "";

            [JsonPropertyName("lscmtd_name")]
            public string lscmtd_name { get; set; } = "";

            [JsonPropertyName("exedept_id")]
            public string exedept_id { get; set; } = "";

            [JsonPropertyName("abo_blood")]
            public string abo_blood { get; set; } = "";

            [JsonPropertyName("rh_blood")]
            public string rh_blood { get; set; } = "";
        }

        public class SurgInfo
        {
            [JsonPropertyName("surg_type")]
            public string surg_type { get; set; } = "";

            [JsonPropertyName("aneitem_id")]
            public string aneitem_id { get; set; } = "";

            [JsonPropertyName("aneitem_name")]
            public string aneitem_name { get; set; } = "";

            [JsonPropertyName("exedept_id")]
            public string exedept_id { get; set; } = "";
        }

        public class AdditionItem
        {
            [JsonPropertyName("item_title")]
            public string item_title { get; set; } = "";

            [JsonPropertyName("item_value")]
            public string item_value { get; set; } = "";

            [JsonPropertyName("element_id")]
            public string element_id { get; set; } = "";

            [JsonPropertyName("required")]
            public string required { get; set; } = "";
        }

        public class Head
        {
            [JsonPropertyName("bizno")]
            public string bizno { get; set; } = "";

            [JsonPropertyName("sysno")]
            public string sysno { get; set; } = "";

            [JsonPropertyName("tarno")]
            public string tarno { get; set; } = "";

            [JsonPropertyName("time")]
            public string time { get; set; } = "";

            [JsonPropertyName("action_no")]
            public string action_no { get; set; } = "";
        }
    }
}
