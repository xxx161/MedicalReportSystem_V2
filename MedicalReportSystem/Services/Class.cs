// Services/GBaseService.cs
using java.sql;
using java.lang;
using System;
using Exception = java.lang.Exception;

public class GBaseService : IDisposable
{
    private Connection? _connection;

    public void Connect(string url, string user, string password)
    {
        try
        {
            // 加载驱动
            Class.forName("com.gbase8c.Driver");

            // 建立连接
            _connection = DriverManager.getConnection(url, user, password);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to connect to GBase: {ex.Message}");
        }
    }

    public string Query(string sql)
    {
        try
        {
            using (Statement stmt = _connection.createStatement())
            using (ResultSet rs = stmt.executeQuery(sql))
            {
                ResultSetMetaData meta = rs.getMetaData();
                int colCount = meta.getColumnCount();
                var result = new System.Text.StringBuilder();

                // 添加表头
                for (int i = 1; i <= colCount; i++)
                {
                    result.Append(meta.getColumnName(i)).Append("\t");
                }
                result.AppendLine();

                // 添加数据行
                while (rs.next())
                {
                    for (int i = 1; i <= colCount; i++)
                    {
                        result.Append(rs.getString(i)).Append("\t");
                    }
                    result.AppendLine();
                }

                return result.ToString();
            }
        }
        catch (SQLException ex)
        {
            throw new Exception($"Query failed: {ex.getMessage()}");
        }
    }

    public int ExecuteUpdate(string sql)
    {
        try
        {
            using (Statement stmt = _connection.createStatement())
            {
                return stmt.executeUpdate(sql);
            }
        }
        catch (SQLException ex)
        {
            throw new Exception($"Execute failed: {ex.getMessage()}");
        }
    }

    public void Dispose()
    {
        if (_connection != null)
        {
            _connection.close();
        }
    }
}