using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public class S5008RequestDto
    {
        [JsonPropertyName("input")]
        public InputData input { get; set; }

        public class InputData
        {
            [JsonPropertyName("apply_info")]
            public List<ApplyInfo> apply_info { get; set; }

            [JsonPropertyName("rpt_info")]
            public List<ReportInfo> rpt_info { get; set; }

            [JsonPropertyName("rpt_file")]
            public List<ReportFile> rpt_file { get; set; }

            [JsonPropertyName("head")]
            public Head head { get; set; }
        }

        public class ApplyInfo
        {
            [JsonPropertyName("apply_id")]
            public string apply_id { get; set; } = "";

            [JsonPropertyName("apply_status")]
            public string apply_status { get; set; } = "";

            [JsonPropertyName("rpt_ps")]
            public string rpt_ps { get; set; } = "";

            [JsonPropertyName("chkin_ps")]
            public string chkin_ps { get; set; } = "";

            [JsonPropertyName("rpt_path")]
            public string rpt_path { get; set; } = "";

            [JsonPropertyName("oaflag")]
            public string oaflag { get; set; } = "";

            [JsonPropertyName("rpt_time")]
            public string rpt_time { get; set; } = "";

            [JsonPropertyName("chkin_time")]
            public string chkin_time { get; set; } = "";

            [JsonPropertyName("check_no")]
            public string check_no { get; set; } = "";
        }

        public class ReportInfo
        {
            [JsonPropertyName("title_type")]
            public string title_type { get; set; } = "1";

            [JsonPropertyName("loitem_cname")]
            public string loitem_cname { get; set; } = "";

            [JsonPropertyName("order_rpt_result")]
            public string order_rpt_result { get; set; } = "";
        }

        public class ReportFile
        {
            [JsonPropertyName("file_format")]
            public string file_format { get; set; } = "";

            [JsonPropertyName("file_name")]
            public string file_name { get; set; } = "";

            [JsonPropertyName("file_content")]
            public string file_content { get; set; } = "";

            [JsonPropertyName("note")]
            public string note { get; set; } = "";
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
