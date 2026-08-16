namespace MedicalReportSystem.Models
{
    public class ReportDetailsDto
    {
        public string? ItemType { get; set; }       // 项目类型
        public string? ItemCode { get; set; }       // 平台项目代码
        public string? ReportOrgCode { get; set; }  // 报告单医疗机构代码
        public string? ReportOrgName { get; set; }  // 报告单医疗机构名称
        public string? ReportNo { get; set; }       // 报告单编号
        public string? RemindId { get; set; }       // 提醒记录主键ID
        public string? RemindName { get; set; }     // 报告名称
        public string? RemindTime { get; set; }     // 报告时间
        public string? DiagTypeCode { get; set; }   // 就诊类型
        public string? SearchType { get; set; }     // 提醒场景
        public string? CrossFlag { get; set; }      // 跨市标记
    }
}
