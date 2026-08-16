using MedicalReportSystem.Models;

public class ReportUploadDto
{
    // 病人信息（用于顶部栏）
    public PatientInfoDto? Patient { get; set; }

    // 检验报告列表（用于左侧列表）
    public List<SimulatedReport>? Reports { get; set; }
}

public class PatientInfoDto
{
    /// <summary>
    /// 姓名
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// 性别
    /// </summary>
    public string? Gender { get; set; }
    /// <summary>
    /// 生日
    /// </summary>
    public string? BirthDate { get;set; } // 保持字符串格式便于前端显示
    /// <summary>
    /// 身份证号
    /// </summary>
    public string? IdCard { get; set; }
    /// <summary>
    /// 电话号码
    /// </summary>
    public string? Phone { get; set; }
    /// <summary>
    /// 病人ID（唯一标识）
    /// </summary>
    public string? ID { get; set; }
}