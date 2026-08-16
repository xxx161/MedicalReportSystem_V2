namespace MedicalReportSystem.Models
{
    public class Test_Detail_oracle
    {
        public List<T_TEST_REC_oracle> Reports_TEST_REC { get; set; }
        public List<T_testr_res_indicate_oracle>? Report_testr_res_indicate { get; set; }
        public List<T_MICROBE_BACTERIA_RES_oracle>? Report_TMICROBE_BACTERIA_RES { get; set; }
        public List<T_MICROBE_SUSCEPT_RES_oracle>? Report_TMICROBE_SUSCEPT_RES { get; set; }
        public List<T_CHECK_REC_oracle>? Report_CHECK_REC_oracle { get; set; }
    }
}
