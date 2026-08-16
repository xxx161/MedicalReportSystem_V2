using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public class S4009RequestDto
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
            [JsonPropertyName("system")]
            public string system { get; set; } = "";

            [JsonPropertyName("apply_id")]
            public string apply_id { get; set; } = "";

            [JsonPropertyName("oprtr_type")]
            public string oprtr_type { get; set; } = "";

            [JsonPropertyName("spcm_bc_no")]
            public string spcm_bc_no { get; set; } = "";

            [JsonPropertyName("oprtr_name")]
            public string oprtr_name { get; set; } = "";

            [JsonPropertyName("organization_id")]
            public string organization_id { get; set; } = "";

            [JsonPropertyName("organization_code")]
            public string organization_code { get; set; } = "";

            [JsonPropertyName("organization_name")]
            public string organization_name { get; set; } = "";

            [JsonPropertyName("oprtr_time")]
            public string oprtr_time { get; set; } = "";

            [JsonPropertyName("rejection_no")]
            public string rejection_no { get; set; } = "";

            [JsonPropertyName("rejection_content")]
            public string rejection_content { get; set; } = "";

            [JsonPropertyName("rejection_note")]
            public string rejection_note { get; set; } = "";

            [JsonPropertyName("verify_chrg")]
            public string verify_chrg { get; set; } = "";

            [JsonPropertyName("submission")]
            public string submission { get; set; } = "";

            [JsonPropertyName("submission_time")]
            public string submission_time { get; set; } = "";

            [JsonPropertyName("eq_id")]
            public string eq_id { get; set; } = "";
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
