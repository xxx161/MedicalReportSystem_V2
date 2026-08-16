namespace MedicalReportSystem.Models
{
    public class Test_Detail
    {
        public List<T_TEST_REC> Reports_TEST_REC { get; set; }
        public List<T_testr_res_indicate>? Report_testr_res_indicate { get; set; }
        public List<T_MICROBE_BACTERIA_RES>? Report_TMICROBE_BACTERIA_RES { get; set; }
        public List<T_MICROBE_SUSCEPT_RES>? Report_TMICROBE_SUSCEPT_RES { get; set; }
    }
}
