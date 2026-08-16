using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalReportSystem.Models
{
    /// <summary>
    /// T_REPORT_RECOGNITION    实体类
    /// </summary>
    public class ReportRecognition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? REPORT_ID { get; set; } // 报告ID

        [Required]
        [StringLength(50)]
        public string? PATIENT_ID { get; set; } // 患者ID

        [StringLength(50)]
        public string? GH_ID { get; set; } // 挂号ID

        [Required]
        public string? RECOGNITION_STATUS { get; set; } // 0不互认 1已互认 2未处理

        [StringLength(50)]
        public string? REQUEST_ID { get; set; } // 请求ID

        [StringLength(50)]
        public string? EXTERNAL_CODE { get; set; } // 外部代码

        [StringLength(200)]
        public string? EXTERNAL_MSG { get; set; } // 外部消息

        [StringLength(20)]
        public string? REPORT_TYPE { get; set; } // 报告类型(lab/exam)

        public DateTime UPDATE_TIME { get; set; } = DateTime.Now; // 更新时间
    }
}
