using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using PanoramaVote;

namespace ZombieEscapePractice
{
    public class VoteManager
    {
        private readonly BasePlugin _plugin;
        private CPanoramaVote? _voteHandler;

        public VoteManager(BasePlugin plugin)
        {
            _plugin = plugin;
            _voteHandler = new CPanoramaVote(plugin);
            _plugin.RegisterEventHandler<EventVoteCast>((@event, info) =>
            {
                _voteHandler.VoteCast(@event);
                return HookResult.Continue;
            });
        }

        public void CommandVoteFor(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount < 2)
            {
                if (player == null)
                    Console.WriteLine(" [训练] 用法: css_vote_for <内容>");
                else
                    player.PrintToChat(" [训练] 用法: !vote_for <内容>");
                return;
            }

            if (_voteHandler == null)
            {
                string msg = " [训练] 投票系统未初始化，请联系管理员。";
                if (player == null) Console.WriteLine(msg);
                else player.PrintToChat(msg);
                return;
            }

            string content = command.ArgByIndex(1);
            int caller = player?.Slot ?? VoteConstants.VOTE_CALLER_SERVER;

            _voteHandler.Init();

            bool success = _voteHandler.SendYesNoVoteToAll(
                flDuration: 30.0f,
                iCaller: caller,
                sVoteTitle: "#SFUI_vote_panorama_vote_default",
                sDetailStr: content,
                resultCallback: VoteForResultCallback,
                handler: VoteHandlerCallback
            );

            if (!success)
            {
                string failMsg = " [训练] 发起投票失败，可能已有投票在进行中或服务器未正确配置。";
                if (player == null) Console.WriteLine(failMsg);
                else player.PrintToChat(failMsg);
            }
        }

        public void CommandVoteExecute(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount < 2)
            {
                if (player == null)
                    Console.WriteLine(" [训练] 用法: css_vote_execute \"命令\" [说明]");
                else
                    player.PrintToChat(" [训练] 用法: !vote_execute \"命令\" [说明]");
                return;
            }

            if (_voteHandler == null)
            {
                string msg = " [训练] 投票系统未初始化，请联系管理员。";
                if (player == null) Console.WriteLine(msg);
                else player.PrintToChat(msg);
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

            bool success = _voteHandler.SendYesNoVoteToAll(
                flDuration: 30.0f,
                iCaller: caller,
                sVoteTitle: "#SFUI_vote_panorama_vote_orange",
                sDetailStr: description,
                resultCallback: (info) => VoteExecuteResultCallback(info, cmd),
                handler: VoteHandlerCallback
            );

            if (!success)
            {
                string failMsg = " [训练] 发起投票失败，可能已有投票在进行中。";
                if (player == null) Console.WriteLine(failMsg);
                else player.PrintToChat(failMsg);
            }
        }

        private bool VoteForResultCallback(YesNoVoteInfo info)
        {
            bool passed = info.yes_votes > info.no_votes;
            Server.PrintToChatAll(passed ? " [训练] 投票通过！" : " [训练] 投票未通过。");
            return passed;
        }

        private bool VoteExecuteResultCallback(YesNoVoteInfo info, string commandToExecute)
        {
            bool passed = info.yes_votes > info.no_votes;
            if (passed)
            {
                Server.PrintToChatAll($" [训练] 投票通过！将执行命令: {commandToExecute}");
                Server.ExecuteCommand(commandToExecute);
            }
            else
            {
                Server.PrintToChatAll(" [训练] 投票未通过，命令不会执行。");
            }
            return passed;
        }

        private void VoteHandlerCallback(YesNoVoteAction action, int param1, int param2)
        {
            switch (action)
            {
                case YesNoVoteAction.VoteAction_Start:
                    Console.WriteLine("[Vote] Vote started.");
                    break;
                case YesNoVoteAction.VoteAction_Vote:
                    var player = Utilities.GetPlayerFromSlot(param1);
                    if (player != null && player.IsValid)
                    {
                        player.PrintToChat($" [训练] 感谢投票！您选择了 {(param2 == 1 ? "赞成" : "反对")}。");
                    }
                    break;
                case YesNoVoteAction.VoteAction_End:
                    Console.WriteLine($"[Vote] Vote ended, reason: {(YesNoVoteEndReason)param1}");
                    break;
            }
        }
    }
}