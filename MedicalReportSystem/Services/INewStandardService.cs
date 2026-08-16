using MedicalReportSystem.Controllers;
using MedicalReportSystem.Models;
using System.Text.Json;
using static com.sun.tools.javadoc.JavaScriptScanner;
using static MedicalReportSystem.Services.NewStandardService;

namespace MedicalReportSystem.Services
{
    
    public interface INewStandardService
    {
        Task<ReminderRecordRequest> SubmitVerificationResult(List<ViewRecordRequest> requests);
        Task<RecognitionResponse> ReportInspectionFinding(List<ViewRecordRequest> requests); 
        Task<JsonElement> ReportInspectionFindingAsync(JsonElement requestJson, string serviceMethod);
        Task<JsonElement> S3112V2(string serviceMethod,
        string patientId,
        string patientVisitType,
        string homePageId,
        string sequenceNo,
        string placerName,
        string applyDeptId,
        string executeDeptId,
        string itemType,
        string itemId,
        string itemName,
        string itemExecuteDeptId,
        string partsName,
        string methodName,
        string registrationNo = "");
        Task<JsonElement> S4024(JsonElement requestJson, string serviceMethod, string patVisitType = "", string operatorId = "", string operatorName = "", string outpno = "", string idno = "", string startDate = "", string endDate = "");
        Task<JsonElement> S4009(JsonElement requestJson, string serviceMethod,string operatorType="", string operatorName = "",string applyId = "", string organizationName = "", string equipmentId = "");
        Task<JsonElement> S4008(string serviceMethod, string patientId, string patientVisitType, string registrationNo, string applyId, string placerName);
        Task<JsonElement> S4009V2(string serviceMethod, string applyId, string operatorType, string specimenBarcodeNo, string operatorName, string organizationName = "", string equipmentId = "");
        Task<JsonElement> S4010(List<T_testr_res_indicate_oracle> reportData, JsonElement additionalInfoJson, string serviceMethod, string applyId, string reportId, string doctorId, string patientId, string checker = "", string reporter = "", string equipmentId = "");
        Task<JsonElement> S4011(string serviceMethod, string applyId, string specimenBarcodeNo, string operatorName);
        Task<JsonElement> S5006(JsonElement requestJson, string serviceMethod, string operatorName, string applyStatus = "", string exeProcess = "", string exeRoom = "");
        Task<JsonElement> S5008(string serviceMethod, string applyId, string applyStatus, string reporterName, string checkerName, string reportId,string doctorId, string patientId, List<ReportResultItem> reportResults, List<ReportFileItem> reportFiles = null);
        Task<JsonElement> S5009(string serviceMethod, string applyId, string OperatorName);
    }
}
