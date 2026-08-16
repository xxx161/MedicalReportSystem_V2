

using GBS.Data.GBasedbt;
using System.Data;

namespace MedicalReportSystem.Services
{
    public class GBaseDbContext
    {
        private readonly IConfiguration _configuration;

        public GBaseDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            // 使用正确的GbsConnection类
            var connection = new GbsConnection(_configuration.GetConnectionString("GBaseConnection"));
            connection.Open();
            return connection;
        }
    }
}
