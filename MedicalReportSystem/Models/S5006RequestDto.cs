using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public class S5006RequestDto
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

            [JsonPropertyName("apply_status")]
            public string apply_status { get; set; } = "";

            [JsonPropertyName("verify_chrg")]
            public string verify_chrg { get; set; } = "";

            [JsonPropertyName("chk_time")]
            public string chk_time { get; set; } = "";

            [JsonPropertyName("chkin_ps")]
            public string chkin_ps { get; set; } = "";

            [JsonPropertyName("exe_process")]
            public string exe_process { get; set; } = "";

            [JsonPropertyName("exe_room")]
            public string exe_room { get; set; } = "";

            [JsonPropertyName("send_no")]
            public string send_no { get; set; } = "";
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
