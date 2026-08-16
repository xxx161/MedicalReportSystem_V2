using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public class S4011RequestDto
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
            public string apply_id { get; set; } = "";

            [JsonPropertyName("spcm_bc_no")]
            public string spcm_bc_no { get; set; } = "";

            [JsonPropertyName("oprtr_name")]
            public string oprtr_name { get; set; } = "";

            [JsonPropertyName("oprtr_time")]
            public string oprtr_time { get; set; } = "";
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
