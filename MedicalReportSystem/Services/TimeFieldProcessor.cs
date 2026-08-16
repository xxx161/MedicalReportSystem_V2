using Dapper;
using Npgsql;

namespace MedicalReportSystem.Services
{
    public class TimeFieldProcessor
    {
        private readonly ILogger _logger;
        private readonly string _logPath;
        private readonly IConfiguration _config;

        public TimeFieldProcessor(ILogger logger, string logPath, IConfiguration config)
        {
            _logger = logger;
            _logPath = logPath;
            _config = config;
        }

        // 识别时间类型字段
        private bool IsTimeType(object value)
        {
            if (value == null) return false;

            return value is DateTime ||
                   value is DateTimeOffset ||
                   (value is string str && DateTime.TryParse(str, out _));
        }

        // 标准化时间值
        private DateTimeOffset? NormalizeTime(object timeValue)
        {
            try
            {
                return timeValue switch
                {
                    DateTimeOffset dto => dto,
                    DateTime dt when dt.Kind == DateTimeKind.Unspecified =>
                        new DateTimeOffset(dt, TimeSpan.Zero), // 假设为UTC
                    DateTime dt => new DateTimeOffset(dt),
                    string str when DateTimeOffset.TryParse(str, out var parsed) => parsed,
                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"时间值标准化失败: {timeValue} - {ex.Message}");
                return null;
            }
        }

        // 处理单条记录的时间字段
        public void ProcessRecord(dynamic record, List<string> specifiedTimeFields = null)
        {
            var dict = (IDictionary<string, object>)record;
            var timeFields = specifiedTimeFields ?? new List<string>();

            // 如果没有指定时间字段，自动识别
            if (timeFields.Count == 0)
            {
                timeFields = dict.Keys
                    .Where(k => IsTimeType(dict[k]))
                    .ToList();
            }

            foreach (var field in timeFields)
            {
                if (!dict.ContainsKey(field)) continue;

                try
                {
                    var originalValue = dict[field];
                    File.AppendAllText(_logPath,
                        $"[{DateTime.Now}] 处理字段[{field}] 原始值: {originalValue} " +
                        $"(类型: {originalValue?.GetType().Name})\n");

                    var normalizedTime = NormalizeTime(originalValue);
                    if (normalizedTime.HasValue)
                    {
                        dict[field] = normalizedTime.Value;
                        File.AppendAllText(_logPath,
                            $"[{DateTime.Now}] 标准化结果: {normalizedTime.Value.ToString("o")}\n");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"处理时间字段[{field}]失败: {ex.Message}");
                }
            }
        }

        // 获取表的时间类型字段
        public async Task<List<string>> GetTableTimeFields(string tableName)
        {
            try
            {
                using var conn = new NpgsqlConnection(_config.GetConnectionString("OpenGaussConnection"));

                var sql = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = @tableName 
                AND data_type IN (
                    'timestamp without time zone', 
                    'timestamp with time zone', 
                    'date', 'time'
                ) and id='b9a3dc05f8304c88bf39bac146c93ed1'";

                return (await conn.QueryAsync<string>(sql, new { tableName })).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取表[{tableName}]时间字段失败: {ex.Message}");
                return new List<string>();
            }
        }
    }
}
