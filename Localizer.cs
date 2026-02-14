using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;
using System.Globalization;  // 添加 CultureInfo 支持
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace ZombieEscapePractice
{
    public static class Localizer
    {
        private static Dictionary<string, Dictionary<string, string>> _localizations = new();

        static Localizer()
        {
            LoadLocalizations();
        }

        private static void LoadLocalizations()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith("ZombieEscapePractice.lang.") && r.EndsWith(".json"));

            foreach (var resourceName in resourceNames)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();
                string langCode = resourceName.Replace("ZombieEscapePractice.lang.", "").Replace(".json", "");
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    _localizations[langCode] = dict;
                }
            }
        }

        public static string ForPlayer(CCSPlayerController player, string key, params object[] args)
        {
            string lang = "en";
            if (player != null && player.IsValid)
            {
                // 获取玩家的 CultureInfo，然后提取两位字母语言代码
                CultureInfo culture = player.GetLanguage();
                if (culture != null)
                {
                    lang = culture.TwoLetterISOLanguageName; // 例如 "en", "zh"
                }
            }

            // 如果玩家语言不存在本地化数据，回退到英文
            if (!_localizations.ContainsKey(lang))
                lang = "en";

            if (_localizations.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
            {
                return string.Format(value, args);
            }

            // 如果键也不存在，返回键名作为后备
            return key;
        }
    }
}