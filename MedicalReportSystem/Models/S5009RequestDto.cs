using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public class S5009RequestDto
    {
        [JsonPropertyName("input")]
        public InputData input { get; set; }

        public class InputData
        {
            [JsonPropertyName("apply_info")]
            public List<ApplyInfo> apply_info { get; set; }

            [JsonPropertyName("head")]
            public Head head { get; set; }
        }

        public class ApplyInfo
        {
            [JsonPropertyName("apply_id")]
            public string apply_id { get; set; } = "";

            [JsonPropertyName("chkin_ps")]
            public string chkin_ps { get; set; } = "";
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
