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

    public class AngleConfig
    {
        [JsonPropertyName("pitch")] public float Pitch { get; set; }
        [JsonPropertyName("yaw")] public float Yaw { get; set; }
        [JsonPropertyName("roll")] public float Roll { get; set; }
    }

    public class ChallengeConfig
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("position")] public PositionConfig? Position { get; set; }
        [JsonPropertyName("angle")] public AngleConfig? Angle { get; set; }

        [JsonPropertyName("blocks")]
        [JsonConverter(typeof(BlockConverter))]
        public List<BlockBase> Blocks { get; set; } = new();
    }

    public class ConfigManager()
    {
        private string _mapsFolderPath;
        public Dictionary<string, List<ChallengeConfig>> Config { get; private set; } = new();

        public void Load(string moduleDirectory)
        {
            // 构建地图配置文件夹路径: configs/plugins/ZombieEscapePractice/maps
            var basePath = Path.GetFullPath(Path.Combine(moduleDirectory, "..", "..", "configs", "plugins", "ZombieEscapePractice", "maps"));
            _mapsFolderPath = basePath;

            if (!Directory.Exists(_mapsFolderPath))
                Directory.CreateDirectory(_mapsFolderPath);

            var files = Directory.GetFiles(_mapsFolderPath, "*.json");
            if (files.Length == 0)
            {
                CreateDefaultExample();
                files = Directory.GetFiles(_mapsFolderPath, "*.json");
            }

            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var fileConfig = JsonSerializer.Deserialize<Dictionary<string, List<ChallengeConfig>>>(json);
                    if (fileConfig != null)
                    {
                        foreach (var kv in fileConfig)
                        {
                            Config[kv.Key] = kv.Value;
                            Console.WriteLine($"[ZombieEscapePractice] 已加载地图配置: {kv.Key} 来自文件 {Path.GetFileName(file)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ZombieEscapePractice] 加载配置文件 {file} 失败: {ex.Message}");
                }
            }
        }

        private void CreateDefaultExample()
        {
            var example = new Dictionary<string, List<ChallengeConfig>>
            {
                ["example_map"] = new List<ChallengeConfig>
                {
                    new ChallengeConfig
                    {
                        Name = "example 1",
                        Position = new PositionConfig { X = -1234.5f, Y = 567.8f, Z = 900.1f },
                        Blocks = new List<BlockBase>
                        {
                            new OnceBlock
                            {
                                Commands = new List<string> { "ent_fire knife_spawner enable" }
                            }
                        }
                    },
                    new ChallengeConfig
                    {
                        Name = "example 2（delay 10 sec）",
                        Position = null,
                        Blocks = new List<BlockBase>
                        {
                            new OnceBlock
                            {
                                Commands = new List<string> { "[delay=10]ent_fire barrage_controller activate" }
                            }
                        }
                    },
                    new ChallengeConfig
                    {
                        Name = "multi blocks example",
                        Blocks = new List<BlockBase>
                        {
                            new OnceBlock { Commands = new List<string> { "say command once" } },
                            new DelayBlock
                            {
                                Interval = 4,
                                Commands = new List<string> { "say command delay 4 sec" }
                            },
                            new RepeatBlock
                            {
                                Interval = 2,
                                Count = 5,
                                Commands = new List<string> { "say command repeat 5 times" }
                            },
                            new RandomBlock
                            {
                                Mode = "PickRandomShuffle",
                                Commands = new List<string> { "say 'skil1'", "say 'skill2'", "say 'skill3'" }
                            }
                        }
                    }
                }
            };
            string json = JsonSerializer.Serialize(example, new JsonSerializerOptions { WriteIndented = true });
            string examplePath = Path.Combine(_mapsFolderPath, "example.json");
            File.WriteAllText(examplePath, json);
        }
    }
}