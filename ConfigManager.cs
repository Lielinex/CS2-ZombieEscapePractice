using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZombieEscapePractice
{
    public class PositionConfig
    {
        [JsonPropertyName("x")] public float X { get; set; }
        [JsonPropertyName("y")] public float Y { get; set; }
        [JsonPropertyName("z")] public float Z { get; set; }
    }

    public class ChallengeConfig
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("position")] public PositionConfig? Position { get; set; } // 可空
        [JsonPropertyName("command")] public string Command { get; set; } = "";
    }

    public class MapConfig
    {
        [JsonPropertyName("challenges")] public List<ChallengeConfig> Challenges { get; set; } = new();
    }

    public class PluginConfig
    {
        [JsonPropertyName("maps")] public Dictionary<string, MapConfig> Maps { get; set; } = new();
    }

    public class ConfigManager
    {
        private string _configPath;
        public PluginConfig Config { get; private set; } = new();

        public void Load(string moduleDirectory)
        {
            _configPath = Path.Combine(moduleDirectory, "challenges.json");
            if (!File.Exists(_configPath))
            {
                CreateDefaultConfig();
                return;
            }

            try
            {
                string json = File.ReadAllText(_configPath);
                Config = JsonSerializer.Deserialize<PluginConfig>(json) ?? new PluginConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZombieEscapePractice] 配置文件加载失败: {ex.Message}，使用默认配置。");
                CreateDefaultConfig();
            }
        }

        private void CreateDefaultConfig()
        {
            Config = new PluginConfig();
            // 示例1：同时有传送和命令
            Config.Maps["123456789"] = new MapConfig
            {
                Challenges = new List<ChallengeConfig>
        {
            new ChallengeConfig
            {
                Name = "跳刀练习 - 第一阶段",
                Position = new PositionConfig { X = -1234.5f, Y = 567.8f, Z = 900.1f },
                Command = "ent_fire knife_spawner addoutput origin -1234.5 567.8 900.1"
            }
        }
            };
            // 示例2：只有传送（无命令）
            Config.Maps["987654321"] = new MapConfig
            {
                Challenges = new List<ChallengeConfig>
        {
            new ChallengeConfig
            {
                Name = "跑路练习 - 传送点",
                Position = new PositionConfig { X = 2000f, Y = 3000f, Z = 512f },
                Command = "" // 明确留空，不执行命令
            }
        }
            };
            // 示例3：只有命令（无传送）
            Config.Maps["ze_no_teleport"] = new MapConfig
            {
                Challenges = new List<ChallengeConfig>
        {
            new ChallengeConfig
            {
                Name = "弹幕启动 - 仅命令",
                Position = null, // 不传送
                Command = "ent_fire barrage_controller activate"
            }
        }
            };
            Save();
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Config, options);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZombieEscapePractice] 配置文件保存失败: {ex.Message}");
            }
        }
    }
}