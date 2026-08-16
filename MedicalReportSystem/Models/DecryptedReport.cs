using System.Collections.Generic;

namespace MedicalReportSystem.Models
{
    public class TestResIndicate
    {
        public string? dataId { get; set; }
        public string? testReportNo { get; set; }
        public string? businessNo { get; set; }
        public string? orgCode { get; set; }
        public string? orgName { get; set; }
        public string? testProjCodeExp { get; set; }
        public string? testProjNameExp { get; set; }
        public string? inspectionMethods { get; set; }
        public string? testIndexResult { get; set; }
        public string? testIndexUint { get; set; }
        public string? normalRefLimit { get; set; }
        /// <summary>
        /// 正 常 值 参 考 描述
        /// </summary>
        public string? normalRefRes { get; set; }
        public string? criticalUpperLimit { get; set; }
        public string? criticalLowerLimit { get; set; }
        public string? criticalValueRes { get; set; }
        public string? clinicalMeaningDesc { get; set; }
        public string? testResTypeName { get; set; }
        public string? testResDescri { get; set; }
        public string? anomalyCode { get; set; }
        public string? anomalyName { get; set; }
        public string? examItemCode { get; set; }
        /// <summary>
        /// 互认范围标识
        /// </summary>
        public string? mtuRecLimitMark { get; set; }
        /// <summary>
        /// 检验指标名称
        /// </summary>
        public string? examItemName { get; set; }
        /// <summary>
        /// LOINC 编码
        /// </summary>
        public string? loincCode { get; set; }
        public string? equipmentCode { get; set; }
        public string? instrumentCode { get; set; }
        public string? instrumentName { get; set; }
        public string? criticalSign { get; set; }
        public string? criticalTimelySign { get; set; }
        public string? dataCode { get; set; }
        public string? dataName { get; set; }
        public string? mtuRecMark { get; set; }
        public string? updateTime { get; set; }
        public string? businessGenerTime { get; set; }
    }

    public class DecryptedReport
    {
        public List<TestResIndicate>? testrResIndicates { get; set; }
        public object? microbeBacteriaRess { get; set; }
        public object? microbeSusceptRess { get; set; }
        public string? testReportNo { get; set; }
        public string? diagNo { get; set; }
        public string? businessNo { get; set; }
        public string? patientOrgNo { get; set; }
        public string? orgCode { get; set; }
        public int? uploadStatusMark { get; set; }
        public string? orgName { get; set; }
        public string? patientName { get; set; }
        public string? healthECode { get; set; }
        public string? genderCode { get; set; }
        public string? genderName { get; set; }
        public string? birthDate { get; set; }
        public string? idCardTypeCode { get; set; }
        public string? idCardTypeName { get; set; }
        public string? idCardValue { get; set; }
        public string? diagTypeCode { get; set; }
        public string? diagTypeName { get; set; }
        public string? orderRecFormNo { get; set; }
        public string? microbeTestMark { get; set; }
        public string? hospitalNo { get; set; }
        public string? wardName { get; set; }
        public string? testProjCategoryCode { get; set; }
        public string? testProjCategoryName { get; set; }
        public string? testApplyOrgName { get; set; }
        public string? testApplyDepartCode { get; set; }
        public string? testApplyDepartName { get; set; }
        public string? testApplyDepartCodeExp { get; set; }
        public string? testApplyDepartNameExp { get; set; }
        public string? applDoctName { get; set; }
        public string? applicationTime { get; set; }
        public string? testRecTime { get; set; }
        public string? testReportDate { get; set; }
        public string? testReportOrgName { get; set; }
        public string? testReportDepartCode { get; set; }
        public string? testReportDepartName { get; set; }
        public string? testReportDepartCodeExp { get; set; }
        public string? testReportDepartNameExp { get; set; }
        public string? reportDoctName { get; set; }
        public string? reportDoctNo { get; set; }
        public string? testDoctName { get; set; }
        public string? testDoctNo { get; set; }
        public string? auditDoctNo { get; set; }
        public string? auditDoctName { get; set; }
        public string? testReportComment { get; set; }
        public string? speInspectMark { get; set; }
        public string? reportTypeCode { get; set; }
        public string? reportTypeName { get; set; }
        public string? testType { get; set; }
        public string? specimenNo { get; set; }
        public string? sampleTime { get; set; }
        public string? acceptSampleTime { get; set; }
        public string? specimenName { get; set; }
        public string? specimenStatus { get; set; }
        public string? specimenCollSite { get; set; }
        public string? sampleDoctName { get; set; }
        public string? reportClinialDiag { get; set; }
        public string? dataCode { get; set; }
        public string? dataName { get; set; }
        public string? mtuRecMark { get; set; }
        public string? mtuRecLimitMark { get; set; }
        public string? patNo { get; set; }
        public string? roomNo { get; set; }
        public string? bedNo { get; set; }
        public string? updateTime { get; set; }
        public string? businessGenerTime { get; set; }
        public string? testAuditTime { get; set; }
        public string? applDoctNo { get; set; }
        public string? ReportId { get; set; }
    }
}