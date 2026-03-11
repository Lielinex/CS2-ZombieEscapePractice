using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using PanoramaVote;

namespace ZombieEscapePractice
{
    public class VoteManager
    {
        private readonly BasePlugin _plugin;
        private readonly PluginConfig _pluginConfig;
        private CPanoramaVote? _voteHandler;

        public string Prefix => $" {ChatColors.Gold}[{ChatColors.Green}ZEP{ChatColors.Gold}]";

        public bool AdminCheck(CCSPlayerController? player)
        {
            if (player == null) return true; // server console
            if (!player.IsValid) return false;

            if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                player.PrintToChat($"{Prefix} {ChatColors.Red}You don't have permission to use this command.");
                return false;
            }
            return true;
        }

        public VoteManager(BasePlugin plugin, PluginConfig pluginConfig)
        {
            _plugin = plugin;
            _pluginConfig = pluginConfig;
            _voteHandler = new CPanoramaVote(plugin);
            _plugin.RegisterEventHandler<EventVoteCast>((@event, info) =>
            {
                _voteHandler?.VoteCast(@event);
                return HookResult.Continue;
            });
        }

        public void CommandVoteFor(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount < 2)
            {
                if (player == null)
                    Console.WriteLine(" [ZEP] 用法: css_vote_for <内容>");
                else
                    player.PrintToChat($"{Prefix} {ChatColors.Red} 用法: !vote_for <内容>");
                return;
            }

            if (_voteHandler == null)
            {
                if (player == null)
                    Console.WriteLine(" [ZEP] 投票系统未初始化，请联系管理员。");
                else
                    player.PrintToChat($"{Prefix} {ChatColors.Red} 投票系统未初始化，请联系管理员。");
                return;
            }

            string content = command.ArgByIndex(1);
            int caller = player?.Slot ?? VoteConstants.VOTE_CALLER_SERVER;

            _voteHandler.Init();

            // 根据配置选择标题
            string title = _pluginConfig.VoteCustom ? "#SFUI_vote_panorama_vote_default" : "投票";

            bool success = _voteHandler.SendYesNoVoteToAll(
                flDuration: _pluginConfig.VoteDuration,
                iCaller: caller,
                sVoteTitle: title,
                sDetailStr: content,
                resultCallback: (info) => VoteForResultCallback(info),
                handler: VoteHandlerCallback
            );

            if (!success)
            {
                if (player == null)
                    Console.WriteLine(" [ZEP] 发起投票失败，可能已有投票在进行中或服务器未正确配置。");
                else
                    player.PrintToChat($"{Prefix} {ChatColors.Red} 发起投票失败，可能已有投票在进行中或服务器未正确配置。");
            }
        }

        public void CommandVoteExecute(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount < 2)
            {
                if (player == null)
                    Console.WriteLine(" [ZEP] 用法: css_vote_execute \"命令\" [说明]");
                else
                    player.PrintToChat($"{Prefix} {ChatColors.Red} 用法: !vote_execute \"命令\" [说明]");
                return;
            }

            if (_voteHandler == null)
            {
                if (player == null)
                    Console.WriteLine(" [ZEP] 投票系统未初始化，请联系管理员。");
                else
                    player.PrintToChat($"{Prefix} {ChatColors.Red} 投票系统未初始化，请联系管理员。");
                return;
            }

            string fullArgs = command.GetCommandString.Substring(command.GetCommandString.IndexOf(' ') + 1);
            string cmd = "";
            string description = "";

            if (fullArgs.Length > 0 && (fullArgs[0] == '\'' || fullArgs[0] == '\"'))
            {
                char quote = fullArgs[0];
                int endQuote = fullArgs.IndexOf(quote, 1);
                if (endQuote > 0)
                {
                    cmd = fullArgs.Substring(1, endQuote - 1);
                    string rest = fullArgs.Substring(endQuote + 1).Trim();
                    description = string.IsNullOrEmpty(rest) ? $"执行命令: {cmd}" : rest;
                }
                else
                {
                    cmd = command.ArgByIndex(1);
                    description = command.ArgCount >= 3 ? command.ArgByIndex(2) : $"执行命令: {cmd}";
                }
            }
            else
            {
                cmd = command.ArgByIndex(1);
                description = command.ArgCount >= 3 ? command.ArgByIndex(2) : $"执行命令: {cmd}";
            }

            int caller = player?.Slot ?? VoteConstants.VOTE_CALLER_SERVER;

            _voteHandler.Init();

            string title = _pluginConfig.VoteCustom ? "#SFUI_vote_panorama_vote_orange" : "执行命令";

            bool success = _voteHandler.SendYesNoVoteToAll(
                flDuration: _pluginConfig.VoteDuration,
                iCaller: caller,
                sVoteTitle: title,
                sDetailStr: description,
                resultCallback: (info) => VoteExecuteResultCallback(info, cmd),
                handler: VoteHandlerCallback
            );

            if (!success)
            {
                if (player == null)
                    Console.WriteLine(" [ZEP] 发起投票失败，可能已有投票在进行中。");
                else
                    player.PrintToChat($"{Prefix} {ChatColors.Red} 发起投票失败，可能已有投票在进行中。");
            }
        }

        public void CommandVoteRestart(CCSPlayerController? player, CommandInfo info)
        {
            if (!AdminCheck(player))
                return;

            float duration = _pluginConfig.VoteDuration;
            float ratio = _pluginConfig.VoteRatio;
            int caller = player?.Slot ?? VoteConstants.VOTE_CALLER_SERVER;

            if (_voteHandler == null)
            {
                if (player == null)
                    Console.WriteLine(" [ZEP] 投票系统未初始化，请联系管理员。");
                else
                    player.PrintToChat($"{Prefix} {ChatColors.Red} 投票系统未初始化，请联系管理员。");
                return;
            }

            _voteHandler.Init();

            YesNoVoteResult resultCallback = (voteInfo) =>
            {
                int yes = voteInfo.yes_votes;
                int no = voteInfo.no_votes;
                int total = yes + no;

                if (total == 0)
                {
                    Server.PrintToChatAll($"{Prefix} {ChatColors.Red}Vote ended with no votes.");
                    return false;
                }

                float yesRatio = (float)yes / total;
                bool passed = yesRatio >= ratio;

                if (passed)
                {
                    Server.PrintToChatAll($"{Prefix} {ChatColors.Green}Vote passed ({yesRatio * 100:F1}%). Restarting match...");
                    Server.ExecuteCommand("mp_restartgame 1");
                }
                else
                {
                    Server.PrintToChatAll($"{Prefix} {ChatColors.Red}Vote failed ({yesRatio * 100:F1}% yes, need {ratio * 100:F0}%).");
                }
                return passed;
            };

            _voteHandler.SendYesNoVoteToAll(duration, caller, "#SFUI_vote_restart_game", "", resultCallback);
        }

        private bool VoteForResultCallback(YesNoVoteInfo info)
        {
            int yes = info.yes_votes;
            int no = info.no_votes;
            int total = yes + no;
            bool passed = total > 0 && (float)yes / total >= _pluginConfig.VoteRatio;
            Server.PrintToChatAll(passed ? $"{Prefix} {ChatColors.Green}投票通过！" : $"{Prefix} {ChatColors.Red}投票未通过。");
            return passed;
        }

        private bool VoteExecuteResultCallback(YesNoVoteInfo info, string commandToExecute)
        {
            int yes = info.yes_votes;
            int no = info.no_votes;
            int total = yes + no;
            bool passed = total > 0 && (float)yes / total >= _pluginConfig.VoteRatio;
            if (passed)
            {
                Server.PrintToChatAll($"{Prefix} {ChatColors.Green} 投票通过！{ChatColors.Gold} 将执行命令: {commandToExecute}");
                Server.ExecuteCommand(commandToExecute);
            }
            else
            {
                Server.PrintToChatAll($"{Prefix} {ChatColors.Red} 投票未通过，命令不会执行。");
            }
            return passed;
        }

        private void VoteHandlerCallback(YesNoVoteAction action, int param1, int param2)
        {
            switch (action)
            {
                case YesNoVoteAction.VoteAction_Start:
                    Console.WriteLine("[ZEP] Vote started.");
                    break;
                case YesNoVoteAction.VoteAction_Vote:
                    var player = Utilities.GetPlayerFromSlot(param1);
                    if (player != null && player.IsValid)
                    {
                        player.PrintToChat($"{Prefix} {ChatColors.Red} 感谢投票！您选择了 {(param2 == 1 ? "反对" : "赞成")}。");
                    }
                    break;
                case YesNoVoteAction.VoteAction_End:
                    Console.WriteLine($"[ZEP] Vote ended, reason: {(YesNoVoteEndReason)param1}");
                    break;
            }
        }
    }
}