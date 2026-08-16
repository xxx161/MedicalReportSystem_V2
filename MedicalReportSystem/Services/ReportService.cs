using MedicalReportSystem.Models;
using System.Text.Json;

namespace MedicalReportSystem.Services
{
    public interface IReportService
    {
        List<ReportItem> GetReportItems();
        DecryptedReport GetDecryptedReport(string reportId);
    }

    public class ReportService : IReportService
    {
        private readonly IWebHostEnvironment _env;

        public ReportService(IWebHostEnvironment env)
        {
            _env = env;
        }
        /// <summary>
        /// 加载患者基本信息
        /// </summary>
        /// <returns></returns>
        public List<ReportItem> GetReportItems()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "Data", "SimulatedData.json");
            var jsonData = File.ReadAllText(filePath);
            var reports = JsonSerializer.Deserialize<List<SimulatedReport>>(jsonData);

            return reports.Select(r => new ReportItem
            {
                reportId = r.reportId,
                reportNo = r.reportNo,
                patientName = r.patientName,
                projNameExp = r.projNameExp,
                reportTime = r.reportTime,
                orgName=r.orgName

            }).ToList();
        }

        public DecryptedReport GetDecryptedReport(string reportId)
        {
            var filePath = Path.Combine(_env.ContentRootPath, "Data", "DecryptedData.json");
            var jsonData = File.ReadAllText(filePath);
            var reports = JsonSerializer.Deserialize<List<DecryptedReport>>(jsonData);

            return reports.FirstOrDefault(r => r.ReportId == reportId);
        }
    }
}