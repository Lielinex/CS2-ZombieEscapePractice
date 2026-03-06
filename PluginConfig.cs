using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZombieEscapePractice
{
    public class PluginConfig
    {
        [JsonPropertyName("EnablePractice")]
        public bool EnablePractice { get; set; } = true;

        [JsonPropertyName("EnableVote")]
        public bool EnableVote { get; set; } = true;

        [JsonPropertyName("EnableDebug")]
        public bool EnableDebug { get; set; } = false;

        public static PluginConfig Load(string configPath)
        {
            string? directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(configPath))
            {
                var defaultConfig = new PluginConfig();
                string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                return defaultConfig;
            }

            string jsonContent = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<PluginConfig>(jsonContent) ?? new PluginConfig();
        }
    }
}