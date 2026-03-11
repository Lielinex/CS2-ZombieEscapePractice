using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZombieEscapePractice
{
    public abstract class BlockBase
    {
        [JsonPropertyName("targetname")] public string? Targetname { get; set; }
        [JsonIgnore] public bool Enabled { get; set; } = true;

        public virtual bool StartDisabled { get; set; } = false;
    }

    // 单次执行块（type = "once"）
    public class OnceBlock : BlockBase
    {
        [JsonPropertyName("commands")]
        public List<string> Commands { get; set; } = new();
        [JsonPropertyName("startdisabled")] public bool StartDisabledProperty { get; set; } = false;

        public override bool StartDisabled
        {
            get => StartDisabledProperty;
            set => StartDisabledProperty = value;
        }
    }

    // 重复执行块（type = "repeat"）
    public class RepeatBlock : BlockBase
    {
        [JsonPropertyName("interval")] public float Interval { get; set; }
        [JsonPropertyName("count")] public int? Count { get; set; } = -1; // -1为无限
        [JsonPropertyName("commands")]
        public List<string> Commands { get; set; } = new();
        [JsonPropertyName("startdisabled")] public bool StartDisabledProperty { get; set; } = false;

        public override bool StartDisabled
        {
            get => StartDisabledProperty;
            set => StartDisabledProperty = value;
        }
    }

    // 随机选择块（type = "random"）
    public class RandomBlock : BlockBase
    {
        [JsonPropertyName("mode")] public string Mode { get; set; } = "PickRandom"; //值为PickRandom或PickRandomShuffle
        [JsonPropertyName("commands")]
        public List<string> Commands { get; set; } = new();
        [JsonPropertyName("startdisabled")] public bool StartDisabledProperty { get; set; } = false;
        [JsonIgnore] internal Queue<string>? ShuffleQueue;

        public override bool StartDisabled
        {
            get => StartDisabledProperty;
            set => StartDisabledProperty = value;
        }
    }

    // 延迟块（type = "delay"）：将内部所有命令统一延迟 interval 秒
    public class DelayBlock : BlockBase
    {
        [JsonPropertyName("interval")] public float Interval { get; set; }
        [JsonPropertyName("commands")]
        public List<string> Commands { get; set; } = new();
        [JsonPropertyName("startdisabled")] public bool StartDisabledProperty { get; set; } = false;

        public override bool StartDisabled
        {
            get => StartDisabledProperty;
            set => StartDisabledProperty = value;
        }
    }

    // 自定义转换器，用于反序列化 blocks 数组，根据 type 字段选择具体类型
    public class BlockConverter : JsonConverter<List<BlockBase>>
    {
        public override List<BlockBase> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("blocks 字段必须是一个数组");

            var list = new List<BlockBase>();

            using var document = JsonDocument.ParseValue(ref reader);
            var arrayEnumerator = document.RootElement.EnumerateArray();

            int index = 0;
            foreach (var element in arrayEnumerator)
            {
                Console.WriteLine($"[ZEP ConfigsHelper] 处理元素 {index}: {element.GetRawText()}");

                if (element.ValueKind != JsonValueKind.Object)
                    throw new JsonException($"blocks 数组第 {index} 个元素必须是对象，但遇到了 {element.ValueKind} 类型的值");

                if (!element.TryGetProperty("type", out var typeProp))
                    throw new JsonException($"blocks 数组中第 {index} 个元素缺少 type 字段");

                string type = typeProp.GetString()?.ToLower() ?? "";
                BlockBase? block = type switch
                {
                    "once" => JsonSerializer.Deserialize<OnceBlock>(element.GetRawText(), options),
                    "repeat" => JsonSerializer.Deserialize<RepeatBlock>(element.GetRawText(), options),
                    "random" => JsonSerializer.Deserialize<RandomBlock>(element.GetRawText(), options),
                    "delay" => JsonSerializer.Deserialize<DelayBlock>(element.GetRawText(), options),
                    _ => throw new JsonException($"未知块类型 '{type}' 在第 {index} 个元素")
                };

                if (block != null)
                {
                    block.Enabled = !block.StartDisabled;
                    list.Add(block);
                    Console.WriteLine($"[ZEP ConfigsHelper] 成功解析块类型: {type}, 名称: {block.Targetname ?? "unnamed"}, 初始状态: {(block.Enabled ? "启用" : "禁用")}");
                }

                index++;
            }

            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<BlockBase> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var block in value)
            {
                JsonSerializer.Serialize(writer, block, block.GetType(), options);
            }
            writer.WriteEndArray();
        }
    }
}