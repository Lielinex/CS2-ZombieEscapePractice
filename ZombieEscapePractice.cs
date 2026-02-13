using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;
using System.Linq;

namespace ZombieEscapePractice
{
    public class ChallengePlugin : BasePlugin
    {
        public override string ModuleName => "Zombie Escape Practice";
        public override string ModuleDescription => "";
        public override string ModuleVersion => "1.1.0";
        public override string ModuleAuthor => "Lielinex";
        private ConfigManager _configManager = new();
        private bool _isPracticeActive = false;

        public override void Load(bool hotReload)
        {
            _configManager.Load(ModuleDirectory);

            AddCommand("css_practice", "打开挑战练习菜单", CommandPractice);
            AddCommand("css_prac", "打开挑战练习菜单", CommandPractice);

            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        }

        private void CommandPractice(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;

            if (_isPracticeActive)
            {
                player.PrintToChat(" [练习] 当前已有激活的练习，请等待回合结束。");
                return;
            }

            // 获取当前地图标识（优先使用Workshop ID，若无则用地名）
            string mapKey = GetCurrentMapKey();
            if (!_configManager.Config.Maps.TryGetValue(mapKey, out var mapConfig) || mapConfig.Challenges.Count == 0)
            {
                player.PrintToChat($" [练习] 当前地图 ({mapKey}) 没有配置挑战。");
                return;
            }

            ShowChallengeMenu(player, mapConfig.Challenges);
        }

        private string GetCurrentMapKey()
        {
            // 尝试获取Workshop ID
            var convar = ConVar.Find("host_workshop_map");
            if (convar != null && !string.IsNullOrEmpty(convar.StringValue))
            {
                return convar.StringValue;
            }
            // 降级使用地图名
            return Server.MapName;
        }

        private void ShowChallengeMenu(CCSPlayerController player, List<ChallengeConfig> challenges)
        {
            var menu = new ChatMenu("选择挑战节点");
            foreach (var challenge in challenges)
            {
                menu.AddMenuOption(challenge.Name, (p, option) =>
                {
                    ExecuteChallenge(challenge);
                });
            }
            MenuManager.OpenChatMenu(player, menu);
        }

        private void ExecuteChallenge(ChallengeConfig challenge)
        {
            // 执行触发命令
            if (!string.IsNullOrEmpty(challenge.Command))
            {
                Server.ExecuteCommand(challenge.Command);
            }

            // 传送所有玩家到指定位置
            Vector targetPos = new Vector(challenge.Position.X, challenge.Position.Y, challenge.Position.Z);
            var players = Utilities.GetPlayers();
            foreach (var player in players)
            {
                if (player != null && player.IsValid && player.PlayerPawn.IsValid)
                {
                    player.PlayerPawn.Value.Teleport(targetPos, player.PlayerPawn.Value.EyeAngles, new Vector(0, 0, 0));
                }
            }

            // 激活练习状态，禁用菜单
            _isPracticeActive = true;

            // 提示所有玩家
            Server.PrintToChatAll($" [练习] 已激活挑战: {challenge.Name}，练习进行中...");
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _isPracticeActive = false;
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            _isPracticeActive = false;
            return HookResult.Continue;
        }
    }
}