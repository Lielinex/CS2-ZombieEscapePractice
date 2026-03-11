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

        [JsonPropertyName("VoteCustom")]
        public bool VoteCustom { get; set; } = false;

        [JsonPropertyName("VoteRatio")]
        public float VoteRatio { get; set; } = 0.5f;

        [JsonPropertyName("VoreDuration")]
        public float VoteDuration { get; set; } = 30.0f;

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