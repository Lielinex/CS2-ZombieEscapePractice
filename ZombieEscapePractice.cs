using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using PanoramaVote;

namespace ZombieEscapePractice
{
    public class ChallengePlugin : BasePlugin
    {
        public override string ModuleName => "ZombieEscapePractice";
        public override string ModuleDescription => "僵尸逃跑弹幕图练习插件";
        public override string ModuleAuthor => "Lielinex";
        public override string ModuleVersion => "3.5.4";

        private ConfigManager _configManager = new();
        private bool _isPracticeActive = false;
        private Dictionary<string, BlockBase> _namedBlocks = new();
        private Dictionary<string, BlockBase> _triggerableBlocks = new(); // 可触发的块
        private Dictionary<string, CounterStrikeSharp.API.Modules.Timers.Timer> _repeatTimers = new(); // 存储重复块的定时器
        private CPanoramaVote? _voteHandler; // 投票处理器实例

        public override void Load(bool hotReload)
        {
            Console.WriteLine("[ZombieEscapePractice] 正在加载.");
            _configManager.Load(ModuleDirectory);
            BuildNamedBlocks();
            _voteHandler = new CPanoramaVote(this);
            Console.WriteLine("[ZombieEscapePractice] Vote handler initialized.");
            RegisterEventHandler<EventVoteCast>((@event, info) =>
            {
                _voteHandler.VoteCast(@event);
                return HookResult.Continue;
            });

            AddCommand("css_practice", "打开训练菜单", CommandPractice);
            AddCommand("css_prac", "打开训练菜单", CommandPractice);
            AddCommand("css_zep", "控制训练命令块 (enable/disable/trigger <targetname>)", CommandZep);
            AddCommand("css_vote_for", "发起一个投票，格式: !vote_for <内容>", CommandVoteFor);
            AddCommand("css_vote_execute", "发起一个执行命令的投票，格式: !vote_execute \"命令\" [说明]", CommandVoteExecute);

            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            _isPracticeActive = false;
            TimerManager.CancelAll();
            _triggerableBlocks.Clear();
            CancelAllRepeatTimers();
            return HookResult.Continue;
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _isPracticeActive = false;
            TimerManager.CancelAll();
            _triggerableBlocks.Clear();
            CancelAllRepeatTimers();
            return HookResult.Continue;
        }

        private void CancelAllRepeatTimers()
        {
            foreach (var timer in _repeatTimers.Values)
            {
                timer.Kill();
            }
            _repeatTimers.Clear();
        }

        private void BuildNamedBlocks()
        {
            _namedBlocks.Clear();
            _triggerableBlocks.Clear();
            _repeatTimers.Clear();

            foreach (var kv in _configManager.Config)
            {
                foreach (var challenge in kv.Value)
                {
                    CollectNamedBlocks(challenge.Blocks);
                }
            }
        }

        private void CollectNamedBlocks(List<BlockBase> blocks)
        {
            foreach (var block in blocks)
            {
                if (!string.IsNullOrEmpty(block.Targetname))
                {
                    _namedBlocks[block.Targetname] = block;

                    // 将随机块加入可触发列表
                    if (block is RandomBlock)
                    {
                        _triggerableBlocks[block.Targetname] = block;
                    }
                }
                // 当前设计无嵌套块，无需递归
            }
        }

        private void CommandZep(CCSPlayerController? player, CommandInfo command)
        {
            // 移除 player == null 的检查，让控制台也能执行命令
            if (command.ArgCount < 3)
            {
                string message = "用法: !zep enable/disable/trigger <targetname>";
                if (player != null && player.IsValid)
                {
                    player.PrintToChat(message);
                }
                else
                {
                    Console.WriteLine(message);
                }
                return;
            }

            string action = command.ArgByIndex(1).ToLower();
            string target = command.ArgByIndex(2);

            if (!_namedBlocks.TryGetValue(target, out var block))
            {
                string message = $"未找到名为 {target} 的命令块";
                if (player != null && player.IsValid)
                {
                    player.PrintToChat(message);
                }
                else
                {
                    Console.WriteLine(message);
                }
                return;
            }

            if (action == "enable")
            {
                block.Enabled = true;
                // 如果是重复块，在启用时启动定时器
                if (block is RepeatBlock repeatBlock)
                {
                    if (!_repeatTimers.ContainsKey(target) || IsTimerKilled(_repeatTimers[target]))
                    {
                        StartRepeatTimer(target, repeatBlock);
                        string message = $"已启用并启动重复块: {target}";
                        if (player != null && player.IsValid)
                        {
                            player.PrintToChat(message);
                        }
                        else
                        {
                            Console.WriteLine(message);
                        }
                    }
                    else
                    {
                        string message = $"重复块 {target} 已经在运行中";
                        if (player != null && player.IsValid)
                        {
                            player.PrintToChat(message);
                        }
                        else
                        {
                            Console.WriteLine(message);
                        }
                    }
                }
                else
                {
                    string message = $"已启用命令块: {target}";
                    if (player != null && player.IsValid)
                    {
                        player.PrintToChat(message);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
            }
            else if (action == "disable")
            {
                block.Enabled = false;
                // 如果是重复块，在禁用时停止定时器
                if (block is RepeatBlock && _repeatTimers.ContainsKey(target))
                {
                    var timer = _repeatTimers[target];
                    if (!IsTimerKilled(timer))
                    {
                        timer.Kill();
                    }
                    _repeatTimers.Remove(target);
                    string message = $"已禁用并停止重复块: {target}";
                    if (player != null && player.IsValid)
                    {
                        player.PrintToChat(message);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
                else
                {
                    string message = $"已禁用命令块: {target}";
                    if (player != null && player.IsValid)
                    {
                        player.PrintToChat(message);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
            }
            else if (action == "trigger")
            {
                // 检查是否为可触发的块
                if (_triggerableBlocks.ContainsKey(target))
                {
                    if (block.Enabled) // 只有启用状态的块才能被触发
                    {
                        TriggerBlock(block);
                        string message = $"已触发命令块: {target}";
                        if (player != null && player.IsValid)
                        {
                            player.PrintToChat(message);
                        }
                        else
                        {
                            Console.WriteLine(message);
                        }
                    }
                    else
                    {
                        string message = $"命令块 {target} 当前处于禁用状态，无法触发";
                        if (player != null && player.IsValid)
                        {
                            player.PrintToChat(message);
                        }
                        else
                        {
                            Console.WriteLine(message);
                        }
                    }
                }
                else
                {
                    string message = $"命令块 {target} 不支持触发操作";
                    if (player != null && player.IsValid)
                    {
                        player.PrintToChat(message);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
            }
            else
            {
                string message = "动作必须是 enable、disable 或 trigger";
                if (player != null && player.IsValid)
                {
                    player.PrintToChat(message);
                }
                else
                {
                    Console.WriteLine(message);
                }
            }
        }

        // 检查定时器是否已经被终止
        private bool IsTimerKilled(CounterStrikeSharp.API.Modules.Timers.Timer timer)
        {
            // 在CounterStrike Sharp中，我们可以简单地检查timer是否为null
            // 但实际上timer对象存在时，我们通过其他方式来跟踪其状态
            // 这里我们假设如果定时器被Kill()后，它会变为无效状态
            return timer == null;
        }

        private void StartRepeatTimer(string targetName, RepeatBlock repeatBlock)
        {
            if (!_namedBlocks.ContainsKey(targetName) || !(_namedBlocks[targetName] is RepeatBlock block))
            {
                return;
            }

            // 使用一个包装类来持有计数器，以便在lambda表达式中使用
            var executionState = new ExecutionState { Count = 0 };

            // 创建递归函数来处理重复执行
            Action executeAndSchedule = null;
            executeAndSchedule = () =>
            {
                // 再次检查块是否仍然启用
                if (!block.Enabled || !(_namedBlocks.ContainsKey(targetName) && _namedBlocks[targetName] is RepeatBlock activeBlock && activeBlock.Enabled))
                {
                    // 如果块已被禁用，停止定时器
                    if (_repeatTimers.ContainsKey(targetName) && !IsTimerKilled(_repeatTimers[targetName]))
                    {
                        _repeatTimers[targetName].Kill();
                        _repeatTimers.Remove(targetName);
                    }
                    return;
                }

                ExecuteCommands(block.Commands, 0);
                executionState.Count++;

                if (block.Count.HasValue && block.Count.Value >= 0 && executionState.Count >= block.Count.Value)
                {
                    // 达到执行次数，移除定时器
                    if (_repeatTimers.ContainsKey(targetName) && !IsTimerKilled(_repeatTimers[targetName]))
                    {
                        _repeatTimers[targetName].Kill();
                        _repeatTimers.Remove(targetName);
                    }
                    return;
                }

                // 继续调度下一次执行
                var newTimer = AddTimer(block.Interval, executeAndSchedule, TimerFlags.STOP_ON_MAPCHANGE);
                _repeatTimers[targetName] = newTimer;
            };

            // 开始第一次执行
            var initialTimer = AddTimer(block.Interval, executeAndSchedule, TimerFlags.STOP_ON_MAPCHANGE);
            _repeatTimers[targetName] = initialTimer;
        }

        // 简单的类来保存执行状态
        private class ExecutionState
        {
            public int Count { get; set; }
        }

        // 触发特定块的逻辑
        private void TriggerBlock(BlockBase block)
        {
            if (block is RandomBlock randomBlock)
            {
                ExecuteRandomBlock(randomBlock, 0f);
            }
            // 可以扩展其他类型的块的触发逻辑
        }

        private void ExecuteRandomBlock(RandomBlock randomBlock, float baseDelay)
        {
            if (randomBlock.Commands.Count == 0) return;

            string selectedCmd;
            if (randomBlock.Mode.ToLower() == "pickrandomshuffle")
            {
                if (randomBlock.ShuffleQueue == null || randomBlock.ShuffleQueue.Count == 0)
                {
                    var shuffled = randomBlock.Commands.OrderBy(x => Guid.NewGuid()).ToList();
                    randomBlock.ShuffleQueue = new Queue<string>(shuffled);
                }
                selectedCmd = randomBlock.ShuffleQueue.Dequeue();
            }
            else
            {
                int idx = new Random().Next(randomBlock.Commands.Count);
                selectedCmd = randomBlock.Commands[idx];
            }

            var (delay, actualCmd) = ParseCommandString(selectedCmd);
            float totalDelay = baseDelay + delay;

            if (totalDelay > 0)
            {
                var timer = AddTimer(totalDelay, () => Server.ExecuteCommand(actualCmd), TimerFlags.STOP_ON_MAPCHANGE);
                TimerManager.AddTimer(timer);
            }
            else
            {
                Server.ExecuteCommand(actualCmd);
            }
        }

        private void CommandPractice(CCSPlayerController? player, CommandInfo command)
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
                    Console.WriteLine($"[训练] 传送角度: pitch={challenge.Angle.Pitch}, yaw={challenge.Angle.Yaw}, roll={challenge.Angle.Roll}");
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
                            var timer = AddTimer(totalDelay, () => Server.ExecuteCommand(actualCmd), TimerFlags.STOP_ON_MAPCHANGE);
                            TimerManager.AddTimer(timer);
                        }
                        else
                        {
                            Server.ExecuteCommand(actualCmd);
                        }
                    }
                }
                else if (block is RepeatBlock repeat)
                {
                    if (!string.IsNullOrEmpty(repeat.Targetname))
                    {
                        // 如果不是禁用开始，则立即启动定时器
                        if (!repeat.StartDisabled)
                        {
                            StartRepeatTimer(repeat.Targetname, repeat);
                        }
                    }
                }
                else if (block is RandomBlock random)
                {
                    // 初始化随机块时将其加入可触发列表
                    if (!string.IsNullOrEmpty(random.Targetname))
                    {
                        _triggerableBlocks[random.Targetname] = random;
                    }

                    // 如果不是禁用开始，则启用它
                    if (!random.StartDisabled)
                    {
                        random.Enabled = true;
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
                    var timer = AddTimer(totalDelay, () => Server.ExecuteCommand(actualCmd), TimerFlags.STOP_ON_MAPCHANGE);
                    TimerManager.AddTimer(timer);
                }
                else
                {
                    Server.ExecuteCommand(actualCmd);
                }
            }
        }

        [RequiresPermissions("@css/vote")]
        private void CommandVoteFor(CCSPlayerController? player, CommandInfo command)
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

        [RequiresPermissions("@css/vote")]
        private void CommandVoteExecute(CCSPlayerController? player, CommandInfo command)
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

            // 解析带引号的命令（支持单引号或双引号）
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
                    // 没有匹配的引号，退化为简单处理
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
            // 可选：记录或提示
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