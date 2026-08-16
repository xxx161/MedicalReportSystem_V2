using Microsoft.AspNetCore.Mvc;

namespace MedicalReportSystem.Controllers
{
    /// <summary>
    /// 已弃用
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GBaseController : Controller
    {
        private readonly GBaseService _gbaseService;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        public GBaseController(GBaseService gbaseService, IConfiguration config, IWebHostEnvironment env)
        {
            _env = env;
            _gbaseService = gbaseService;
            _config=config;
        }

        [HttpGet("query")]
        public async Task<IActionResult> Query([FromQuery] string sql)
        {
            using (var service = new GBaseService())
            {
                service.Connect(_config["GBase:ConnectionString"],
                _config["GBase:Username"],
                _config["GBase:Password"]);
                var result = service.Query(sql);
                return Ok(result);
            }
        }

        [HttpPost("execute")]
        public IActionResult Execute([FromBody] string sql)
        {
            try
            {
                var affectedRows = _gbaseService.ExecuteUpdate(sql);
                return Ok($"Affected rows: {affectedRows}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
