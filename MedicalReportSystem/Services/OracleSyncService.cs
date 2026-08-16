using com.sun.org.apache.bcel.@internal.generic;
using Dapper;
using MedicalReportSystem.Models;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MedicalReportSystem.Services
{
    public class OracleSyncService : IHostedService, IDisposable
    {
        private readonly IConfiguration _config;
        private Timer _timer;
        private readonly ILogger<OracleSyncService> _logger;
        private readonly IWebHostEnvironment _env;

        public OracleSyncService(IConfiguration config, ILogger<OracleSyncService> logger, IWebHostEnvironment env)
        {
            _config = config;
            _logger = logger;
            _env = env;
        }

        //public async Task StartAsync(CancellationToken cancellationToken)
        //{
        //    _logger.LogInformation("Oracle同步服务启动...");

        //    try
        //    {
        //        // 检查取消请求
        //        cancellationToken.ThrowIfCancellationRequested();

        //        // 初始化时检查并同步表结构
        //        await ValidateAndSyncTableStructure(cancellationToken);

        //        // 每5分钟执行一次同步
        //        _timer = new Timer(
        //            callback: SyncData,
        //            state: null,
        //            dueTime: TimeSpan.Zero,
        //            period: TimeSpan.FromMinutes(5)
        //        );
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        _logger.LogInformation("服务启动被取消");
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "服务启动失败");
        //        throw;
        //    }
        //}
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Oracle同步服务启动...");

            try
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("开始检查并同步表结构...");

                        bool success = false;
                        int retryCount = 0;
                        int baseDelay = 10; // 初始延迟10秒

                        while (!success && !cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                retryCount++;
                                _logger.LogInformation("第 {RetryCount} 次尝试同步表结构...", retryCount);

                                await ValidateAndSyncTableStructure(cancellationToken);
                                success = true;
                                _logger.LogInformation("✅ 表结构同步成功！(共尝试 {RetryCount} 次)", retryCount);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex,
                                    "表结构同步失败，第 {RetryCount} 次尝试失败",
                                    retryCount);

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    _logger.LogInformation("表结构同步已被取消");
                                    return;
                                }

                                // 指数退避：10秒, 20秒, 40秒, 80秒... 最大5分钟
                                var delay = Math.Min(baseDelay * Math.Pow(2, retryCount - 1), 300);
                                var waitTime = TimeSpan.FromSeconds(delay);

                                _logger.LogInformation("等待 {WaitTime} 秒后重试...", waitTime.TotalSeconds);
                                await Task.Delay(waitTime, cancellationToken);
                            }
                        }

                        if (success)
                        {
                            _timer = new Timer(
                                callback: _ =>
                                {
                                    _ = Task.Run(() =>
                                    {
                                        try
                                        {
                                            SyncData(null);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "数据同步失败（已捕获，不影响站点）");
                                        }
                                    });
                                },
                                state: null,
                                dueTime: TimeSpan.FromSeconds(10),
                                period: TimeSpan.FromMinutes(5)
                            );

                            _logger.LogInformation("数据同步定时器已启动，每5分钟执行一次");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("表结构同步已被取消");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "同步服务初始化失败");
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动后台任务失败，但不影响站点");
            }

            return Task.CompletedTask;
        }
        private async void SyncData(object state)
        {
            try
            {
                _logger.LogInformation("开始数据同步...");

                // 1. 同步检验报告主表
                await SyncTable("t_test_rec", "ID", "INSTOCK_TIME");

                // 2. 同步检验结果明细表
                await SyncTable("t_testr_res_indicate", "ID", "INSTOCK_TIME");

                // 3. 同步微生物检验表
                await SyncTable("t_microbe_bacteria_res", "ID", "INSTOCK_TIME");

                // 4. 同步药敏结果表
                await SyncTable("t_microbe_suscept_res", "ID", "INSTOCK_TIME");
                //5. 
                await SyncTable("t_check_rec", "ID", "INSTOCK_TIME");

                _logger.LogInformation("数据同步完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据同步失败");
            }
        }
        /// <summary>
        /// 同步函数
        /// </summary>
        /// <param name="tableName">表明</param>
        /// <param name="idColumn">主键</param>
        /// <param name="timestampColumn">入库时间字段</param>
        /// <returns></returns>
        public async Task SyncTable(string tableName, string idColumn, string timestampColumn)
        {
            // 创建日志目录和文件路径
            var logDir = Path.Combine(_env.ContentRootPath, "Data", "SyncLogs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"{tableName}_sync.log");

            try
            {
                // 1. 获取上次同步时间（首次同步返回null）
                var lastSyncTime = await GetLastSyncTime(tableName);
                File.AppendAllText(logPath, $"[{DateTime.Now}]{Environment.NewLine} 1.开始同步表 {tableName},当前时间:{DateTime.Now}, 上次同步时间: {lastSyncTime?.ToString() ?? "首次同步"}{Environment.NewLine}");

                // 2. 连接数据库
                using var gbaseConn = new NpgsqlConnection(_config.GetConnectionString("OpenGaussConnection"));
                using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));

                await oracleConn.OpenAsync();
                await gbaseConn.OpenAsync();

                // 3. 构建查询SQL（区分首次和增量同步）
                string sql;
                //object parameters;

                if (lastSyncTime == null)
                {
                    // 首次同步 - 全量数据
                    // ✅ 修改：添加 INSTOCK_TIME::TEXT AS INSTOCK_TIME_RAW
                    sql = $@"SELECT * FROM sharedata.{tableName} ORDER BY {timestampColumn}";
                    //parameters = null;
                    File.AppendAllText(logPath, $"2.执行全量同步查询{Environment.NewLine},SQL: {sql} {Environment.NewLine}");
                }
                else
                {
                    // 增量同步 - 只获取更新的数据
                    // ✅ 修改：添加 INSTOCK_TIME::TEXT AS INSTOCK_TIME_RAW   , {timestampColumn}::TEXT AS {timestampColumn}_RAW
                    sql = $@"SELECT * FROM sharedata.{tableName} 
                     WHERE {timestampColumn} > @lastSyncTime
                     ORDER BY {timestampColumn}";
                    //arameters = new { lastSyncTime = lastSyncTime.Value.ToUniversalTime() };
                    File.AppendAllText(logPath, $"2.执行增量同步查询, 时间阈值: {lastSyncTime}{Environment.NewLine},SQL:  {sql} {Environment.NewLine}");
                }

                // 4. 分批处理数据
                const int batchSize = 1000;
                var totalCount = lastSyncTime == null
                    ? await gbaseConn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM sharedata.{tableName}")
                    : await gbaseConn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM sharedata.{tableName} WHERE {timestampColumn} > :lastSyncTime ", new { lastSyncTime });
                var sqltotalCount = $"SELECT COUNT(*) FROM sharedata.{tableName} WHERE {timestampColumn} > :lastSyncTime {lastSyncTime}";
                File.AppendAllText(logPath, $"3.发现待同步记录总数: {totalCount}{Environment.NewLine}sql:" + sqltotalCount + $"{Environment.NewLine}");

                if (totalCount == 0)
                {
                    File.AppendAllText(logPath, $"无新数据需要同步{Environment.NewLine}");
                    File.AppendAllText(logPath, $"--------------------------------------------------------------------------------------------------------------------------------{Environment.NewLine}");
                    _logger.LogInformation($"表 {tableName} 无新数据需要同步");
                    return;
                }

                DateTime? maxTimestampInBatch = null;

                for (int offset = 0; offset < totalCount; offset += batchSize)
                {
                    var batchSql = lastSyncTime == null
                        ? $"{sql} LIMIT {batchSize} OFFSET {offset}"
                        : $"{sql} LIMIT {batchSize} OFFSET {offset}";

                    File.AppendAllText(logPath, $"处理批次 {offset / batchSize + 1}, batchSqlSQL: {batchSql}{Environment.NewLine}");

                    var queryParams = new
                    {
                        lastSyncTime = lastSyncTime ,
                        limit = batchSize,
                        offset = offset
                    };

                    // 执行查询，返回 dynamic
                    var incrementalData = await gbaseConn.QueryAsync<dynamic>(batchSql, queryParams);


                    File.AppendAllText(logPath, $"处理批次 {offset / batchSize + 1}, SQL: {batchSql}{Environment.NewLine},数据:{Environment.NewLine}{JsonSerializer.Serialize(incrementalData, new JsonSerializerOptions { WriteIndented = true })}{Environment.NewLine}");

                    using var transaction = (OracleTransaction)await oracleConn.BeginTransactionAsync();
                    try
                    {
                        // 5. 执行批量MERGE操作（✅ 保持不变，仍使用 dynamic 对象）
                        await BulkMergeWithArrayBinding(oracleConn, transaction, tableName, incrementalData, idColumn);

                        // 6. 获取本批次的最大时间戳（✅ 保持不变，使用 DateTime 值）
                        var batchMaxTimestamp = incrementalData
                            .Select(r =>
                            {
                                var rawValue = ((IDictionary<string, object>)r)[timestampColumn];
                                var dt = ParseDateTime(rawValue);
                                File.AppendAllText(logPath, $"原始时间值: {rawValue} -> 解析结果: {dt}{Environment.NewLine}");
                                return dt;
                            })
                            .Max();

                        if (batchMaxTimestamp.HasValue)
                        {
                            maxTimestampInBatch = batchMaxTimestamp.Value.ToUniversalTime();
                            File.AppendAllText(logPath, $"本批次UTC最大时间戳: {maxTimestampInBatch:yyyy-MM-ddTHH:mm:ssZ}{Environment.NewLine}");
                        }
                        File.AppendAllText(logPath, $"本批次的最大时间戳:{batchMaxTimestamp.HasValue},maxTimestampInBatch: {maxTimestampInBatch},batchMaxTimestamp:{batchMaxTimestamp}{Environment.NewLine}");
                        File.AppendAllText(logPath, $"------------------------------------------------------------------------------------------------------------------------------------------{Environment.NewLine}");

                       await transaction.CommitAsync();
                        //File.AppendAllText(logPath, $"批次 {offset / batchSize + 1} 同步成功, 处理记录: {incrementalData.Count}\n");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        File.AppendAllText(logPath, $"批次 {offset / batchSize + 1} 同步失败: {ex.Message}{Environment.NewLine}");
                        _logger.LogError(ex, $"表 {tableName} 批次 {offset / batchSize + 1} 同步失败");
                        throw;
                    }
                }

                //File.AppendAllText(logPath, $"更新同步元数据:{maxTimestampInBatch.HasValue},maxTimestampInBatch: {maxTimestampInBatch},maxTimestampInBatchValue:{maxTimestampInBatch?.Value}\n");

                // 7. 更新同步元数据（✅ 保持不变）
                if (maxTimestampInBatch.HasValue)
                {
                    // 调用方代码
                    //string formattedTime = maxTimestampInBatch.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    //DateTime syncTime = DateTime.ParseExact(
                    //    formattedTime,
                    //    "yyyy-MM-ddTHH:mm:ssZ",
                    //    CultureInfo.InvariantCulture,
                    //    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                    //);
                    string  formattedTime = maxTimestampInBatch.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    await SetLastSyncTime(tableName, formattedTime);
                    File.AppendAllText(logPath, $"更新最后同步时间为: {maxTimestampInBatch.Value}{Environment.NewLine}");
                    File.AppendAllText(logPath, $"--------------------------------------------------------------------------------------------------------------------------------{Environment.NewLine}");
                }

                File.AppendAllText(logPath, $"表 {tableName} 同步完成, 共处理 {totalCount} 条记录{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"同步过程中发生错误: {ex}{Environment.NewLine}");
                _logger.LogError(ex, $"表 {tableName} 同步失败");
                throw;
            }
        }

        public async Task SyncTable2(string tableName, string idColumn, string timestampColumn)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Data", "SyncLogs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"{tableName}_sync.log");

            try
            {
                // 初始化时间处理器
                var timeProcessor = new TimeFieldProcessor(_logger, logPath, _config);

                // 1. 获取上次同步时间
                var lastSyncTime = await GetLastSyncTime(tableName);
                File.AppendAllText(logPath, $"[{DateTime.Now}] 开始同步表 {tableName},获取上次同步时间{lastSyncTime};\n");

                // 2. 获取表的所有时间字段
                var timeFields = await timeProcessor.GetTableTimeFields(tableName);
                timeFields.Add(timestampColumn); // 确保包含主时间字段
                timeFields = timeFields.Distinct().ToList();

                File.AppendAllText(logPath, $"识别到时间字段: {string.Join(", ", timeFields)}\n");

                // 3. 连接数据库
                using var gbaseConn = new NpgsqlConnection(_config.GetConnectionString("OpenGaussConnection"));
                using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));

                await oracleConn.OpenAsync();
                await gbaseConn.OpenAsync();

                // 4. 构建查询SQL
                string sql;
                object parameters;

                if (lastSyncTime == null)
                {
                    sql = $@"SELECT * FROM sharedata.{tableName} 
            ORDER BY {timestampColumn}";
                    parameters = null;
                }
                else
                {
                    // 使用PostgreSQL的TO_CHAR函数获取原始时间字符串
                    sql = $@"SELECT 
            *,
            TO_CHAR({timestampColumn}, 'YYYY-MM-DD HH24:MI:SS.US TZH:TZM') AS time_original_value
            FROM sharedata.{tableName} 
            WHERE {timestampColumn} > @lastSyncTime
            AND id = @recordId
            ORDER BY {timestampColumn}";

                    parameters = new
                    {
                        lastSyncTime = lastSyncTime,
                        recordId = "b9a3dc05f8304c88bf39bac146c93ed1"
                    };
                }

                // 5. 分批处理数据
                const int batchSize = 1000;
                var totalCount = lastSyncTime == null
                    ? await gbaseConn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM sharedata.{tableName}")
                    : await gbaseConn.ExecuteScalarAsync<int>(
                        $"SELECT COUNT(*) FROM sharedata.{tableName} WHERE {timestampColumn} > :lastSyncTime",
                        new { lastSyncTime });

                if (totalCount == 0)
                {
                    File.AppendAllText(logPath, "无新数据需要同步\n");
                    return;
                }

                DateTimeOffset? maxTimestampInBatch = null;

                for (int offset = 0; offset < totalCount; offset += batchSize)
                {
                    var batchSql = $"{sql} LIMIT {batchSize} OFFSET {offset}";
                    File.AppendAllText(logPath, $"[{DateTime.Now}] 处理批次 {offset / batchSize + 1},batchSql:{batchSql};\n");

                    var incrementalData = (await gbaseConn.QueryAsync(batchSql, parameters)).ToList();

                    // 处理时间字段
                    // 处理每个记录的时间字段
                    foreach (var record in incrementalData)
                    {
                        var dict = (IDictionary<string, object>)record;

                        // 获取原始时间字符串和数据库值
                        var originalTimeStr = dict[$"{timestampColumn}_original"] as string;
                        var dbTimeValue = dict[timestampColumn];

                        // 解析原始时间（使用上文的ParseOriginalTime方法）
                        var correctTime = ParseOriginalTime(originalTimeStr, dbTimeValue);

                        if (correctTime.HasValue)
                        {
                            // 替换为正确的时间值
                            dict[timestampColumn] = correctTime.Value;

                            // 记录调试信息
                            File.AppendAllText(logPath,
                                $"[{DateTime.Now}] 时间修正结果:\n" +
                                $"原始值: {originalTimeStr}\n" +
                                $"数据库转换值: {dbTimeValue}\n" +
                                $"修正后UTC: {correctTime.Value.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff}\n" +
                                $"本地时间: {correctTime.Value.LocalDateTime:yyyy-MM-dd HH:mm:ss.fff}\n");
                        }
                    }

                    //using var transaction = await oracleConn.BeginTransactionAsync();
                    using var transaction = await oracleConn.BeginTransactionAsync() as OracleTransaction;
                    if (transaction == null)
                    {
                        throw new InvalidOperationException("无法获取Oracle事务");
                    }
                    try
                    {
                        // 执行批量MERGE操作
                        await BulkMergeWithArrayBinding(oracleConn, transaction, tableName, incrementalData, idColumn);

                        // 获取本批次的最大时间戳
                        var batchMaxTimestamp = incrementalData
                            .Select(r => ((IDictionary<string, object>)r)[timestampColumn] as DateTimeOffset?)
                            .Max();

                        if (batchMaxTimestamp.HasValue &&
                           (maxTimestampInBatch == null || batchMaxTimestamp > maxTimestampInBatch))
                        {
                            maxTimestampInBatch = batchMaxTimestamp;
                        }

                        await transaction.CommitAsync();
                        File.AppendAllText(logPath, $"批次处理成功, 记录数: {incrementalData.Count}\n");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        File.AppendAllText(logPath, $"批次处理失败: {ex.Message}\n");
                        throw;
                    }
                }

                // 更新同步元数据
                if (maxTimestampInBatch.HasValue)
                {
                    // 调用方代码
                    
                    string  formattedTime = maxTimestampInBatch.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    await SetLastSyncTime(tableName, formattedTime);
                    File.AppendAllText(logPath, $"更新最后同步时间为: {maxTimestampInBatch.Value}\n");
                }

                File.AppendAllText(logPath, $"[{DateTime.Now}] 同步完成, 共处理 {totalCount} 条记录\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] 同步失败: {ex}\n");
                throw;
            }
        }
        public async Task SyncTable1(string tableName, string idColumn, string timestampColumn)
        {
            // 创建日志目录和文件路径
            var logDir = Path.Combine(_env.ContentRootPath, "Data", "SyncLogs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"{tableName}_sync.log");

            try
            {
                // 1. 获取上次同步时间（首次同步返回null）
                var lastSyncTime = await GetLastSyncTime(tableName);
                File.AppendAllText(logPath, $"[{DateTime.Now}] 开始同步表 {tableName}当前时间:{DateTime.Now}, 上次同步时间: {lastSyncTime?.ToString() ?? "首次同步"}\n");

                // 2. 连接数据库
                using var gbaseConn = new NpgsqlConnection(_config.GetConnectionString("OpenGaussConnection"));
                using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));

                await oracleConn.OpenAsync();
                await gbaseConn.OpenAsync();

                // 3. 构建查询SQL（区分首次和增量同步）
                string sql;
                object parameters;

                if (lastSyncTime == null)
                {
                    // 首次同步 - 全量数据
                    sql = $@"SELECT * FROM sharedata.{tableName} ORDER BY {timestampColumn}";
                    parameters = null;
                    File.AppendAllText(logPath, "执行全量同步查询\n,SQL:" + sql + ";\n");
                }
                else
                {
                    // 增量同步 - 只获取更新的数据
                    //sql = $@"SELECT * FROM sharedata.{tableName} 
                    //WHERE {timestampColumn} > :lastSyncTime
                    //ORDER BY {timestampColumn}";
                    //parameters = new { lastSyncTime };
                    // 使用参数化查询确保时间格式正确
                    sql = $@"SELECT * FROM sharedata.{tableName} 
                     WHERE {timestampColumn} > @lastSyncTime and id='b9a3dc05f8304c88bf39bac146c93ed1'
                     ORDER BY {timestampColumn}";
                    //parameters = new { lastSyncTime = lastSyncTime.Value.ToUniversalTime() };
                    File.AppendAllText(logPath, $"执行增量同步查询, 时间阈值: {lastSyncTime}\n,SQL:" + sql + "\n");
                }

                // 4. 分批处理数据
                const int batchSize = 1000;
                var totalCount = lastSyncTime == null
                    ? await gbaseConn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM sharedata.{tableName}")
                    : await gbaseConn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM sharedata.{tableName} WHERE {timestampColumn} > :lastSyncTime and id='b9a3dc05f8304c88bf39bac146c93ed1'", new { lastSyncTime });
                var sqltotalCount = $"SELECT COUNT(*) FROM sharedata.{tableName} WHERE {timestampColumn} > :lastSyncTime {lastSyncTime}";
                File.AppendAllText(logPath, $"发现待同步记录总数: {totalCount}\n,sql:" + sqltotalCount + "\n");

                if (totalCount == 0)
                {
                    File.AppendAllText(logPath, "无新数据需要同步\n");
                    _logger.LogInformation($"表 {tableName} 无新数据需要同步");
                    return;
                }

                DateTime? maxTimestampInBatch = null;

                for (int offset = 0; offset < totalCount; offset += batchSize)
                {
                    var batchSql = lastSyncTime == null
                        ? $"{sql} LIMIT {batchSize} OFFSET {offset}"
                        : $"{sql} LIMIT {batchSize} OFFSET {offset}";

                    File.AppendAllText(logPath, $"处理批次 {offset / batchSize + 1}, batchSqlSQL: {batchSql}\n");

                    //var incrementalData = (await gbaseConn.QueryAsync(batchSql, parameters)).ToList();
                    //TEST_REPORT_DATE,UPDATE_TIME,INSERT_DATACENTER_TIME,TEST_AUDIT_TIME
                    //ID, INSTOCK_TIME,ACCEPT_SAMPLE_TIME,APPLICATION_TIME,BIRTH_DATE,BUSINESS_GENER_TIME,SAMPLE_TIME,TEST_REC_TIME,TEST_REPORT_DATE,UPDATE_TIME,INSERT_DATACENTER_TIME,TEST_AUDIT_TIME
                    //var batchSql = "SELECT ID, TEST_AUDIT_TIME,INSTOCK_TIME FROM sharedata.t_test_rec ORDER BY INSTOCK_TIME LIMIT :limit OFFSET :offset";
                    var queryParams = new
                    {
                        lastSyncTime = lastSyncTime, // 确保总是有这个字段
                        limit = batchSize,
                        offset = offset
                    };


                    //var sql = "SELECT INSTOCK_TIME FROM your_table WHERE ...";
                    // ✅ 正确：使用 dynamic
                    // var incrementalData = (await gbaseConn.QueryAsync<dynamic>(batchSql, queryParams)).ToList();
                    // 执行查询，返回 dynamic（实际是 DapperRow）
                    var incrementalData = await gbaseConn.QueryAsync<dynamic>(batchSql, queryParams);
                    // ✅ DapperRow 实现了 IDictionary<string, object>，可以直接作为字典使用
                    foreach (IDictionary<string, object> row in incrementalData)
                    {
                        IDictionary<string, object> dict = row; // ✅ 合法！DapperRow 实现了 IDictionary<string, object>

                        foreach (var kvp in dict)
                        {
                            File.AppendAllText(logPath, $"记录原始值INSTOCK_TIME {kvp.Key},(类型: {kvp.Value?.GetType()})\n");
                            Console.WriteLine($"{kvp.Key}: {kvp.Value} ({kvp.Value?.GetType()})");
                        }
                    }
                    //foreach (var record in incrementalData)
                    //{
                    //    var dict = (IDictionary<string, object>)record;
                    //    var instockTime = dict["INSTOCK_TIME"]; // 这里获取的是原始值

                    //    // 记录原始值
                    //    File.AppendAllText(logPath, $"记录原始值INSTOCK_TIME {instockTime},(类型: {instockTime.GetType()})\n");
                    //    Console.WriteLine($"原始 INSTOCK_TIME 值: {instockTime} (类型: {instockTime.GetType()})");
                    //}
                    //var incrementalData = (await gbaseConn.QueryAsync(batchSql, new
                    //{
                    //    limit = batchSize,
                    //    offset = offset
                    //})).ToList();
                    //File.AppendAllText(logPath, $"处理批次 {offset / batchSize + 1}, SQL: {batchSql}\n,数据:{incrementalData}");

                    File.AppendAllText(logPath, $"处理批次 {offset / batchSize + 1}, SQL: {batchSql}\n,数据:\n{JsonSerializer.Serialize(incrementalData, new JsonSerializerOptions { WriteIndented = true })}\n");

                    using var transaction = (OracleTransaction)await oracleConn.BeginTransactionAsync();
                    try
                    {
                        // 5. 执行批量MERGE操作
                        await BulkMergeWithArrayBinding(oracleConn, transaction, tableName, incrementalData, idColumn);

                        // 6. 获取本批次的最大时间戳
                        //var batchMaxTimestamp = incrementalData
                        //    .Select(r => ((IDictionary<string, object>)r)[timestampColumn] as DateTime?)
                        //    .Max();
                        // 修改后的批次最大时间戳获取逻辑
                        // 6. 获取本批次的最大时间戳（修复后）
                        var batchMaxTimestamp = incrementalData
                            .Select(r => {
                                var rawValue = ((IDictionary<string, object>)r)[timestampColumn];
                                var dt = ParseDateTime(rawValue);
                                File.AppendAllText(logPath, $"原始时间值: {rawValue} -> 解析结果: {dt}\n");
                                return dt;
                            })
                            .Max();

                        //if (batchMaxTimestamp.HasValue)
                        //{
                        //    if (maxTimestampInBatch == null || batchMaxTimestamp > maxTimestampInBatch)
                        //    {
                        //        maxTimestampInBatch = batchMaxTimestamp;
                        //    }
                        //}
                        if (batchMaxTimestamp.HasValue)
                        {
                            // 转换为UTC时间确保一致性
                            maxTimestampInBatch = batchMaxTimestamp.Value.ToUniversalTime();
                            File.AppendAllText(logPath, $"本批次UTC最大时间戳: {maxTimestampInBatch:yyyy-MM-ddTHH:mm:ssZ}\n");
                        }
                        File.AppendAllText(logPath, $"本批次的最大时间戳:{batchMaxTimestamp.HasValue},maxTimestampInBatch: {maxTimestampInBatch},batchMaxTimestamp:{batchMaxTimestamp}\n");
                        await transaction.CommitAsync();
                        //File.AppendAllText(logPath, $"批次 {offset / batchSize + 1} 同步成功, 处理记录: {incrementalData.Count}\n");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        File.AppendAllText(logPath, $"批次 {offset / batchSize + 1} 同步失败: {ex.Message}\n");
                        _logger.LogError(ex, $"表 {tableName} 批次 {offset / batchSize + 1} 同步失败");
                        throw;
                    }
                }
                File.AppendAllText(logPath, $"更新同步元数据:{maxTimestampInBatch.HasValue},maxTimestampInBatch: {maxTimestampInBatch},maxTimestampInBatchValue:{maxTimestampInBatch.Value}\n");
                // 7. 更新同步元数据
                if (maxTimestampInBatch.HasValue)
                {
                    string formattedTime = maxTimestampInBatch.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    await SetLastSyncTime(tableName, formattedTime);
                    File.AppendAllText(logPath, $"更新最后同步时间为: {maxTimestampInBatch.Value}\n");
                }

                File.AppendAllText(logPath, $"表 {tableName} 同步完成, 共处理 {totalCount} 条记录\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"同步过程中发生错误: {ex}\n");
                _logger.LogError(ex, $"表 {tableName} 同步失败");
                throw;
            }
        }
        private DateTimeOffset? ParseOriginalTime(string originalString, object dbValue)
        {
            if (string.IsNullOrEmpty(originalString))
                return null;

            try
            {
                // 1. 尝试解析PostgreSQL原始格式 "2025-07-18 10:25:31.000 +0800"
                if (Regex.IsMatch(originalString, @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{4}"))
                {
                    var pgDateTime = DateTime.ParseExact(
                        originalString,
                        "yyyy-MM-dd HH:mm:ss.fff zzz",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal);

                    return new DateTimeOffset(pgDateTime);
                }

                // 2. 尝试解析ISO 8601格式
                if (DateTimeOffset.TryParse(originalString,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var isoDateTimeOffset))
                {
                    return isoDateTimeOffset;
                }

                // 3. 处理数据库返回的值
                switch (dbValue)
                {
                    case DateTimeOffset existingDto:
                        return existingDto;

                    case DateTime dateTime:
                        return dateTime.Kind switch
                        {
                            DateTimeKind.Utc => new DateTimeOffset(dateTime, TimeSpan.Zero),
                            DateTimeKind.Local => new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime)),
                            _ => new DateTimeOffset(dateTime, TimeSpan.Zero) // 假设未指定时为UTC
                        };

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"时间解析失败: {originalString} - {ex.Message}");
                return null;
            }
        }
        private DateTime? ParseDateTime(object timeValue)
        {
            if (timeValue == null) return null;

            // 处理字符串格式（如 "2025-07-18T02:25:31Z"）
            if (timeValue is string str)
            {
                if (DateTimeOffset.TryParse(str, out var dto1))
                    return dto1.UtcDateTime;
                if (DateTime.TryParse(str, out var dt1))
                    return dt1;
            }

            // 处理DateTime/DateTimeOffset
            if (timeValue is DateTime dt) return dt;
            if (timeValue is DateTimeOffset dto) return dto.UtcDateTime;

            // 其他类型尝试转换
            try
            {
                return Convert.ToDateTime(timeValue);
            }
            catch
            {
                return null;
            }
        }
        //private async Task BulkMergeWithArrayBinding(
        //                    OracleConnection connection,
        //                    OracleTransaction transaction,
        //                    string tableName,
        //                    IEnumerable<dynamic> data, string idColumn)
        //{
        //    if (!data.Any()) return;

        //    var firstRecord = (IDictionary<string, object>)data.First();
        //    var columnNames = firstRecord.Keys.ToList();
        //    var mergeSql = BuildMergeSql(tableName, columnNames, idColumn);
        //    var logDir = Path.Combine(_env.ContentRootPath, "Data", "SyncLogs");
        //    var logPath = Path.Combine(logDir, $"{tableName}_sync1.log");
        //    // 添加日志输
        //    _logger.LogInformation($"执行 MERGE SQL:\n{mergeSql}");
        //    File.AppendAllText(logPath, $"执行 MERGE SQL:\n{mergeSql}\n");

        //    using var cmd = new OracleCommand(mergeSql, connection);
        //    cmd.Transaction = transaction;  // 关键修正点
        //    cmd.ArrayBindCount = data.Count();
        //    cmd.BindByName = true;  // 显式启用按名称绑定[7](@ref)

        //    //foreach (var column in columnNames)
        //    //{
        //    //    var oracleType = GetOracleDbType(firstRecord[column]);
        //    //    var values = data.Select(x =>
        //    //        ((IDictionary<string, object>)x).TryGetValue(column, out var val) ? val : DBNull.Value
        //    //    ).ToArray();

        //    //    var param = cmd.Parameters.Add(column, oracleType);
        //    //    param.Value = values;

        //    //    // 设置ArrayBindSize（以Varchar2为例）
        //    //    if (oracleType == OracleDbType.Varchar2)
        //    //        param.ArrayBindSize = values.Select(v => v?.ToString()?.Length ?? 0).ToArray();
        //    //}
        //    foreach (var column in columnNames)
        //    {
        //        // 特殊处理时间字段
        //        //if (column.Equals("TEST_AUDIT_TIME", StringComparison.OrdinalIgnoreCase) ||
        //        //    column.Equals("UPDATE_TIME", StringComparison.OrdinalIgnoreCase) ||
        //        //    column.Equals("INSERT_DATACENTER_TIME", StringComparison.OrdinalIgnoreCase) ||
        //        //    column.Equals("TEST_REPORT_DATE", StringComparison.OrdinalIgnoreCase))
        //        //{
        //        //    var values = data.Select(x =>
        //        //    {
        //        //        var dict = (IDictionary<string, object>)x;
        //        //        return dict.TryGetValue(column, out var val) && val != null ?
        //        //            ConvertToOracleTimeStamp(val) :
        //        //            (object)DBNull.Value;
        //        //    }).ToArray();

        //        //    var param = cmd.Parameters.Add(column, OracleDbType.TimeStamp);
        //        //    param.Value = values;
        //        //}
        //        //else
        //        //{
        //        //    var oracleType = GetOracleDbType(firstRecord[column]);
        //        //    var values = data.Select(x =>
        //        //        ((IDictionary<string, object>)x).TryGetValue(column, out var val) ? val : DBNull.Value
        //        //    ).ToArray();

        //        //    var param = cmd.Parameters.Add(column, oracleType);
        //        //    param.Value = values;

        //        //    if (oracleType == OracleDbType.Varchar2)
        //        //        param.ArrayBindSize = values.Select(v => v?.ToString()?.Length ?? 0).ToArray();
        //        //}
        //        var oracleType = GetOracleDbType(firstRecord[column]);
        //        var values = data.Select(x =>
        //            ((IDictionary<string, object>)x).TryGetValue(column, out var val) ? val : DBNull.Value
        //        ).ToArray();

        //        var param = cmd.Parameters.Add(column, oracleType);
        //        param.Value = values;

        //        if (oracleType == OracleDbType.Varchar2)
        //            param.ArrayBindSize = values.Select(v => v?.ToString()?.Length ?? 0).ToArray();
        //    }

        //    await cmd.ExecuteNonQueryAsync();
        //}
        private async Task BulkMergeWithArrayBinding(
                    OracleConnection connection,
                    OracleTransaction transaction,
                    string tableName,
                    IEnumerable<dynamic> data,
                    string idColumn)
        {
            if (!data.Any()) return;

            var firstRecord = (IDictionary<string, object>)data.First();
            var columnNames = firstRecord.Keys.ToList();
            var mergeSql = BuildMergeSql(tableName, columnNames, idColumn);

            // 日志路径
            var logDir = Path.Combine(_env.ContentRootPath, "Data", "SyncLogs");
            var logPath = Path.Combine(logDir, $"{tableName}_bulk_merge.log");

            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 开始强制字符串格式批量MERGE\n");
            File.AppendAllText(logPath, $"表名: {tableName}\n列列表: {string.Join(", ", columnNames)}\n");

            using var cmd = new OracleCommand(mergeSql, connection);
            cmd.Transaction = transaction;
            cmd.ArrayBindCount = data.Count();
            cmd.BindByName = true;

            foreach (var column in columnNames)
            {
                try
                {
                    // 获取原始值并确保为字符串或DBNull
                    var values = data.Select<dynamic, object>(x =>
                    {
                        var dict = (IDictionary<string, object>)x;
                        if (!dict.TryGetValue(column, out var val) || val == null)
                        {
                            return DBNull.Value;
                        }

                        if (val is DateTime dt)
                        {
                            return dt.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        }
                        else if (val is DateTimeOffset dto)
                        {
                            return dto.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        }

                        return val.ToString();
                    }).ToArray();

                    // 记录处理详情
                    File.AppendAllText(logPath, $"\n列名: {column}\n");
                    File.AppendAllText(logPath, $"值示例(前3条): {string.Join(" | ", values.Take(3).Select(v => v is DBNull ? "NULL" : v.ToString()))}\n");

                    // 始终作为VARCHAR2处理
                    var param = cmd.Parameters.Add(column, OracleDbType.Varchar2);
                    param.Value = values;

                    // 计算并设置字符串长度
                    var sizes = values.Select(v =>
                        v is DBNull ? 0 : v.ToString().Length).ToArray();
                    param.ArrayBindSize = sizes;

                    File.AppendAllText(logPath, $"设置ArrayBindSize: {string.Join(", ", sizes.Take(3))}...\n");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(logPath, $"处理列 {column} 时发生错误: {ex}\n");
                    throw;
                }
            }

            try
            {
                File.AppendAllText(logPath, $"\n开始执行MERGE操作...\n");
                var sw = Stopwatch.StartNew();

                await cmd.ExecuteNonQueryAsync();

                sw.Stop();
                File.AppendAllText(logPath, $"MERGE操作完成，耗时: {sw.ElapsedMilliseconds}ms\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"MERGE操作失败: {ex}\n");
                throw;
            }
            finally
            {
                File.AppendAllText(logPath, $"\n批量MERGE操作结束\n\n");
            }
        }
        // 辅助方法：将各种时间格式转换为Oracle时间戳
        private object ConvertToOracleTimeStamp(object timeValue)
        {
            if (timeValue == null) return DBNull.Value;

            if (timeValue is DateTime dt)
                return dt;

            if (timeValue is string str && DateTime.TryParse(str, out var parsedDt))
                return parsedDt;

            // 处理其他可能的时间格式
            return DBNull.Value;
        }
        //private string BuildMergeSql(string tableName, List<string> columns, string idColumn)
        //{
        //    var normalizedIdColumn = idColumn.ToUpper();
        //    //var setClause = string.Join(", ",
        //    //    columns.Where(c => c != idColumn)
        //    //          .Select(c => $"target.{c} = :{c}"));
        //    var setClause = string.Join(", ",
        //        columns.Where(c => !c.Equals(idColumn, StringComparison.OrdinalIgnoreCase))
        //            .Select(c => $"target.\"{c.ToUpper()}\" = :{c}"));

        //    var insertColumns = string.Join(", ", columns);
        //    var insertValues = string.Join(", ", columns.Select(c => $":{c}"));

        //    return $@"
        //    MERGE INTO ZLHIS.{tableName} target
        //    USING (SELECT 1 FROM DUAL) src
        //    ON (target.{idColumn} = :{idColumn})
        //    WHEN MATCHED THEN
        //        UPDATE SET {setClause}
        //    WHEN NOT MATCHED THEN
        //    INSERT ({insertColumns}) 
        //    VALUES ({insertValues})";
        //}
        private string BuildMergeSql(string tableName, List<string> columns, string idColumn)
        {
            // 确保使用主键/唯一键作为匹配条件
            var normalizedIdColumn = idColumn.ToUpper();

            var setClause = string.Join(", ",
                columns.Where(c => !c.Equals(idColumn, StringComparison.OrdinalIgnoreCase))
                      .Select(c => $"target.\"{c.ToUpper()}\" = :{c}"));

            var insertColumns = string.Join(", ", columns.Select(c => $"\"{c.ToUpper()}\""));
            var insertValues = string.Join(", ", columns.Select(c => $":{c}"));

            return $@"
        MERGE INTO ZLHIS.{tableName.ToUpper()} target
        USING(SELECT 1 FROM DUAL) src
        ON(target.{normalizedIdColumn}= :{idColumn})
        WHEN MATCHED THEN
            UPDATE SET {setClause}
            WHEN NOT MATCHED THEN
            INSERT({insertColumns}) 
            VALUES({insertValues})";
        }
        private OracleDbType GetOracleDbType(object value)
        {
            if (value == null) return OracleDbType.Varchar2;

            return value switch
            {
                string _ => OracleDbType.Varchar2,
                int _ => OracleDbType.Int32,
                long _ => OracleDbType.Int64,
                decimal _ => OracleDbType.Decimal,
                DateTime _ => OracleDbType.Date,
                bool _ => OracleDbType.Char,
                byte[] _ => OracleDbType.Blob,
                _ => OracleDbType.Varchar2
            };
        }
        private async Task<DateTime?> GetLastSyncTime1(string tableName)
        {
            try
            {
                using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));

                // 检查是否是首次同步
                var checkSql = "SELECT COUNT(*) FROM ZLHIS.SYNC_METADATA WHERE TABLE_NAME = :tableName";
                var exists = await oracleConn.ExecuteScalarAsync<int>(checkSql, new { tableName });

                // 首次同步返回null表示同步所有数据
                if (exists == 0) return null;

                // 非首次同步返回记录的时间
                var sql = "SELECT LAST_SYNC_TIME FROM ZLHIS.SYNC_METADATA WHERE TABLE_NAME = :tableName";
                return await oracleConn.QueryFirstOrDefaultAsync<DateTime?>(sql, new { tableName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取{tableName}的最后同步时间失败");
                return null; // 出错时也执行全量同步
            }
        }
        private async Task<DateTime?> GetLastSyncTime2(string tableName)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Data", "SyncLogs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"{tableName}_sync_metadata.log");

            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] 开始获取表 {tableName} 的最后同步时间\n");

                using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));
                await oracleConn.OpenAsync();

                // 1. 检查是否是首次同步
                var checkSql = "SELECT COUNT(*) FROM ZLHIS.SYNC_METADATA WHERE TABLE_NAME = :tableName";
                var exists = await oracleConn.ExecuteScalarAsync<int>(checkSql, new { tableName });

                File.AppendAllText(logPath, $"检查表是否存在: {(exists > 0 ? "存在" : "不存在")}\n");

                // 首次同步返回null表示同步所有数据
                if (exists == 0)
                {
                    File.AppendAllText(logPath, "首次同步，返回 null\n");
                    return null;
                }

                // 2. 获取最后同步时间
                //var sql = "SELECT LAST_SYNC_TIME FROM ZLHIS.SYNC_METADATA WHERE TABLE_NAME = :tableName";
                //var lastSyncTime = await oracleConn.QueryFirstOrDefaultAsync<DateTime?>(sql, new { tableName });
                var sql = @"
    SELECT 
        TO_CHAR(LAST_SYNC_TIME, 'YYYY-MM-DD HH24:MI:SS') AS LAST_SYNC_TIME_STR 
    FROM ZLHIS.SYNC_METADATA 
    WHERE TABLE_NAME = :tableName";

                var result = await oracleConn.QueryFirstOrDefaultAsync<dynamic>(sql, new { tableName });
                var lastSyncTime = result != null ?
                    DateTime.ParseExact(result.LAST_SYNC_TIME_STR, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) :
                    (DateTime?)null;

                // 3. 记录原始时间值（包括时区信息）
                var detailedSql = @"
            SELECT 
                LAST_SYNC_TIME AS raw_time,
                TO_CHAR(LAST_SYNC_TIME, 'YYYY-MM-DD HH24:MI:SS') AS formatted_time,
                EXTRACT(TIMEZONE_HOUR FROM LAST_SYNC_TIME) AS tz_hour,
                EXTRACT(TIMEZONE_MINUTE FROM LAST_SYNC_TIME) AS tz_minute
            FROM ZLHIS.SYNC_METADATA 
            WHERE TABLE_NAME = :tableName";

                var timeDetails = await oracleConn.QueryFirstOrDefaultAsync<dynamic>(detailedSql, new { tableName });

                File.AppendAllText(logPath, $"从Oracle获取的原始时间值: {timeDetails?.raw_time}\n");
                File.AppendAllText(logPath, $"格式化时间: {timeDetails?.formatted_time}\n");
                File.AppendAllText(logPath, $"时区偏移: {timeDetails?.tz_hour}:{timeDetails?.tz_minute}\n");
                File.AppendAllText(logPath, $"最终返回的时间值: {lastSyncTime}\n");

                return lastSyncTime;
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"获取最后同步时间失败: {ex}\n");
                _logger.LogError(ex, $"获取{tableName}的最后同步时间失败");
                return null; // 出错时也执行全量同步
            }
        }
        private async Task<string> GetLastSyncTime(string tableName)
        {
            try
            {
                using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));
                await oracleConn.OpenAsync();

                // 查询带时区的时间字符串
                var sql = @"
            SELECT TO_CHAR(
                FROM_TZ(CAST(LAST_SYNC_TIME AS TIMESTAMP), 'UTC') AT TIME ZONE 'Asia/Shanghai',
                'YYYY-MM-DD HH24:MI:SS.FF TZHTZM'
            ) AS sync_time
            FROM ZLHIS.SYNC_METADATA 
            WHERE TABLE_NAME = :tableName";

                return await oracleConn.ExecuteScalarAsync<string>(sql, new { tableName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取{tableName}的最后同步时间失败");
                return null;
            }
        }

        //private async Task SetLastSyncTime(string tableName, DateTimeOffset syncTime)
        //{
        //    try
        //    {
        //        using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));

        //        var mergeSql = @"MERGE INTO ZLHIS.SYNC_METADATA target
        //                USING (SELECT :tableName AS TABLE_NAME FROM DUAL) src
        //                ON (target.TABLE_NAME = src.TABLE_NAME)
        //                WHEN MATCHED THEN
        //                    UPDATE SET 
        //                        LAST_SYNC_TIME = :syncTime,
        //                        LAST_SUCCESS_TIME = SYSTIMESTAMP AT TIME ZONE 'UTC',
        //                        RECORDS_SYNCED = NVL(RECORDS_SYNCED,0) + :recordCount
        //                WHEN NOT MATCHED THEN
        //                    INSERT (TABLE_NAME, LAST_SYNC_TIME, LAST_SUCCESS_TIME, RECORDS_SYNCED)
        //                    VALUES (:tableName, :syncTime, SYSTIMESTAMP AT TIME ZONE 'UTC', :recordCount)";

        //        await oracleConn.ExecuteAsync(mergeSql, new
        //        {
        //            tableName,
        //            syncTime = syncTime.UtcDateTime,
        //            recordCount = 1
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"更新{tableName}的最后同步时间失败");
        //        throw;
        //    }
        //}
        //private async Task SetLastSyncTime(string tableName, DateTime syncTime)
        //{
        //    try
        //    {
        //        using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));

        //        // 方案A：更新到数据库
        //        var mergeSql = @"
        //    MERGE INTO ZLHIS.SYNC_METADATA target
        //    USING (SELECT :tableName AS TABLE_NAME FROM DUAL) src
        //    ON (target.TABLE_NAME = src.TABLE_NAME)
        //    WHEN MATCHED THEN
        //        UPDATE SET 
        //            LAST_SYNC_TIME = :syncTime,
        //            LAST_SUCCESS_TIME = SYSTIMESTAMP,
        //            RECORDS_SYNCED = NVL(RECORDS_SYNCED,0) + :recordCount
        //    WHEN NOT MATCHED THEN
        //        INSERT (TABLE_NAME, LAST_SYNC_TIME, LAST_SUCCESS_TIME, RECORDS_SYNCED)
        //        VALUES (:tableName, :syncTime, SYSTIMESTAMP, :recordCount)";

        //        // 假设recordCount可以从其他地方获取
        //        await oracleConn.ExecuteAsync(mergeSql, new
        //        {
        //            tableName,
        //            syncTime,
        //            recordCount = 1 // 实际应该传入本次同步的记录数
        //        });

        //        /* 方案B：更新到文件
        //        var dirPath = "SyncData";
        //        Directory.CreateDirectory(dirPath);
        //        var filePath = Path.Combine(dirPath, $"{tableName}.lastSync");
        //        await File.WriteAllTextAsync(filePath, syncTime.ToString("o"));
        //        */
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"更新{tableName}的最后同步时间失败");
        //        // 可以添加重试逻辑
        //    }
        //}
        private async Task SetLastSyncTime(string tableName, string isoTimeString)
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Data", "SyncLogs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"{tableName}_sync_metadata.log");

            try
            {
                ConvertToOracleTimeStamp(isoTimeString);
                // 解析 ISO 8601 时间
                DateTimeOffset syncTime1 = DateTimeOffset.ParseExact(
                    isoTimeString,
                    "yyyy-MM-ddTHH:mm:ssZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal
                );
                File.AppendAllText(logPath, $"[{DateTime.Now}] ;{Environment.NewLine}{syncTime1}1.开始更新同步元数据{Environment.NewLine}");
                File.AppendAllText(logPath, $"2.表名: {tableName}, 同步时间: {isoTimeString}{Environment.NewLine}");

                using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));
                await oracleConn.OpenAsync();
                File.AppendAllText(logPath, "3.成功连接到Oracle数据库{Environment.NewLine}");

                // 1. 检查SYNC_METADATA表是否存在
                var checkTableSql = "SELECT COUNT(*) FROM ALL_TABLES WHERE TABLE_NAME = 'SYNC_METADATA' AND OWNER = 'ZLHIS'";
                var tableExists = await oracleConn.ExecuteScalarAsync<int>(checkTableSql);
                File.AppendAllText(logPath, $"4.检查SYNC_METADATA表是否存在: {tableExists > 0}{Environment.NewLine}");

                if (tableExists == 0)
                {
                    File.AppendAllText(logPath, $"SYNC_METADATA表不存在，尝试创建{Environment.NewLine}");
                    var createTableSql = @"
                CREATE TABLE ZLHIS.SYNC_METADATA (
                    TABLE_NAME VARCHAR2(100) PRIMARY KEY,
                    LAST_SYNC_TIME TIMESTAMP,
                    LAST_SUCCESS_TIME TIMESTAMP,
                    RECORDS_SYNCED NUMBER DEFAULT 0,
                    LAST_ERROR_MESSAGE VARCHAR2(4000)
                )";
                    await oracleConn.ExecuteAsync(createTableSql);
                    File.AppendAllText(logPath, $"成功创建SYNC_METADATA表{Environment.NewLine}");
                }

                // 2. 执行MERGE操作
                var mergeSql = @"
            MERGE INTO ZLHIS.SYNC_METADATA target
            USING (SELECT :tableName AS TABLE_NAME FROM DUAL) src
            ON (target.TABLE_NAME = src.TABLE_NAME)
            WHEN MATCHED THEN
                UPDATE SET 
                    LAST_SYNC_TIME = TO_TIMESTAMP_TZ(:isoTimeString, 'YYYY-MM-DD""T""HH24:MI:SS""Z""'),
                    LAST_SUCCESS_TIME = SYSTIMESTAMP,
                    RECORDS_SYNCED = NVL(RECORDS_SYNCED,0) + :recordCount
            WHEN NOT MATCHED THEN
                INSERT (TABLE_NAME, LAST_SYNC_TIME, LAST_SUCCESS_TIME, RECORDS_SYNCED)
                VALUES (:tableName, TO_TIMESTAMP_TZ(:isoTimeString, 'YYYY-MM-DD""T""HH24:MI:SS""Z""'), SYSTIMESTAMP, :recordCount)";

                File.AppendAllText(logPath, $"5.执行MERGE SQL:{Environment.NewLine}{mergeSql}{Environment.NewLine}");
                File.AppendAllText(logPath, $"6.参数: tableName={tableName}, syncTime={isoTimeString}, recordCount=1{Environment.NewLine}");

                var affectedRows = await oracleConn.ExecuteAsync(mergeSql, new
                {
                    tableName,
                    isoTimeString,
                    recordCount = 1
                });

                File.AppendAllText(logPath, $"7.MERGE操作影响行数: {affectedRows}{Environment.NewLine}");

                // 3. 验证数据是否写入
                var verifySql = "SELECT LAST_SYNC_TIME FROM ZLHIS.SYNC_METADATA WHERE TABLE_NAME = :tableName";
                var lastSync = await oracleConn.QueryFirstOrDefaultAsync<DateTime?>(verifySql, new { tableName });
                File.AppendAllText(logPath, $"8.lastSync: 最后同步时间为 {lastSync.Value}{Environment.NewLine}");
                if (lastSync.HasValue)
                {
                    File.AppendAllText(logPath, $"9.验证成功: 最后同步时间为 {lastSync.Value}{Environment.NewLine}");
                }
                else
                {
                    File.AppendAllText(logPath, $"警告: 验证失败，未找到同步记录{Environment.NewLine}");
                }

                File.AppendAllText(logPath, $"10.同步元数据更新完成{Environment.NewLine}");
            }
            catch (OracleException orex)
            {
                File.AppendAllText(logPath, $"Oracle数据库错误: {orex.Message}{Environment.NewLine}");
                File.AppendAllText(logPath, $"错误代码: {orex.Number}, 错误位置: {orex.StackTrace}{Environment.NewLine}");
                _logger.LogError(orex, $"更新{tableName}的最后同步时间失败(Oracle错误)");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"更新同步元数据时发生错误: {ex}{Environment.NewLine}");
                _logger.LogError(ex, $"更新{tableName}的最后同步时间失败");
            }
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
        /// <summary>
        /// 表结构检查
        /// </summary>
        /// <returns></returns>
        //private async Task ValidateAndSyncTableStructure(CancellationToken cancellationToken = default)
        //{

        //    try
        //    {
        //        var tables = new[] {
        //        "t_test_rec",
        //        "t_testr_res_indicate",
        //        "t_microbe_bacteria_res",
        //        "t_microbe_suscept_res"
        //        };

        //        using var gbaseConn = new NpgsqlConnection(_config.GetConnectionString("OpenGaussConnection"));
        //        using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));

        //        await gbaseConn.OpenAsync();
        //        await oracleConn.OpenAsync();

        //        foreach (var table in tables)
        //        {
        //            // 获取GBase表结构
        //            var gbaseColumns = await gbaseConn.QueryAsync<ColumnInfo>(@"
        //            SELECT column_name, data_type 
        //            FROM information_schema.columns 
        //            WHERE table_name = :tableName", new { tableName = table });

        //            // 检查Oracle表结构并添加缺失字段
        //            foreach (var column in gbaseColumns)
        //            {
        //                var exists = await oracleConn.ExecuteScalarAsync<int>(@"
        //                SELECT COUNT(*) 
        //                FROM all_tab_columns 
        //                WHERE table_name = :tableName 
        //                AND column_name = :columnName
        //                AND owner = 'ZLHIS'", new
        //                {
        //                    tableName = table.ToUpper(),
        //                    columnName = column.column_name.ToUpper()
        //                });

        //                if (exists == 0)
        //                {
        //                    var alterSql = GetAlterTableSql(table, column);
        //                    await oracleConn.ExecuteAsync(alterSql);
        //                    _logger.LogInformation($"已添加字段 {table}.{column.column_name}");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "表结构同步失败");
        //        throw;
        //    }
        //}

        //private string GetAlterTableSql(string tableName, ColumnInfo column)
        //{
        //    var typeMapping = new Dictionary<string, string>
        //    {
        //        {"character varying", "VARCHAR2(255)"},
        //        {"text", "CLOB"},
        //        {"integer", "NUMBER(10)"},
        //        {"timestamp without time zone", "TIMESTAMP"},
        //        {"boolean", "CHAR(1)"}
        //    };

        //    var oracleType = typeMapping.TryGetValue(column.data_type, out var t)
        //        ? t
        //        : "VARCHAR2(255)";

        //    return $"ALTER TABLE ZLHIS.{tableName.ToUpper()} ADD {column.column_name.ToUpper()} {oracleType}";
        //}

        //private class ColumnInfo
        //{
        //    public string column_name { get; set; }
        //    public string data_type { get; set; }
        //}
        public async Task ValidateAndSyncTableStructure(CancellationToken cancellationToken = default)
        {
            // 创建日志目录和文件路径
            var logDir = Path.Combine(_env.ContentRootPath, "Data", "SyncStructureLogs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"structure_sync_{DateTime.Now:yyyyMMddHHmmss}.log");

            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 开始表结构验证和同步{Environment.NewLine}");

                var tables = new[] {
            "t_test_rec",
            "t_testr_res_indicate",
            "t_microbe_bacteria_res",
            "t_microbe_suscept_res",
            "t_check_rec"
        };

                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 需要检查的表: {string.Join(", ", tables)}{Environment.NewLine}");

                using var gbaseConn = new NpgsqlConnection(_config.GetConnectionString("OpenGaussConnection"));
                using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));

                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 正在连接到GBase数据库...{Environment.NewLine}");
                await gbaseConn.OpenAsync(cancellationToken);
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] GBase数据库连接成功{Environment.NewLine}");

                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 正在连接到Oracle数据库...{Environment.NewLine}");
                await oracleConn.OpenAsync(cancellationToken);
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Oracle数据库连接成功{Environment.NewLine}");

                foreach (var table in tables)
                {
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 开始处理表: {table}{Environment.NewLine}");

                    // 获取GBase表结构
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 查询GBase表结构...{Environment.NewLine}");
                    var gbaseColumns = await gbaseConn.QueryAsync<ColumnInfo>(@"
                SELECT column_name, data_type 
                FROM information_schema.columns 
                WHERE table_name = @tableName", new { tableName = table });

                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 获取到GBase表{table}的列信息({gbaseColumns.Count()}列):{Environment.NewLine}");
                    foreach (var col in gbaseColumns)
                    {
                        File.AppendAllText(logPath, $"\t列名: {col.column_name}, 类型: {col.data_type}{Environment.NewLine}");
                    }

                    // 检查Oracle表是否存在
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 检查Oracle表{table}是否存在...{Environment.NewLine}");
                    var oracleTableExists = await oracleConn.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM all_tables WHERE table_name = :tableName AND owner = 'ZLHIS'",
                        new { tableName = table.ToUpper() });

                    if (oracleTableExists == 0)
                    {
                        File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Oracle表{table}不存在，将创建新表{Environment.NewLine}");
                        try
                        {
                            // 创建表（只包含ID和TID两个字段）
                            var createTableSql = $@"
                            CREATE TABLE ZLHIS.{table.ToUpper()} (
                                ""ID"" VARCHAR2(255),
                                ""TID"" VARCHAR2(255)
                            )";

                            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 执行创建表SQL: {createTableSql}{Environment.NewLine}");
                            await oracleConn.ExecuteAsync(createTableSql);

                            // 创建主键索引
                            var createPkSql = $@"CREATE UNIQUE INDEX {table.ToUpper()}_pkey ON ZLHIS.{table.ToUpper()} (""ID"")";
                            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 执行创建主键索引SQL: {createPkSql}{Environment.NewLine}");
                            await oracleConn.ExecuteAsync(createPkSql);

                            // 创建TID索引
                            var createTidIdxSql = $@"CREATE INDEX {table.ToUpper()}_tid_idx ON ZLHIS.{table.ToUpper()} (""TID"")";
                            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 执行创建TID索引SQL: {createTidIdxSql}{Environment.NewLine}");
                            await oracleConn.ExecuteAsync(createTidIdxSql);

                            // 授权
                            var grantSql = $@"GRANT ALL ON ZLHIS.{table.ToUpper()} TO ZLHIS";
                            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 执行授权SQL: {grantSql}{Environment.NewLine}");
                            await oracleConn.ExecuteAsync(grantSql);

                            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 成功创建表{table}并设置索引和权限{Environment.NewLine}");

                            // 创建表后继续检查其他列
                            oracleTableExists = 1;
                        }
                        catch (Exception ex)
                        {
                            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 创建表{table}失败: {ex.Message}{Environment.NewLine}");
                            continue;
                        }
                    }

                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 开始检查Oracle表{table}的列...{Environment.NewLine}");

                    // 检查Oracle表结构并添加缺失字段
                    foreach (var column in gbaseColumns)
                    {
                        File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 检查列 {column.column_name}...{Environment.NewLine}");

                        var exists = await oracleConn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) 
                    FROM all_tab_columns 
                    WHERE table_name = :tableName 
                    AND column_name = :columnName
                    AND owner = 'ZLHIS'", new
                        {
                            tableName = table.ToUpper(),
                            columnName = column.column_name.ToUpper()
                        });

                        if (exists == 0)
                        {
                            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 列 {column.column_name} 不存在，将添加{Environment.NewLine}");

                            var alterSql = GetAlterTableSql(table, column);
                            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 执行SQL: {alterSql}{Environment.NewLine}");

                            try
                            {
                                await oracleConn.ExecuteAsync(alterSql);
                                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 成功添加列 {column.column_name}{Environment.NewLine}");
                            }
                            catch (Exception ex)
                            {
                                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 添加列 {column.column_name} 失败: {ex.Message}{Environment.NewLine}");
                            }
                        }
                        else
                        {
                            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 列 {column.column_name} 已存在，跳过{Environment.NewLine}");
                        }
                    }

                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 表 {table} 处理完成{Environment.NewLine}");
                }

                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 所有表结构验证和同步完成{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 表结构验证和同步过程中发生错误: {ex}{Environment.NewLine}");
                _logger.LogError(ex, "表结构验证和同步失败");
                throw;
            }
        }

        private string GetAlterTableSql(string tableName, ColumnInfo column)
        {
            var typeMapping = new Dictionary<string, string>
    {
        {"character varying", $"VARCHAR2({GetVarcharLength(column)})"},
        {"text", $"VARCHAR2(4000)"},
        {"integer", "NUMBER(10)"},
        {"bigint", "NUMBER(19)"},
        {"numeric", "NUMBER"},
        {"timestamp without time zone", $"VARCHAR2({GetVarcharLength(column)})"},
        {"timestamp with time zone", $"VARCHAR2({GetVarcharLength(column)})"},
        {"date", $"VARCHAR2({GetVarcharLength(column)})"},
        {"boolean", "CHAR(1)"},
        {"bytea", "BLOB"}
    };

            var oracleType = typeMapping.TryGetValue(column.data_type.ToLower(), out var t)
                ? t
                : "VARCHAR2(255)";

            return $"ALTER TABLE ZLHIS.{tableName.ToUpper()} ADD {column.column_name.ToUpper()} {oracleType}";
        }

        private string GetVarcharLength(ColumnInfo column)
        {
            // 这里可以添加逻辑从GBase获取character varying列的实际长度
            // 目前默认返回255
            return "255";
        }

        private class ColumnInfo
        {
            public string column_name { get; set; }
            public string data_type { get; set; }
        }
        public class Test_rec
        {
            public DateTimeOffset INSTOCK_TIME { get; set; }
        }
    }
}
