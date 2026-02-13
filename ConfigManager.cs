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
        [JsonPropertyName("position")] public PositionConfig Position { get; set; } = new();
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
            // 添加示例数据
            Config.Maps["123456789"] = new MapConfig
            {
                Challenges = new List<ChallengeConfig>
                {
                    new ChallengeConfig
                    {
                        Name = "跳刀练习 - 第一阶段",
                        Position = new PositionConfig { X = -1234.5f, Y = 567.8f, Z = 900.1f },
                        Command = "ent_fire knife_spawner addoutput origin -1234.5 567.8 900.1"
                    },
                    new ChallengeConfig
                    {
                        Name = "弹幕练习 - 第二阶段",
                        Position = new PositionConfig { X = 1000.0f, Y = 2000.0f, Z = 300.0f },
                        Command = "ent_fire barrage_controller activate"
                    }
                }
            };
            // 添加一个以地图名为键的示例
            Config.Maps["ze_example_map"] = new MapConfig
            {
                Challenges = new List<ChallengeConfig>
                {
                    new ChallengeConfig
                    {
                        Name = "BOSS战练习",
                        Position = new PositionConfig { X = 500.0f, Y = -500.0f, Z = 128.0f },
                        Command = "ent_fire boss_relay trigger"
                    }
                }
            };
            Save();
            Console.WriteLine("[ZombieEscapePractice] 已生成默认配置文件: " + _configPath);
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