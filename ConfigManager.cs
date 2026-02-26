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

    public class ConfigManager
    {
        private string _configsFolderPath;
        public Dictionary<string, List<ChallengeConfig>> Config { get; private set; } = new();

        public void Load(string moduleDirectory)
        {
            // 构建配置文件夹路径: csgo/addons/counterstrikesharp/configs/plugins/ZombieEscapePractice
            var basePath = Path.GetFullPath(Path.Combine(moduleDirectory, "..", "..", "configs", "plugins", "ZombieEscapePractice"));
            _configsFolderPath = basePath;

            if (!Directory.Exists(_configsFolderPath))
                Directory.CreateDirectory(_configsFolderPath);

            var files = Directory.GetFiles(_configsFolderPath, "*.json");
            if (files.Length == 0)
            {
                CreateDefaultExample();
                files = Directory.GetFiles(_configsFolderPath, "*.json");
            }

            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    // 反序列化为字典，键为地图标识符，值为挑战列表
                    var fileConfig = JsonSerializer.Deserialize<Dictionary<string, List<ChallengeConfig>>>(json);
                    if (fileConfig != null)
                    {
                        foreach (var kv in fileConfig)
                        {
                            // 如果键已存在，覆盖
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
                Name = "跳刀练习示例",
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
                Name = "弹幕练习示例（延迟10秒）",
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
                Name = "多块示例",
                Blocks = new List<BlockBase>
                {
                    new OnceBlock { Commands = new List<string> { "say '第一行命令'" } },
                    new DelayBlock
                    {
                        Interval = 4,
                        Commands = new List<string> { "say '延迟4秒后的命令'" }
                    },
                    new RepeatBlock
                    {
                        Interval = 2,
                        Count = 5,
                        Commands = new List<string> { "c_entfire lvl2_final_dynamic SetAnimationNotLooping atk2" }
                    },
                    new RandomBlock
                    {
                        Mode = "PickRandomShuffle",
                        Commands = new List<string> { "say '技能1'", "say '技能2'", "say '技能3'" }
                    }
                }
            }
        }
            };
            string json = JsonSerializer.Serialize(example, new JsonSerializerOptions { WriteIndented = true });
            string examplePath = Path.Combine(_configsFolderPath, "example.json");
            File.WriteAllText(examplePath, json);
        }
    }
}