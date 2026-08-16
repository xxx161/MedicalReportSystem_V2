using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    // 定义DTO类
    // 更新后的DTO类
    public class InspectionFindingRequestDto
    {
        [JsonPropertyName("input")]
        public InputData input { get; set; }

        public class InputData
        {
            [JsonPropertyName("apply_info")]
            public ApplyInfo apply_info { get; set; }

            [JsonPropertyName("head")]
            public Head head { get; set; }
        }

        public class ApplyInfo
        {
            [JsonPropertyName("apply_id")]
            public string apply_id { get; set; }

            [JsonPropertyName("pat_visit_type")]
            public string pat_visit_type { get; set; }

            [JsonPropertyName("oprtr_id")]
            public string oprtr_id { get; set; }

            [JsonPropertyName("oprtr_name")]
            public string oprtr_name { get; set; }

            [JsonPropertyName("outpno")]
            public string outpno { get; set; }

            [JsonPropertyName("idno")]
            public string idno { get; set; }

            [JsonPropertyName("start_date")]
            public string start_date { get; set; }

            [JsonPropertyName("end_date")]
            public string end_date { get; set; }
        }

        public class Head
        {
            [JsonPropertyName("bizno")]
            public string bizno { get; set; }

            [JsonPropertyName("sysno")]
            public string sysno { get; set; }

            [JsonPropertyName("tarno")]
            public string tarno { get; set; }

            [JsonPropertyName("time")]
            public string time { get; set; }

            [JsonPropertyName("action_no")]
            public string action_no { get; set; }
        }
    }
}
