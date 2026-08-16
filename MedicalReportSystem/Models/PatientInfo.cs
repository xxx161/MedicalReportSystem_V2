using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalReportSystem.Models
{
    public class PatientInfo
    {
        /// <summary>
        /// 病人ID
        /// </summary>
        [Column("病人ID")]
        public long? PatientId { get; set; }

        /// <summary>
        /// 门诊号
        /// </summary>
        [Column("门诊号")]
        public long? OutpatientNumber { get; set; }

        /// <summary>
        /// 住院号
        /// </summary>
        [Column("住院号")]
        public long? HospitalizationNumber { get; set; }


        /// <summary>
        /// 就诊卡号
        /// </summary>
        [Column("就诊卡号")]
        public string? MedicalCardNumber { get; set; }

        /// <summary>
        /// 卡验证码
        /// </summary>
        [Column("卡验证码")]
        public string? CardVerificationCode { get; set; }

        /// <summary>
        /// 费别
        /// </summary>
        [Column("费别")]
        public string? FeeType { get; set; }

        /// <summary>
        /// 医疗付款方式
        /// </summary>
        [Column("医疗付款方式")]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Column("姓名")]
        public string? Name { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [Column("性别")]
        public string? Gender { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [Column("年龄")]
        public string? Age { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        [Column("出生日期")]
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// 出生地点
        /// </summary>
        [Column("出生地点")]
        public string? BirthPlace { get; set; }

        /// <summary>
        /// 身份证号
        /// </summary>
        [Column("身份证号")]
        public string? IdNumber { get; set; }

        /// <summary>
        /// 身份
        /// </summary>
        [Column("身份")]
        public string? Identity { get; set; }

        /// <summary>
        /// 职业
        /// </summary>
        [Column("职业")]
        public string? Occupation { get; set; }

        /// <summary>
        /// 民族
        /// </summary>
        [Column("民族")]
        public string? Ethnicity { get; set; }

        /// <summary>
        /// 国籍
        /// </summary>
        [Column("国籍")]
        public string? Nationality { get; set; }

        /// <summary>
        /// 区域
        /// </summary>
        [Column("区域")]
        public string? Region { get; set; }

        /// <summary>
        /// 学历
        /// </summary>
        [Column("学历")]
        public string? Education { get; set; }

        /// <summary>
        /// 婚姻状况
        /// </summary>
        [Column("婚姻状况")]
        public string? MaritalStatus { get; set; }

        /// <summary>
        /// 家庭地址
        /// </summary>
        [Column("家庭地址")]
        public string? HomeAddress { get; set; }

        /// <summary>
        /// 家庭电话
        /// </summary>
        [Column("家庭电话")]
        public string? HomePhone { get; set; }

        /// <summary>
        /// 家庭地址邮编
        /// </summary>
        [Column("家庭地址邮编")]
        public string? HomePostalCode { get; set; }

        /// <summary>
        /// 联系人姓名
        /// </summary>
        [Column("联系人姓名")]
        public string? ContactName { get; set; }

        /// <summary>
        /// 联系人关系
        /// </summary>
        [Column("联系人关系")]
        public string? ContactRelationship { get; set; }

        /// <summary>
        /// 联系人地址
        /// </summary>
        [Column("联系人地址")]
        public string? ContactAddress { get; set; }

        /// <summary>
        /// 联系人电话
        /// </summary>
        [Column("联系人电话")]
        public string? ContactPhone { get; set; }

        /// <summary>
        /// 合同单位ID
        /// </summary>
        [Column("合同单位ID")]
        public long? ContractUnitId { get; set; }

        /// <summary>
        /// 工作单位
        /// </summary>
        [Column("工作单位")]
        public string? WorkUnit { get; set; }

        /// <summary>
        /// 单位电话
        /// </summary>
        [Column("单位电话")]
        public string? WorkPhone { get; set; }

        /// <summary>
        /// 单位邮编
        /// </summary>
        [Column("单位邮编")]
        public string? WorkPostalCode { get; set; }

        /// <summary>
        /// 单位开户行
        /// </summary>
        [Column("单位开户行")]
        public string? WorkBank { get; set; }

        /// <summary>
        /// 单位帐号
        /// </summary>
        [Column("单位帐号")]
        public string? WorkAccount { get; set; }

        /// <summary>
        /// 担保人
        /// </summary>
        [Column("担保人")]
        public string? Guarantor { get; set; }

        /// <summary>
        /// 担保额
        /// </summary>
        [Column("担保额")]
        public decimal? GuaranteeAmount { get; set; }

        /// <summary>
        /// 担保性质
        /// </summary>
        [Column("担保性质")]
        public int? GuaranteeType { get; set; }

        /// <summary>
        /// 就诊时间
        /// </summary>
        [Column("就诊时间")]
        public DateTime? VisitTime { get; set; }

        /// <summary>
        /// 就诊状态
        /// </summary>
        [Column("就诊状态")]
        public int? VisitStatus { get; set; }

        /// <summary>
        /// 就诊诊室
        /// </summary>
        [Column("就诊诊室")]
        public string? ConsultationRoom { get; set; }

        /// <summary>
        /// 住院次数
        /// </summary>
        [Column("住院次数")]
        public int? HospitalizationCount { get; set; }

        /// <summary>
        /// 当前科室ID
        /// </summary>
        [Column("当前科室ID")]
        public long? CurrentDepartmentId { get; set; }

        /// <summary>
        /// 当前病区ID
        /// </summary>
        [Column("当前病区ID")]
        public long? CurrentWardId { get; set; }

        /// <summary>
        /// 当前床号
        /// </summary>
        [Column("当前床号")]
        public string? CurrentBedNumber { get; set; }

        /// <summary>
        /// 入院时间
        /// </summary>
        [Column("入院时间")]
        public DateTime? AdmissionTime { get; set; }

        /// <summary>
        /// 出院时间
        /// </summary>
        [Column("出院时间")]
        public DateTime? DischargeTime { get; set; }

        /// <summary>
        /// 险类
        /// </summary>
        [Column("险类")]
        public int? InsuranceType { get; set; }

        /// <summary>
        /// 登记时间
        /// </summary>
        [Column("登记时间")]
        public DateTime? RegistrationTime { get; set; }

        /// <summary>
        /// 停用时间
        /// </summary>
        [Column("停用时间")]
        public DateTime? DeactivationTime { get; set; }

        /// <summary>
        /// IC卡号
        /// </summary>
        [Column("IC卡号")]
        public string? IcCardNumber { get; set; }

        /// <summary>
        /// 健康号
        /// </summary>
        [Column("健康号")]
        public string? HealthNumber { get; set; }

        /// <summary>
        /// 医保号
        /// </summary>
        [Column("医保号")]
        public string? MedicalInsuranceNumber { get; set; }

        /// <summary>
        /// 其他证件
        /// </summary>
        [Column("其他证件")]
        public string? OtherCertificates { get; set; }

        /// <summary>
        /// 监护人
        /// </summary>
        [Column("监护人")]
        public string? Guardian { get; set; }

        /// <summary>
        /// 查询密码
        /// </summary>
        [Column("查询密码")]
        public string? QueryPassword { get; set; }

        /// <summary>
        /// 在院
        /// </summary>
        [Column("在院")]
        public int? InHospital { get; set; }

        /// <summary>
        /// 锁定
        /// </summary>
        [Column("锁定")]
        public int? Locked { get; set; }

        /// <summary>
        /// 户口邮编
        /// </summary>
        [Column("户口邮编")]
        public string? HouseholdPostalCode { get; set; }

        /// <summary>
        /// 户口地址
        /// </summary>
        [Column("户口地址")]
        public string? HouseholdAddress { get; set; }

        /// <summary>
        /// 户口地址邮编
        /// </summary>
        [Column("户口地址邮编")]
        public string? HouseholdAddressPostalCode { get; set; }

        /// <summary>
        /// 籍贯
        /// </summary>
        [Column("籍贯")]
        public string? NativePlace { get; set; }

        /// <summary>
        /// EMAIL
        /// </summary>
        [Column("EMAIL")]
        public string? Email { get; set; }

        /// <summary>
        /// QQ
        /// </summary>
        [Column("QQ")]
        public string? Qq { get; set; }

        /// <summary>
        /// 联系人身份证号
        /// </summary>
        [Column("联系人身份证号")]
        public string? ContactIdNumber { get; set; }

        /// <summary>
        /// 主页ID
        /// </summary>
        [Column("主页ID")]
        public int? HomePageId { get; set; }

        /// <summary>
        /// 病人类型
        /// </summary>
        [Column("病人类型")]
        public string? PatientType { get; set; }

        /// <summary>
        /// 结算模式
        /// </summary>
        [Column("结算模式")]
        public int? SettlementMode { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Column("备注")]
        public string? Remarks { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        [Column("手机号")]
        public string? MobileNumber { get; set; }

        /// <summary>
        /// 单位地址
        /// </summary>
        [Column("单位地址")]
        public string? WorkAddress { get; set; }

        /// <summary>
        /// 临时就诊标志
        /// </summary>
        [Column("临时就诊标志")]
        public int? TemporaryVisitFlag { get; set; } 
    }
}
