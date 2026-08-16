using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public class S4008RequestDto
    {
        [JsonPropertyName("input")]
        public InputData input { get; set; }

        public class InputData
        {
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

            [JsonPropertyName("rgst_no")]
            public string rgst_no { get; set; } = "";
        }

        public class ApplyInfo
        {
            [JsonPropertyName("apply_id")]
            public string apply_id { get; set; } = "";

            [JsonPropertyName("apply_time")]
            public string apply_time { get; set; } = "";

            [JsonPropertyName("placer_name")]
            public string placer_name { get; set; } = "";
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
