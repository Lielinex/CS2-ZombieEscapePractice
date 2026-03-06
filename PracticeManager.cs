using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieEscapePractice
{
    public class PracticeManager
    {
        private readonly BasePlugin _plugin;
        private readonly ConfigManager _configManager;
        private bool _isPracticeActive = false;

        public PracticeManager(BasePlugin plugin, ConfigManager configManager)
        {
            _plugin = plugin;
            _configManager = configManager;
        }

        public void OnRoundStart() => _isPracticeActive = false;
        public void OnRoundEnd() => _isPracticeActive = false;

        public void CommandPractice(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;

            if (_isPracticeActive)
            {
                player.PrintToChat(" [训练] 当前已有激活的训练，请等待回合结束。");
                return;
            }

            string mapKey = GetCurrentMapKey();
            if (!_configManager.Config.TryGetValue(mapKey, out var challenges) || challenges.Count == 0)
            {
                player.PrintToChat($" [训练] 当前地图 ({mapKey}) 没有配置可用的训练。");
                return;
            }

            ShowChallengeMenu(player, challenges);
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
            var menu = new ChatMenu("选择训练节点");
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
            ExecuteBlocks(challenge.Blocks, 0f);

            if (challenge.Position != null)
            {
                Vector targetPos = new Vector(challenge.Position.X, challenge.Position.Y, challenge.Position.Z);
                QAngle? targetAngles = null;
                if (challenge.Angle != null)
                {
                    targetAngles = new QAngle(challenge.Angle.Pitch, challenge.Angle.Yaw, challenge.Angle.Roll);
                }

                var players = Utilities.GetPlayers();
                foreach (var p in players)
                {
                    if (p != null && p.IsValid && p.PlayerPawn.IsValid)
                    {
                        QAngle? useAngles = targetAngles ?? p.PlayerPawn.Value.EyeAngles;
                        p.PlayerPawn.Value.Teleport(targetPos, useAngles, new Vector(0, 0, 0));
                    }
                }
            }

            _isPracticeActive = true;

            var allPlayers = Utilities.GetPlayers();
            foreach (var player in allPlayers)
            {
                if (player != null && player.IsValid)
                {
                    player.PrintToChat($" [训练] 已激活训练: {challenge.Name}，正在进行中...");
                }
            }
        }

        private (float delay, string command) ParseCommandString(string cmd)
        {
            string trimmed = cmd.Trim();
            if (trimmed.StartsWith("[delay="))
            {
                int endIdx = trimmed.IndexOf(']');
                if (endIdx > 0)
                {
                    string delayStr = trimmed.Substring(7, endIdx - 7);
                    if (float.TryParse(delayStr, out float delay))
                        return (delay, trimmed.Substring(endIdx + 1).Trim());
                }
            }
            return (0f, trimmed);
        }

        private void ExecuteBlocks(List<BlockBase> blocks, float baseDelay)
        {
            foreach (var block in blocks)
            {
                if (!block.Enabled) continue;

                if (block is OnceBlock once)
                {
                    foreach (var cmd in once.Commands)
                    {
                        var (delay, actualCmd) = ParseCommandString(cmd);
                        float totalDelay = baseDelay + delay;
                        if (totalDelay > 0)
                        {
                            var timer = _plugin.AddTimer(totalDelay, () => Server.ExecuteCommand(actualCmd), TimerFlags.STOP_ON_MAPCHANGE);
                            TimerManager.AddTimer(timer);
                        }
                        else
                        {
                            Server.ExecuteCommand(actualCmd);
                        }
                    }
                }
                else if (block is DelayBlock delayBlock)
                {
                    ExecuteCommands(delayBlock.Commands, baseDelay + delayBlock.Interval);
                }
            }
        }

        private void ExecuteCommands(List<string> commands, float baseDelay)
        {
            foreach (var cmd in commands)
            {
                var (delay, actualCmd) = ParseCommandString(cmd);
                float totalDelay = baseDelay + delay;
                if (totalDelay > 0)
                {
                    var timer = _plugin.AddTimer(totalDelay, () => Server.ExecuteCommand(actualCmd), TimerFlags.STOP_ON_MAPCHANGE);
                    TimerManager.AddTimer(timer);
                }
                else
                {
                    Server.ExecuteCommand(actualCmd);
                }
            }
        }
    }
}