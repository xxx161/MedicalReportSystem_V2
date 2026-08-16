// FileDataStorageService.cs
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedicalReportSystem.Services
{
    public class FileDataStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileDataStorageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _storagePath;
        private readonly TimeSpan _defaultExpiration;

        public FileDataStorageService(
            IWebHostEnvironment env,
            ILogger<FileDataStorageService> logger,
            IConfiguration configuration)
        {
            _env = env;
            _logger = logger;
            _configuration = configuration;

            _logger.LogInformation("初始化文件存储服务...");

            // 优先使用App_Data目录（IIS通常对此目录有完全控制权限）
            var appDataPath = Path.Combine(_env.ContentRootPath, "App_Data", "TempData");

            // 检查配置是否有自定义路径
            var configuredPath = _configuration.GetValue<string>("FileStorage:BasePath");
            if (!string.IsNullOrEmpty(configuredPath))
            {
                // 如果是相对路径，转换为绝对路径
                if (!Path.IsPathRooted(configuredPath))
                {
                    configuredPath = Path.Combine(_env.ContentRootPath, configuredPath);
                }
                appDataPath = configuredPath;
            }

            _storagePath = appDataPath;

            // 从配置读取默认过期时间
            var expirationHours = _configuration.GetValue<int>("FileStorage:DefaultExpirationHours", 24);
            _defaultExpiration = TimeSpan.FromHours(expirationHours);

            // 创建目录（简化，不设置额外属性）
            EnsureStorageDirectorySimple();

            _logger.LogInformation($"文件存储服务初始化完成，存储路径: {_storagePath}");
        }
        /// <summary>
        /// 简单的目录创建（不设置任何额外属性）
        /// </summary>
        private void EnsureStorageDirectorySimple()
        {
            try
            {
                if (!Directory.Exists(_storagePath))
                {
                    Directory.CreateDirectory(_storagePath);
                    _logger.LogInformation($"创建目录成功: {_storagePath}");
                }

                // 记录目录信息
                var dirInfo = new DirectoryInfo(_storagePath);
                _logger.LogInformation($"目录信息: 路径={dirInfo.FullName}, 存在={dirInfo.Exists}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建目录失败: {_storagePath}");
                throw new InvalidOperationException($"无法创建或访问存储目录: {_storagePath}", ex);
            }
        }
        private void EnsureStorageDirectoryWithPermissions()
        {
            try
            {
                // 检查目录是否存在
                if (Directory.Exists(_storagePath))
                {
                    _logger.LogInformation($"目录已存在: {_storagePath}");

                    // 检查写入权限
                    var testFile = Path.Combine(_storagePath, $"test_{Guid.NewGuid():N}.txt");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    _logger.LogInformation("目录写入权限检查通过");
                }
                else
                {
                    _logger.LogInformation($"创建目录: {_storagePath}");
                    Directory.CreateDirectory(_storagePath);
                    _logger.LogInformation("目录创建成功");
                }

                // 检查目录权限详细信息
                var dirInfo = new DirectoryInfo(_storagePath);
                _logger.LogInformation($"目录完整路径: {dirInfo.FullName}");
                _logger.LogInformation($"目录属性: {dirInfo.Attributes}");
                _logger.LogInformation($"目录创建时间: {dirInfo.CreationTime}");

                // 检查磁盘空间
                var drive = new DriveInfo(Path.GetPathRoot(_storagePath));
                _logger.LogInformation($"磁盘信息 - 名称: {drive.Name}, 总空间: {drive.TotalSize / 1024 / 1024}MB, 可用空间: {drive.AvailableFreeSpace / 1024 / 1024}MB");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, $"目录访问被拒绝: {_storagePath}");
                throw;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"IO错误: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"检查目录权限失败");
                throw;
            }
        }
        /// <summary>
        /// 确保存储目录存在
        /// </summary>
        private void EnsureStorageDirectory()
        {
            try
            {
                if (!Directory.Exists(_storagePath))
                {
                    Directory.CreateDirectory(_storagePath);
                    _logger.LogInformation($"创建存储目录: {_storagePath}");
                }

                // 设置目录权限（可选）
                //var directoryInfo = new DirectoryInfo(_storagePath);
                //directoryInfo.Attributes |= FileAttributes.NotContentIndexed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建存储目录失败: {_storagePath}");
                throw;
            }
        }

        /// <summary>
        /// 存储数据到文件
        /// </summary>
        public async Task<string> SaveDataAsync(string data, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("数据不能为空", nameof(data));

            // 清理数据：移除Base64中的换行符
            string cleanedData = data.Replace("\r", "").Replace("\n", "");

            _logger.LogInformation($"原始数据长度: {data.Length}, 清理后长度: {cleanedData.Length}");

            // 生成唯一的文件ID
            string fileId = Guid.NewGuid().ToString("N");

            // 使用配置的默认过期时间
            var actualExpiration = expiration ?? _defaultExpiration;

            // 构建文件信息对象
            var fileInfo = new FileDataInfo
            {
                Id = fileId,
                Data = cleanedData,  // 使用清理后的数据
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(actualExpiration),
                Size = cleanedData.Length
            };

            // 序列化为JSON
            string jsonContent = JsonSerializer.Serialize(fileInfo, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // 文件路径
            string filePath = GetFilePath(fileId);

            try
            {
                // 写入文件
                await File.WriteAllTextAsync(filePath, jsonContent, Encoding.UTF8);

                _logger.LogInformation($"数据已保存到文件: {filePath}, 过期时间: {fileInfo.ExpiresAt:yyyy-MM-dd HH:mm:ss}");

                // 记录统计信息
                LogStorageStatistics();

                return fileId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"保存数据到文件失败: {filePath}");
                throw;
            }
        }

        public async Task<string> GetDataAsync(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("文件ID不能为空", nameof(fileId));

            string filePath = GetFilePath(fileId);

            _logger.LogInformation($"🔥 GetDataAsync 开始: fileId={fileId}");
            _logger.LogInformation($"🔥 文件路径: {filePath}");
            _logger.LogInformation($"🔥 文件存在: {File.Exists(filePath)}");

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"数据文件不存在: {filePath}");
                return null;
            }

            try
            {
                // 读取文件内容
                string jsonContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                _logger.LogInformation($"🔥 文件内容长度: {jsonContent.Length}");
                _logger.LogInformation($"🔥 文件内容前200字符: {jsonContent.Substring(0, Math.Min(200, jsonContent.Length))}");

                if (string.IsNullOrEmpty(jsonContent))
                {
                    _logger.LogWarning($"数据文件为空: {filePath}");
                    return null;
                }

                // 尝试解析
                FileDataInfo fileInfo = null;
                bool parseSuccess = false;

                try
                {
                    fileInfo = JsonSerializer.Deserialize<FileDataInfo>(jsonContent);
                    parseSuccess = fileInfo != null;
                    _logger.LogInformation($"🔥 JSON解析结果: {parseSuccess}");

                    if (fileInfo != null)
                    {
                        _logger.LogInformation($"🔥 解析成功 - Id: {fileInfo.Id}, Data长度: {fileInfo.Data?.Length ?? 0}");
                        _logger.LogInformation($"🔥 创建时间: {fileInfo.CreatedAt}, 过期时间: {fileInfo.ExpiresAt}");
                        _logger.LogInformation($"🔥 当前UTC时间: {DateTime.UtcNow}");
                        _logger.LogInformation($"🔥 是否过期: {fileInfo.ExpiresAt < DateTime.UtcNow}");
                    }
                }
                catch (JsonException jex)
                {
                    _logger.LogError($"🔥 JSON解析异常: {jex.Message}");
                    parseSuccess = false;
                }

                // 如果解析失败，尝试修复
                if (!parseSuccess)
                {
                    _logger.LogWarning($"🔥 JSON解析失败，尝试修复文件: {fileId}");

                    // 尝试修复JSON（可能是data字段有换行符）
                    try
                    {
                        // 手动修复：移除data字段中的换行符
                        var repairedJson = RepairJsonData(jsonContent);
                        fileInfo = JsonSerializer.Deserialize<FileDataInfo>(repairedJson);

                        if (fileInfo != null)
                        {
                            _logger.LogInformation($"🔥 文件修复成功: {fileId}");
                        }
                        else
                        {
                            _logger.LogError($"🔥 文件修复失败: {fileId}");
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"🔥 修复文件失败: {ex.Message}");
                        return null;
                    }
                }

                if (fileInfo == null)
                {
                    _logger.LogWarning($"🔥 fileInfo为null");
                    return null;
                }

                // 检查是否过期
                if (fileInfo.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogInformation($"🔥 数据已过期: {fileId}, 过期时间: {fileInfo.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
                    // 返回数据，让调用方决定是否使用过期数据
                }

                // 检查数据完整性
                if (string.IsNullOrEmpty(fileInfo.Data))
                {
                    _logger.LogWarning($"🔥 数据文件内容为空: {filePath}");
                    return null;
                }

                _logger.LogInformation($"🔥 从文件读取数据成功: {filePath}");
                return fileInfo.Data;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"🔥 数据文件JSON格式错误: {filePath}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"🔥 读取数据文件失败: {filePath}");
                return null;
            }
        }

        /// <summary>
        /// 修复JSON中的data字段（移除换行符）
        /// </summary>
        private string RepairJsonData(string jsonContent)
        {
            try
            {
                _logger.LogInformation($"🔥 开始修复JSON，原始长度: {jsonContent.Length}");

                // 查找 "data":" 之后的内容
                int dataStart = jsonContent.IndexOf("\"data\":\"", StringComparison.OrdinalIgnoreCase);
                if (dataStart == -1)
                {
                    _logger.LogWarning($"🔥 找不到data字段");
                    return jsonContent;
                }

                dataStart += 7; // "\"data\":\"".Length

                // 查找data字段的结束位置
                int dataEnd = -1;
                int quoteCount = 0;
                bool escaped = false;

                for (int i = dataStart; i < jsonContent.Length; i++)
                {
                    char c = jsonContent[i];

                    if (!escaped && c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (!escaped && c == '"')
                    {
                        quoteCount++;
                        if (quoteCount == 1) // 找到结束引号
                        {
                            dataEnd = i;
                            break;
                        }
                    }

                    escaped = false;
                }

                if (dataEnd == -1)
                {
                    _logger.LogWarning($"🔥 找不到data字段结束位置");
                    return jsonContent;
                }

                // 提取data字段值
                string dataValue = jsonContent.Substring(dataStart, dataEnd - dataStart);
                _logger.LogInformation($"🔥 提取的data字段值，长度: {dataValue.Length}");
                _logger.LogInformation($"🔥 data值前100字符: {dataValue.Substring(0, Math.Min(100, dataValue.Length))}");

                // 清理Base64中的换行符
                string cleanedData = new string(dataValue.Where(c => !char.IsWhiteSpace(c)).ToArray());
                _logger.LogInformation($"🔥 清理后的data长度: {cleanedData.Length}");

                // 只有有变化时才修复
                if (dataValue == cleanedData)
                {
                    _logger.LogInformation($"🔥 data字段无需修复");
                    return jsonContent;
                }

                // 替换原数据
                string repairedJson = jsonContent.Substring(0, dataStart) +
                                     cleanedData +
                                     jsonContent.Substring(dataEnd);

                _logger.LogInformation($"🔥 修复完成，新JSON长度: {repairedJson.Length}");
                return repairedJson;
            }
            catch (Exception ex)
            {
                _logger.LogError($"🔥 修复JSON失败: {ex.Message}");
                return jsonContent;
            }
        }
        /// <summary>
        /// 删除数据文件
        /// </summary>
        public async Task<bool> DeleteDataAsync(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
                return false;

            string filePath = GetFilePath(fileId);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogDebug($"删除数据文件: {filePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除数据文件失败: {filePath}");
                return false;
            }
        }

        /// <summary>
        /// 清理过期文件（修复版 - 忽略删除错误）
        /// </summary>
        public async Task<int> CleanupExpiredFilesAsync()
        {
            int deletedCount = 0;
            int errorCount = 0;
            var now = DateTime.UtcNow;

            try
            {
                if (!Directory.Exists(_storagePath))
                {
                    _logger.LogDebug($"存储目录不存在，无需清理: {_storagePath}");
                    return 0;
                }

                var files = Directory.GetFiles(_storagePath, "*.json");
                _logger.LogInformation($"开始检查 {files.Length} 个数据文件...");

                foreach (var file in files)
                {
                    bool shouldDelete = false;
                    string reason = "";

                    try
                    {
                        // 尝试读取文件信息
                        string jsonContent = await File.ReadAllTextAsync(file, Encoding.UTF8);
                        var fileInfo = JsonSerializer.Deserialize<FileDataInfo>(jsonContent);

                        if (fileInfo == null)
                        {
                            shouldDelete = true;
                            reason = "JSON格式错误";
                        }
                        else if (fileInfo.ExpiresAt < now)
                        {
                            shouldDelete = true;
                            reason = "已过期";
                        }
                    }
                    catch (JsonException)
                    {
                        shouldDelete = true;
                        reason = "JSON解析失败";
                    }
                    catch (IOException ioEx)
                    {
                        // 文件被占用或其他IO错误
                        _logger.LogWarning($"文件被占用，跳过: {file}, 错误: {ioEx.Message}");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"检查文件失败，跳过: {file}, 错误: {ex.Message}");
                        continue;
                    }

                    // 如果需要删除，尝试删除
                    if (shouldDelete)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                            _logger.LogDebug($"清理文件: {Path.GetFileName(file)} ({reason})");
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // 权限不足，忽略并记录警告
                            _logger.LogWarning($"权限不足，无法删除文件: {file}");
                            errorCount++;
                        }
                        catch (IOException ioEx)
                        {
                            // 文件被占用
                            _logger.LogWarning($"文件被占用，无法删除: {file}, 错误: {ioEx.Message}");
                            errorCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"删除文件失败: {file}, 错误: {ex.Message}");
                            errorCount++;
                        }
                    }
                }

                if (deletedCount > 0 || errorCount > 0)
                {
                    _logger.LogInformation($"清理完成，成功删除 {deletedCount} 个文件，失败 {errorCount} 个");
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期文件时出错");
                return 0; // 返回0而不是抛出异常，避免影响主流程
            }
        }

        /// <summary>
        /// 获取存储统计信息
        /// </summary>
        public StorageStatistics GetStorageStatistics()
        {
            try
            {
                if (!Directory.Exists(_storagePath))
                    return new StorageStatistics();

                var files = Directory.GetFiles(_storagePath, "*.json");
                var totalSize = files.Sum(f => new FileInfo(f).Length);
                var now = DateTime.UtcNow;
                int expiredCount = 0;
                int validCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(file, Encoding.UTF8);
                        var fileInfo = JsonSerializer.Deserialize<FileDataInfo>(jsonContent);

                        if (fileInfo?.ExpiresAt < now)
                            expiredCount++;
                        else
                            validCount++;
                    }
                    catch
                    {
                        // 忽略解析错误的文件
                    }
                }

                return new StorageStatistics
                {
                    TotalFiles = files.Length,
                    ValidFiles = validCount,
                    ExpiredFiles = expiredCount,
                    TotalSize = totalSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取存储统计信息失败");
                return new StorageStatistics();
            }
        }

        /// <summary>
        /// 记录存储统计信息
        /// </summary>
        private void LogStorageStatistics()
        {
            try
            {
                var stats = GetStorageStatistics();
                _logger.LogDebug($"存储统计: 总文件数={stats.TotalFiles}, 有效文件={stats.ValidFiles}, 过期文件={stats.ExpiredFiles}, 总大小={stats.TotalSize / 1024}KB");
            }
            catch
            {
                // 忽略统计错误
            }
        }

        /// <summary>
        /// 获取文件完整路径
        /// </summary>
        private string GetFilePath(string fileId)
        {
            // 安全性检查：移除不安全的字符
            string safeFileName = new string(fileId.Where(c =>
                char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());

            if (string.IsNullOrEmpty(safeFileName) || safeFileName.Length < 10)
                throw new ArgumentException("无效的文件ID", nameof(fileId));

            return Path.Combine(_storagePath, $"{safeFileName}.json");
        }

        /// <summary>
        /// 文件数据信息类
        /// </summary>
        private class FileDataInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("data")]
            public string Data { get; set; }

            [JsonPropertyName("createdAt")]
            public DateTime CreatedAt { get; set; }

            [JsonPropertyName("expiresAt")]
            public DateTime ExpiresAt { get; set; }

            [JsonPropertyName("size")]
            public int Size { get; set; }
        }

        /// <summary>
        /// 存储统计信息
        /// </summary>
        public class StorageStatistics
        {
            public int TotalFiles { get; set; }
            public int ValidFiles { get; set; }
            public int ExpiredFiles { get; set; }
            public long TotalSize { get; set; }
        }
    }
}