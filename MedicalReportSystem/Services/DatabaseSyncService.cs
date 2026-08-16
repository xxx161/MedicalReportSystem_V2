using Dapper;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace MedicalReportSystem.Services
{
    public class DatabaseSyncService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DatabaseSyncService> _logger;

        public DatabaseSyncService(IConfiguration config, ILogger<DatabaseSyncService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// 从GBase同步表结构到Oracle（包含注释）
        /// </summary>
        public async Task SyncTableStructureWithComments(string[] tableNames, CancellationToken cancellationToken = default)
        {

            try
            {
                using var gbaseConn = new NpgsqlConnection(_config.GetConnectionString("OpenGaussConnection"));
                using var oracleConn = new OracleConnection(_config.GetConnectionString("OracleConnection"));

                await gbaseConn.OpenAsync(cancellationToken);
                await oracleConn.OpenAsync(cancellationToken);

                foreach (var tableName in tableNames)
                {
                    try
                    {
                        // 1. 获取GBase表结构信息
                        var tableInfo = await GetGBaseTableInfo(gbaseConn, tableName);

                        // 2. 检查Oracle表是否存在
                        bool tableExists = await CheckOracleTableExists(oracleConn, tableName);

                        if (!tableExists)
                        {
                            // 3. 创建新表
                            await CreateOracleTable(oracleConn, tableInfo);
                            _logger.LogInformation($"已创建表 {tableName}");
                        }
                        else
                        {
                            // 4. 添加缺失字段
                            await AddMissingColumns(oracleConn, tableInfo);
                        }

                        // 5. 同步表注释和列注释
                        await SyncComments(oracleConn, tableInfo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"同步表 {tableName} 结构失败");
                        //throw;
                    }
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "数据库同步服务初始化失败，但不影响站点启动");
            }
            
        }

        /// <summary>
        /// 获取GBase表完整信息
        /// </summary>
        private async Task<TableInfo> GetGBaseTableInfo(NpgsqlConnection connection, string tableName)
        {
            // 获取表基本信息
            var tableInfo = await connection.QueryFirstOrDefaultAsync<TableInfo>(@"
            SELECT 
                table_name as TableName,
                table_schema as SchemaName,
                obj_description((table_schema || '.' || table_name)::regclass) as TableComment
            FROM information_schema.tables
            WHERE table_name = @tableName", new { tableName });

            if (tableInfo == null)
                throw new Exception($"表 {tableName} 在GBase中不存在");

            // 获取列信息
            tableInfo.Columns = (await connection.QueryAsync<ColumnInfo>(@"
            SELECT 
                column_name,
                data_type,
                is_nullable,
                column_default,
                character_maximum_length,
                numeric_precision,
                numeric_scale,
                col_description((table_schema || '.' || table_name)::regclass, ordinal_position) as column_comment
            FROM information_schema.columns
            WHERE table_name = @tableName
            ORDER BY ordinal_position", new { tableName })).ToList();

            return tableInfo;
        }

        /// <summary>
        /// 检查Oracle表是否存在
        /// </summary>
        private async Task<bool> CheckOracleTableExists(OracleConnection connection, string tableName)
        {
            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM all_tables WHERE table_name = :tableName AND owner = 'ZLHIS'",
                new { tableName = tableName.ToUpper() });

            return exists > 0;
        }

        /// <summary>
        /// 在Oracle中创建新表
        /// </summary>
        private async Task CreateOracleTable(OracleConnection connection, TableInfo tableInfo)
        {
            var columnsSql = string.Join(",\n", tableInfo.Columns.Select(c =>
                $"{c.column_name.ToUpper()} {MapDataType(c)}" +
                (c.column_default != null ? $" DEFAULT {ConvertDefaultValue(c.column_default)}" : "") +
                (c.is_nullable == "NO" ? " NOT NULL" : "")));

            var createSql = $"CREATE TABLE ZLHIS.{tableInfo.TableName.ToUpper()} (\n{columnsSql}\n)";

            await connection.ExecuteAsync(createSql);
        }

        /// <summary>
        /// 添加缺失的列
        /// </summary>
        private async Task AddMissingColumns(OracleConnection connection, TableInfo tableInfo)
        {
            foreach (var column in tableInfo.Columns)
            {
                var exists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM all_tab_columns WHERE table_name = :tableName AND column_name = :columnName AND owner = 'ZLHIS'",
                    new
                    {
                        tableName = tableInfo.TableName.ToUpper(),
                        columnName = column.column_name.ToUpper()
                    });

                if (exists == 0)
                {
                    var alterSql = $"ALTER TABLE ZLHIS.{tableInfo.TableName.ToUpper()} ADD {column.column_name.ToUpper()} {MapDataType(column)}" +
                        (column.column_default != null ? $" DEFAULT {ConvertDefaultValue(column.column_default)}" : "") +
                        (column.is_nullable == "NO" ? " NOT NULL" : "");

                    await connection.ExecuteAsync(alterSql);
                    _logger.LogInformation($"已添加字段 {tableInfo.TableName}.{column.column_name}");
                }
            }
        }

        /// <summary>
        /// 同步表注释和列注释
        /// </summary>
        private async Task SyncComments(OracleConnection connection, TableInfo tableInfo)
        {
            // 同步表注释
            if (!string.IsNullOrEmpty(tableInfo.TableComment))
            {
                await connection.ExecuteAsync(
                    "COMMENT ON TABLE ZLHIS.:tableName IS :comment",
                    new
                    {
                        tableName = tableInfo.TableName.ToUpper(),
                        comment = tableInfo.TableComment
                    });
            }

            // 同步列注释
            foreach (var column in tableInfo.Columns.Where(c => !string.IsNullOrEmpty(c.column_comment)))
            {
                await connection.ExecuteAsync(
                    "COMMENT ON COLUMN ZLHIS.:tableName.:columnName IS :comment",
                    new
                    {
                        tableName = tableInfo.TableName.ToUpper(),
                        columnName = column.column_name.ToUpper(),
                        comment = column.column_comment
                    });
            }
        }

        /// <summary>
        /// 数据类型映射
        /// </summary>
        private string MapDataType(ColumnInfo column)
        {
            var typeMapping = new Dictionary<string, Func<ColumnInfo, string>>
        {
            {"character varying", c => $"VARCHAR2({c.character_maximum_length ?? 255})"},
            {"text", _ => "CLOB"},
            {"integer", _ => "NUMBER(10)"},
            {"bigint", _ => "NUMBER(19)"},
            {"numeric", c => $"NUMBER({c.numeric_precision}, {c.numeric_scale})"},
            {"timestamp without time zone", _ => "TIMESTAMP"},
            {"timestamp with time zone", _ => "TIMESTAMP WITH TIME ZONE"},
            {"boolean", _ => "CHAR(1)"},
            {"date", _ => "DATE"}
        };

            return typeMapping.TryGetValue(column.data_type, out var mapper)
                ? mapper(column)
                : $"VARCHAR2(255)";
        }

        /// <summary>
        /// 转换默认值
        /// </summary>
        private string ConvertDefaultValue(string defaultValue)
        {
            if (defaultValue == null) return null;

            // 处理PostgreSQL的特殊默认值格式
            if (defaultValue.StartsWith("nextval(")) return null; // 序列忽略
            if (defaultValue.StartsWith("'") && defaultValue.EndsWith("'"))
                return defaultValue; // 字符串直接返回

            // 处理布尔值
            if (defaultValue == "true") return "'Y'";
            if (defaultValue == "false") return "'N'";

            return defaultValue;
        }
    }
    /// <summary>
    /// 表信息
    /// </summary>
    public class TableInfo
    {
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public string TableComment { get; set; }
        public List<ColumnInfo> Columns { get; set; }
    }

    /// <summary>
    /// 列信息
    /// </summary>
    public class ColumnInfo
    {
        public string column_name { get; set; }
        public string data_type { get; set; }
        public string is_nullable { get; set; }
        public string column_default { get; set; }
        public int? character_maximum_length { get; set; }
        public int? numeric_precision { get; set; }
        public int? numeric_scale { get; set; }
        public string column_comment { get; set; }
    }
}
