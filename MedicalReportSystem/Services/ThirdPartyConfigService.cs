using System.Text.Json;

namespace MedicalReportSystem.Services
{
    public class ThirdPartyConfigService
    {
        private const string ConfigPath = "Configurations/ThirdPartySettings.json";
        private ThirdPartySettings _settings;

        public ThirdPartyConfigService()
        {
            ReloadConfig();
        }

        public ThirdPartySettings GetSettings() => _settings;

        public void UpdateSettings(ThirdPartySettings newSettings)
        {
            var json = JsonSerializer.Serialize(newSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            ReloadConfig();
        }

        private void ReloadConfig()
        {
            var json = File.ReadAllText(ConfigPath);
            _settings = JsonSerializer.Deserialize<ThirdPartySettings>(json);
        }
    }

    //public class ThirdPartySettings
    //{
    //    public string? BaseUrl { get; set; }
    //    public Dictionary<string, string>? ApiEndpoints { get; set; }
    //}
    public class ThirdPartySettings
    {
        public string? BaseUrl { get; set; }
        public Dictionary<string, string>? ApiEndpoints { get; set; }

        // 新增POST请求专用配置
        public Dictionary<string, object>? PostParameters { get; set; }
    }
}
