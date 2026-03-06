using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZombieEscapePractice
{
    public class BlockManager
    {
        private readonly BasePlugin _plugin;
        private readonly ConfigManager _configManager;
        private readonly PluginConfig _pluginConfig;
        private Dictionary<string, BlockBase> _namedBlocks = new();
        private Dictionary<string, BlockBase> _triggerableBlocks = new();
        private Dictionary<string, CounterStrikeSharp.API.Modules.Timers.Timer> _repeatTimers = new();

        public BlockManager(BasePlugin plugin, ConfigManager configManager, PluginConfig pluginConfig)
        {
            _plugin = plugin;
            _configManager = configManager;
            _pluginConfig = pluginConfig;
            BuildNamedBlocks();
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

                    if (block is RandomBlock)
                    {
                        _triggerableBlocks[block.Targetname] = block;
                    }
                }
                // 无嵌套块，无需递归
            }
        }

        public void OnRoundStart()
        {
            CancelAllRepeatTimers();
            _triggerableBlocks.Clear();
            // 重新构建命名块（因为配置可能已变？但通常不变，可以不重建）
        }

        public void OnRoundEnd()
        {
            CancelAllRepeatTimers();
            _triggerableBlocks.Clear();
        }

        private void CancelAllRepeatTimers()
        {
            foreach (var timer in _repeatTimers.Values)
            {
                timer.Kill();
            }
            _repeatTimers.Clear();
        }

        public void CommandZep(CCSPlayerController? player, CommandInfo command)
        {
            // 检查调试开关：如果调试关闭且是玩家执行，则拒绝
            if (player != null && !_pluginConfig.EnableDebug)
            {
                player.PrintToChat(" [训练] 调试模式已关闭，无法使用此命令。");
                return;
            }

            if (command.ArgCount < 3)
            {
                string message = "用法: !zep enable/disable/trigger <targetname>";
                if (player != null) player.PrintToChat(message);
                else Console.WriteLine(message);
                return;
            }

            string action = command.ArgByIndex(1).ToLower();
            string target = command.ArgByIndex(2);

            if (!_namedBlocks.TryGetValue(target, out var block))
            {
                string message = $"未找到名为 {target} 的命令块";
                if (player != null) player.PrintToChat(message);
                else Console.WriteLine(message);
                return;
            }

            if (action == "enable")
            {
                block.Enabled = true;
                if (block is RepeatBlock repeatBlock)
                {
                    if (!_repeatTimers.ContainsKey(target) || IsTimerKilled(_repeatTimers[target]))
                    {
                        StartRepeatTimer(target, repeatBlock);
                        string message = $"已启用并启动重复块: {target}";
                        if (player != null) player.PrintToChat(message);
                        else Console.WriteLine(message);
                    }
                    else
                    {
                        string message = $"重复块 {target} 已经在运行中";
                        if (player != null) player.PrintToChat(message);
                        else Console.WriteLine(message);
                    }
                }
                else
                {
                    string message = $"已启用命令块: {target}";
                    if (player != null) player.PrintToChat(message);
                    else Console.WriteLine(message);
                }
            }
            else if (action == "disable")
            {
                block.Enabled = false;
                if (block is RepeatBlock && _repeatTimers.ContainsKey(target))
                {
                    var timer = _repeatTimers[target];
                    if (!IsTimerKilled(timer)) timer.Kill();
                    _repeatTimers.Remove(target);
                    string message = $"已禁用并停止重复块: {target}";
                    if (player != null) player.PrintToChat(message);
                    else Console.WriteLine(message);
                }
                else
                {
                    string message = $"已禁用命令块: {target}";
                    if (player != null) player.PrintToChat(message);
                    else Console.WriteLine(message);
                }
            }
            else if (action == "trigger")
            {
                // 检查块类型是否支持触发
                if (block is RandomBlock)
                {
                    if (block.Enabled)
                    {
                        TriggerBlock(block);
                        string message = $"已触发命令块: {target}";
                        if (player != null) player.PrintToChat(message);
                        else Console.WriteLine(message);
                    }
                    else
                    {
                        string message = $"命令块 {target} 当前处于禁用状态，无法触发";
                        if (player != null) player.PrintToChat(message);
                        else Console.WriteLine(message);
                    }
                }
                else
                {
                    string message = $"命令块 {target} 不支持触发操作";
                    if (player != null) player.PrintToChat(message);
                    else Console.WriteLine(message);
                }
            }
            else
            {
                string message = "动作必须是 enable、disable 或 trigger";
                if (player != null) player.PrintToChat(message);
                else Console.WriteLine(message);
            }
        }

        private bool IsTimerKilled(CounterStrikeSharp.API.Modules.Timers.Timer timer) => timer == null;

        private void StartRepeatTimer(string targetName, RepeatBlock repeatBlock)
        {
            if (!_namedBlocks.ContainsKey(targetName) || !(_namedBlocks[targetName] is RepeatBlock block))
                return;

            var executionState = new ExecutionState { Count = 0 };

            Action executeAndSchedule = null;
            executeAndSchedule = () =>
            {
                if (!block.Enabled || !(_namedBlocks.ContainsKey(targetName) && _namedBlocks[targetName] is RepeatBlock activeBlock && activeBlock.Enabled))
                {
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
                    if (_repeatTimers.ContainsKey(targetName) && !IsTimerKilled(_repeatTimers[targetName]))
                    {
                        _repeatTimers[targetName].Kill();
                        _repeatTimers.Remove(targetName);
                    }
                    return;
                }

                var newTimer = _plugin.AddTimer(block.Interval, executeAndSchedule, TimerFlags.STOP_ON_MAPCHANGE);
                _repeatTimers[targetName] = newTimer;
            };

            var initialTimer = _plugin.AddTimer(block.Interval, executeAndSchedule, TimerFlags.STOP_ON_MAPCHANGE);
            _repeatTimers[targetName] = initialTimer;
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

        private void TriggerBlock(BlockBase block)
        {
            if (block is RandomBlock randomBlock)
            {
                ExecuteRandomBlock(randomBlock, 0f);
            }
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
                var timer = _plugin.AddTimer(totalDelay, () => Server.ExecuteCommand(actualCmd), TimerFlags.STOP_ON_MAPCHANGE);
                TimerManager.AddTimer(timer);
            }
            else
            {
                Server.ExecuteCommand(actualCmd);
            }
        }

        private class ExecutionState
        {
            public int Count { get; set; }
        }
    }
}