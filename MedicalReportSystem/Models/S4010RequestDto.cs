using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public class S4010RequestDto
    {
        [JsonPropertyName("input")]
        public InputData input { get; set; }

        public class InputData
        {
            [JsonPropertyName("apply_info")]
            public ApplyInfo apply_info { get; set; }

            [JsonPropertyName("head")]
            public Head head { get; set; }

            [JsonPropertyName("rpt_info")]
            public List<ReportInfo> rpt_info { get; set; }
        }

        public class ApplyInfo
        {
            [JsonPropertyName("apply_id")]
            public string apply_id { get; set; } = "";

            [JsonPropertyName("chk_time")]
            public string chk_time { get; set; } = "";

            [JsonPropertyName("chkin_ps")]
            public string chkin_ps { get; set; } = "";

            [JsonPropertyName("chkin_time")]
            public string chkin_time { get; set; } = "";

            [JsonPropertyName("chkr")]
            public string chkr { get; set; } = "";

            [JsonPropertyName("eq_id")]
            public string eq_id { get; set; } = "";

            [JsonPropertyName("pdf_only")]
            public string pdf_only { get; set; } = "";

            [JsonPropertyName("pat_name")]
            public string pat_name { get; set; } = "";

            [JsonPropertyName("report_time")]
            public string report_time { get; set; } = "";

            [JsonPropertyName("rpt_type")]
            public string rpt_type { get; set; } = "";

            [JsonPropertyName("rptr")]
            public string rptr { get; set; } = "";

            [JsonPropertyName("spcm_bc_no")]
            public string spcm_bc_no { get; set; } = "";

            [JsonPropertyName("spcm_clct_time")]
            public string spcm_clct_time { get; set; } = "";
        }

        public class ReportInfo
        {
            [JsonPropertyName("apply_id")]
            public string apply_id { get; set; } = "";

            [JsonPropertyName("loitem_cname")]
            public string loitem_cname { get; set; } = "";

            /// <summary>
            /// 检验指标代码（对应ZLHIS诊治所见项目.编码或检验指标.指标代码）
            /// </summary>
            [JsonPropertyName("loitem_id")]
            public string loitem_id { get; set; } = "";

            [JsonPropertyName("loitem_rv")]
            public string loitem_rv { get; set; } = "";

            [JsonPropertyName("loitem_unit")]
            public string loitem_unit { get; set; } = "";
            /// <summary>
            /// 异常标志|1-正常、2-偏低、3-偏高、4-阳性(异常)、5-警戒下限、6-警戒上限
            /// </summary>
            [JsonPropertyName("oaflag")]
            public string oaflag { get; set; } = "";
            /// <summary>
            /// 结果值，为微生物报告时，该值只能是阴性或阳性
            /// </summary>
            [JsonPropertyName("order_rpt_result")]
            public string order_rpt_result { get; set; } = ""; 

            /// <summary>
            /// 指标结果小数点位数
            /// </summary>
            [JsonPropertyName("decimals")]
            public string decimals { get; set; } = "";

            /// <summary>
            /// 结果类型（1-定量;2-定性;3-半定量）
            /// </summary>
            [JsonPropertyName("loitem_result_type")]
            public string loitem_result_type { get; set; } = "";

            [JsonPropertyName("seq")]
            public string seq { get; set; } = "";
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
