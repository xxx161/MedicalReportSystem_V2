namespace MedicalReportSystem.Models
{
    public class ReportItem
    {
        /// <summary>
        /// 数据详情 id 用于查询具体报告
        /// </summary>
        public string? reportId { get; set; }
        /// <summary>
        /// 报告单号( 被调阅方)
        /// </summary>
        public string? reportNo { get; set; }
        /// <summary>
        /// 患者姓名
        /// </summary>
        public string? patientName { get; set; }
        /// <summary>
        /// 院内项目名称
        /// </summary>
        public string? projNameExp { get; set; }
        /// <summary>
        /// 报告时间
        /// </summary>
        public string? reportTime { get; set; }
        /// <summary>
        /// 机构名称
        /// </summary>
        public string? orgName { get; set; }=string.Empty;
    }
}