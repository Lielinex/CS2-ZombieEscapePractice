using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
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
        public override string ModuleVersion => "1.3.0";

        private ConfigManager _configManager = new();
        private bool _isPracticeActive = false;

        public override void Load(bool hotReload)
        {
            _configManager.Load(ModuleDirectory);

            AddCommand("css_practice", "Open challenge practice menu", CommandPractice);
            AddCommand("css_prac", "Open challenge practice menu", CommandPractice);

            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        }

        private void CommandPractice(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;

            if (_isPracticeActive)
            {
                player.PrintToChat(Localizer.ForPlayer(player, "practice.active"));
                return;
            }

            string mapKey = GetCurrentMapKey();
            if (!_configManager.Config.Maps.TryGetValue(mapKey, out var mapConfig) || mapConfig.Challenges.Count == 0)
            {
                player.PrintToChat(Localizer.ForPlayer(player, "practice.no_challenges", mapKey));
                return;
            }

            ShowChallengeMenu(player, mapConfig.Challenges);
        }

        private string GetCurrentMapKey()
        {
            var convar = ConVar.Find("host_workshop_map");
            if (convar != null && !string.IsNullOrEmpty(convar.StringValue))
            {
                return convar.StringValue;
            }
            return Server.MapName;
        }

        private void ShowChallengeMenu(CCSPlayerController player, List<ChallengeConfig> challenges)
        {
            var menu = new ChatMenu(Localizer.ForPlayer(player, "menu.title"));
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
            if (!string.IsNullOrEmpty(challenge.Command))
            {
                Server.ExecuteCommand(challenge.Command);
            }

            if (challenge.Position != null)
            {
                Vector targetPos = new Vector(challenge.Position.X, challenge.Position.Y, challenge.Position.Z);
                var players = Utilities.GetPlayers();
                foreach (var player in players)
                {
                    if (player != null && player.IsValid && player.PlayerPawn.IsValid)
                    {
                        player.PlayerPawn.Value.Teleport(targetPos, player.PlayerPawn.Value.EyeAngles, new Vector(0, 0, 0));
                    }
                }
            }

            _isPracticeActive = true;

            // 为每个玩家发送本地化提示
            var allPlayers = Utilities.GetPlayers();
            foreach (var player in allPlayers)
            {
                if (player != null && player.IsValid)
                {
                    player.PrintToChat(Localizer.ForPlayer(player, "practice.activated", challenge.Name));
                }
            }
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