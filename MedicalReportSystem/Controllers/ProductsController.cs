using com.sun.org.apache.xalan.@internal.xsltc.compiler.util;
using com.sun.tools.doclint;
using Dapper;
using java.sql;
using MedicalReportSystem.Models;
using MedicalReportSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
using System.Data.SqlClient;

namespace MedicalReportSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;


        public ProductsController(IWebHostEnvironment env,AppDbContext context, IConfiguration configuration)
        {
            _env = env;
            _context = context;
            _configuration = configuration;
        }
        [HttpGet("raw-query")]
        public async Task<IActionResult> RawQuery()
        {
            // 使用原生SQL验证（绕过EF映射）
            var data = await _context.Set<T_TEST_REC>()
                .FromSqlRaw("SELECT * FROM sharedata.t_test_rec")
                .ToListAsync();

            return Ok(data);
        }
        /// <summary>
        /// 测试获取实体
        /// </summary>
        /// <returns></returns>
        [HttpGet("dapper-test")]
        public async Task<IActionResult> DapperTest()
        {
            using var conn = new NpgsqlConnection(
                _configuration.GetConnectionString("OpenGaussConnection"));

            var data = await conn.QueryAsync(
                "SELECT * FROM sharedata.t_test_rec");

            return Ok(data);
        }
        /// <summary>
        /// 检查指定条件下是否存在报告数据
        /// </summary>
        /// <param name="userID">患者ID</param>
        /// <param name="type">报告类型(lab/exam)</param>
        /// <param name="dataCode">平台项目代码</param>
        /// <returns>返回布尔值表示是否存在匹配数据</returns>
        [HttpGet("exists")]
        public async Task<ActionResult<bool>> CheckReportExists(
            [FromQuery, SwaggerParameter(Required = true)] string userID,
            [FromQuery, SwaggerParameter(Required = false)] string? type = "lab",
            [FromQuery, SwaggerParameter(Required = false)] string? dataCode = null)
        {
            try
            {
                bool exists = false;

                // 默认查询检验报告
                if (string.IsNullOrEmpty(type) || type.ToLower() == "lab")
                {
                    var query = _context.T_TEST_RECS.AsNoTracking()
                        .Where(x => x.PatientOrgNo == userID);

                    if (!string.IsNullOrEmpty(dataCode))
                    {
                        query = query.Where(x => x.DataCode == dataCode);
                    }

                    exists = await query.AnyAsync();
                }
                else if (type.ToLower() == "exam")
                {
                    var query = _context.t_CHECK_RECs.AsNoTracking()
                        .Where(x => x.PatientOrgNo == userID);

                    if (!string.IsNullOrEmpty(dataCode))
                    {
                        query = query.Where(x => x.DataCode == dataCode);
                    }

                    exists = await query.AnyAsync();
                }
                else
                {
                    return BadRequest("无效的报告类型，请使用lab或exam");
                }

                return Ok(exists);
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
                return StatusCode(503, "服务暂不可用：" + ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "服务器内部错误：" + ex.Message);
            }
        }
        /// <summary>
        /// 根据传入条件获取报告列表或者单个报告的表头
        /// </summary>
        /// <param name="reportId">报告ID</param>
        /// <param name="userID">患者ID</param>
        /// <param name="type">报告类型(lab/exam)</param>
        /// <returns></returns>
        [HttpGet("detail")]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts(
            [FromQuery, SwaggerParameter(Required = false)] string? reportId,
            [FromQuery, SwaggerParameter(Required = false)] string? userID,
            [FromQuery, SwaggerParameter(Required = false)] string? type = "lab")
        {
            try
            {
                // 默认查询检验报告
                if (string.IsNullOrEmpty(type) || type.ToLower() == "lab")
                {
                    var query = _context.T_TEST_RECS.AsNoTracking().AsQueryable();

                    if (!string.IsNullOrEmpty(userID))
                    {
                        query = query.Where(x => x.PatientOrgNo == userID);
                    }

                    if (!string.IsNullOrEmpty(reportId))
                    {
                        query = query.Where(x => x.Id == reportId);
                    }

                    var results = await query.ToListAsync();
                    return results.Any() ? Ok(results) : NotFound("未找到匹配的检验记录");
                }
                else if (type.ToLower() == "exam")
                {
                    var query = _context.t_CHECK_RECs.AsNoTracking().AsQueryable();

                    if (!string.IsNullOrEmpty(userID))
                    {
                        query = query.Where(x => x.PatientOrgNo == userID);
                    }

                    if (!string.IsNullOrEmpty(reportId))
                    {
                        query = query.Where(x => x.Id == reportId);
                    }

                    var results = await query.ToListAsync();
                    return results.Any() ? Ok(results) : NotFound("未找到匹配的检查记录");
                }
                else
                {
                    return BadRequest("无效的报告类型，请使用lab或exam");
                }
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
                return StatusCode(503, "服务暂不可用：" + ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "服务器内部错误：" + ex.Message);
            }
        }

        /// <summary>
        /// 查询报告明细(表身)
        /// </summary>
        [HttpGet("report-details/{TestReportNo}/{reportId}")]
        [Produces("application/json")] // 明确指定返回类型
        public async Task<ActionResult<IEnumerable<object>>> GetDetailsAsync(
            [FromRoute] string? TestReportNo,
            [FromRoute] string? reportId)
        {
            try
            {
                // 创建日志目录和文件路径
                //var userDirLog = Path.Combine(_env.ContentRootPath, "Data", "user_Log");
                //Directory.CreateDirectory(userDirLog);
                //var listPath = Path.Combine(userDirLog, "listLog.txt");
                // 确保参数有效性
                if (string.IsNullOrWhiteSpace(TestReportNo) || string.IsNullOrWhiteSpace(reportId))
                    return BadRequest("参数不能为空");

                var query =
                    from t in _context.T_TEST_RECS
                    join a in _context.TESTR_RES_INDICATES
                        on t.TestReportNo equals a.TestReportNo
                    where a.TestReportNo == TestReportNo && t.Id == reportId
                    select new
                    {
                        t.OrgName,
                        t.TestProjCategoryName,
                        t.PatientName,
                        t.GenderName,
                        t.BirthDate,
                        t.PatNo,
                        t.TestApplyDepartNameExp,
                        t.BedNo,
                        t.ReportClinialDiag,
                        a.TestProjNameExp,
                        a.TestIndexResult,
                        a.TestIndexUnit,
                        a.NormalRefLimit,
                        a.AnomalyCode
                    };

                var results = await query.ToListAsync();
                //System.IO.File.AppendAllText(listPath, "results"+results.ToString());
                //if (!results.Any())
                    //return NotFound(new { message = "未找到匹配记录" });

                return Ok(results);
            }
            catch (Exception ex)
            { 
                return StatusCode(500, new
                {
                    error = "服务器内部错误",
                    detail = ex.Message
                });
            }
        }
        /// <summary>
        /// 获取微生物检验报告
        /// </summary>
        /// <param name="TestReportNo">外键报告ID</param>
        /// <param name="reportId">T_TEST_RECS表唯一ID</param>
        /// <returns></returns>
        [HttpGet("microbial-culture/{TestReportNo}/{reportId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetBacterialCultureResult(
        [FromRoute] string? TestReportNo,
        [FromRoute] string? reportId)
        {
            try
            {
                var query =
                    from t in _context.T_TEST_RECS
                    join a in _context.MICROBE_BACTERIA_RES on t.TestReportNo equals a.TestReportNo
                    where a.TestReportNo == TestReportNo && t.Id == reportId
                    select new
                    {
                        t.OrgName,
                        t.TestProjCategoryName,
                        t.PatientName,
                        t.GenderName,
                        t.BirthDate,
                        t.PatNo,
                        t.TestApplyDepartNameExp,
                        t.BedNo,
                        t.ReportClinialDiag,
                        a.BacteriaName,
                        a.TestResult,
                        a.IncubationTime,
                        a.TestBoardNumber,
                        a.TestBoardName,
                        a.ColonyCount,
                        a.IncubationCondition
                    };

                var results = await query.ToListAsync();
                return results.Any() ? Ok(results) : NotFound("未找到匹配记录");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "服务器内部错误: " + ex.Message);
            }
        }
        /// <summary>
        /// 获取微生物检验药敏报告
        /// </summary>
        /// <param name="TestReportNo"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        [HttpGet("drug-sensitivity-reports/{TestReportNo}/{reportId}")]
        public async Task<ActionResult<IEnumerable<object>>> GenerateDrugSensitivityReport(
        [FromRoute] string? TestReportNo,
        [FromRoute] string? reportId)
        {
            try
            {
                var query =
                    from t in _context.T_TEST_RECS
                    join a in _context.MICROBE_SUSCEPT_REs on t.TestReportNo equals a.TestReportNo
                    where a.TestReportNo == TestReportNo && t.Id == reportId  && a.TestReportNo== reportId
                    select new
                    {
                        t.OrgName,
                        t.TestProjCategoryName,
                        t.PatientName,
                        t.GenderName,
                        t.BirthDate,
                        t.PatNo,
                        t.TestApplyDepartNameExp,
                        t.BedNo,
                        t.ReportClinialDiag,
                        a.BacteriaName,// 细菌名称
                        a.SusceptibilityResultDescription,// 药敏检测结果描述
                        a.BacteriostaticConcentrate,// 抑菌浓度
                        a.DrugSusceptibilityCode,// 抗药结果代码
                        a.DrugSusceptibilityName,// 抗药结果名称
                        a.DrugSusceptibleName,// 药敏名称
                        a.DrugSusceptibleNo// 药敏编码
                    };

                var results = await query.ToListAsync();
                return results.Any() ? Ok(results) : NotFound("未找到匹配记录");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "服务器内部错误: " + ex.Message);
            }
        }
        /// <summary>
        /// 获取微生物检验报告及药敏结果（融合版本）
        /// </summary>
        /// <param name="TestReportNo">外键报告ID</param>
        /// <param name="reportId">T_TEST_RECS表唯一ID</param>
        /// <returns></returns>
        [HttpGet("microbial-combined-report/{TestReportNo}/{reportId}")]
        public async Task<ActionResult<object>> GetCombinedMicrobialReport(
            [FromRoute] string? TestReportNo,
            [FromRoute] string? reportId)
        {
            try
            {
                // 基本信息和微生物培养结果查询
                var baseQuery =
                    from t in _context.T_TEST_RECS
                    join b in _context.MICROBE_BACTERIA_RES
                        on t.TestReportNo equals b.TestReportNo into bacteriaJoin
                    from b in bacteriaJoin.DefaultIfEmpty() // 左连接微生物培养结果
                    where t.TestReportNo == TestReportNo && t.Id == reportId
                    select new
                    {
                        // 基本信息
                        BaseInfo = new
                        {
                            t.OrgName,
                            t.TestProjCategoryName,
                            t.PatientName,
                            t.GenderName,
                            t.BirthDate,
                            t.PatNo,
                            t.TestApplyDepartNameExp,
                            t.BedNo,
                            t.ReportClinialDiag
                        },
                        // 微生物培养结果
                        BacteriaResult = b != null ? new
                        {
                            b.BacteriaName,
                            b.TestResult,
                            b.IncubationTime,
                            b.TestBoardNumber,
                            b.TestBoardName,
                            b.ColonyCount,
                            b.IncubationCondition,
                            b.TestReportNo,
                            b.BacteriaIndicateNo,
                            b.BacteriaResultDescription,
                            b.FoundWay
                        } : null,
                        // 标记是否有微生物培养结果
                        HasBacteriaResult = b != null
                    };

                var baseResults = await baseQuery.ToListAsync();

                if (!baseResults.Any())
                {
                    return NotFound("未找到匹配的检验记录");
                }

                // 获取所有相关的TestReportNo（用于查询药敏结果）
                var TestReportNos = baseResults
                    .Where(x => x.HasBacteriaResult)
                    .Select(x => x.BacteriaResult.TestReportNo)
                    .Distinct()
                    .ToList();

                // 查询药敏结果（如果有businessNo）
                List<object> drugResults = new List<object>();
                if (TestReportNos.Any())
                {
                    drugResults = await _context.MICROBE_SUSCEPT_REs
                        .Where(a => TestReportNos.Contains(a.TestReportNo))
                        .Select(a => new
                        {
                            a.BacteriaName,
                            a.SusceptibilityResultDescription,
                            a.BacteriostaticConcentrate,
                            a.DrugSusceptibilityCode,
                            a.DrugSusceptibilityName,
                            a.DrugSusceptibleName,
                            a.DrugSusceptibleNo,
                            a.ExpertRule,
                            a.TestReportNo,
                            a.InspectionMethods
                        })
                        .ToListAsync<object>();
                }

                // 构建最终响应
                var response = new
                {
                    // 基础信息（取第一条，因为基本信息相同）
                    BaseInfo = baseResults.First().BaseInfo,
                    // 微生物培养结果（可能多条）
                    BacteriaResults = baseResults.Where(x => x.HasBacteriaResult)
                                        .Select(x => x.BacteriaResult)
                                        .ToList(),
                    // 药敏结果
                    DrugResults = drugResults,
                    // 是否有药敏结果
                    HasDrugResults = drugResults.Any()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "服务器内部错误: " + ex.Message);
            }
        }
        [HttpPost("validate-sync-structure")]
        [SwaggerOperation(Summary = "验证并同步表结构")]
        public async Task<IActionResult> ValidateTableStructure()
        {
            try
            {
                // 创建OracleSyncService实例
                var syncService = new OracleSyncService(
                    _configuration,
                    LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<OracleSyncService>(),
                    _env);

                // 调用验证和同步方法
                await syncService.ValidateAndSyncTableStructure();

                return Ok("表结构验证和同步已完成");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"表结构验证和同步失败: {ex.Message}");
            }
        }
        [HttpPost("test-sync")]
        [SwaggerOperation(Summary = "测试Oracle同步功能")]
        public async Task<IActionResult> TestOracleSync(
            [FromQuery] string tableName = "t_test_rec",
            [FromQuery] string idColumn = "ID",
            [FromQuery] string timestampColumn = "INSTOCK_TIME")
        {
            try
            {
                // 创建同步服务实例
                var syncService = new OracleSyncService(
                    _configuration,
                    LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<OracleSyncService>(),
                    _env);

                // 调用同步方法
                await syncService.SyncTable(tableName, idColumn, timestampColumn);

                return Ok($"表 {tableName} 同步测试已启动，请查看日志文件确认结果");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"同步测试失败: {ex.Message}");
            }
        }
        // GET: api/Products/dapper (使用Dapper)
        [HttpGet("dapper")]
        public async Task<ActionResult<IEnumerable<T_TEST_REC>>> GetProductsDapper()
        {
            using IDbConnection dbConnection = new NpgsqlConnection(
                _configuration.GetConnectionString("OpenGaussConnection"));

            var products = await dbConnection.QueryAsync<T_TEST_REC>("SELECT test_proj_category_name FROM sharedata.T_TEST_REC");
            return Ok(products);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<T_TEST_REC>> GetProduct(string id)
        {
            var product = await _context.T_TEST_RECS.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<T_TEST_REC>> PostProduct(T_TEST_REC product)
        {
            _context.T_TEST_RECS.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(string id, T_TEST_REC product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.T_TEST_RECS.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.T_TEST_RECS.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        /// <summary>
        /// 连接测试
        /// </summary>
        /// <returns></returns>
        [HttpGet("test-connection-with-timeout")]
        public async Task<IActionResult> TestConnectionWithTimeout()
        {
            string connectionString = _configuration.GetConnectionString("OpenGaussConnection") + ";Timeout=9"; // 9秒超时

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(9)); // 双保险
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cts.Token);
                return Ok("连接成功（带超时控制）");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, "连接超时");
            }
        }

        private bool ProductExists(string id)
        {
            return _context.T_TEST_RECS.Any(e => e.Id == id);
        }
    }
}
