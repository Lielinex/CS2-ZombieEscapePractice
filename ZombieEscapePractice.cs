using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace ZombieEscapePractice
{
    public class ChallengePlugin : BasePlugin
    {
        public override string ModuleName => "ZombieEscapePractice";
        public override string ModuleDescription => "僵尸逃跑弹幕图练习插件";
        public override string ModuleAuthor => "Lielinex";
        public override string ModuleVersion => "4.0.0";

        private PluginConfig _pluginConfig;
        private ConfigManager _configManager;
        private BlockManager _blockManager;
        private PracticeManager? _practiceManager;
        private VoteManager? _voteManager;

        public override void Load(bool hotReload)
        {
            Console.WriteLine("[ZombieEscapePractice] 正在加载...");

            // 加载插件配置
            string configPath = Path.Combine(ModuleDirectory, "..", "..", "configs", "plugins", "ZombieEscapePractice", "ZEPconfig.json");
            _pluginConfig = PluginConfig.Load(configPath);
            Console.WriteLine($"[ZombieEscapePractice] 配置加载完成: Practice={_pluginConfig.EnablePractice}, Vote={_pluginConfig.EnableVote}, Debug={_pluginConfig.EnableDebug}");

            // 加载地图配置
            _configManager = new ConfigManager();
            _configManager.Load(ModuleDirectory);

            // 初始化命令块管理器（始终启用）
            _blockManager = new BlockManager(this, _configManager, _pluginConfig);
            AddCommand("css_zep", "控制训练命令块 (enable/disable/trigger <targetname>)", (player, info) => _blockManager.CommandZep(player, info));

            // 根据配置初始化其他管理器并注册命令
            if (_pluginConfig.EnablePractice)
            {
                _practiceManager = new PracticeManager(this, _configManager);
                AddCommand("css_practice", "打开训练菜单", (player, info) => _practiceManager.CommandPractice(player, info));
                AddCommand("css_prac", "打开训练菜单", (player, info) => _practiceManager.CommandPractice(player, info));
            }

            if (_pluginConfig.EnableVote)
            {
                _voteManager = new VoteManager(this);
                AddCommand("css_vote_for", "发起一个投票，格式: !vote_for <内容>", (player, info) => _voteManager.CommandVoteFor(player, info));
                AddCommand("css_vote_execute", "发起一个执行命令的投票，格式: !vote_execute \"命令\" [说明]", (player, info) => _voteManager.CommandVoteExecute(player, info));
            }

            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

            Console.WriteLine("[ZombieEscapePractice] 加载完成。");
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _blockManager?.OnRoundStart();
            _practiceManager?.OnRoundStart();
            TimerManager.CancelAll();
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            _blockManager?.OnRoundEnd();
            _practiceManager?.OnRoundEnd();
            TimerManager.CancelAll();
            return HookResult.Continue;
        }

        public override void Unload(bool hotReload)
        {
            TimerManager.CancelAll();
            base.Unload(hotReload);
        }
    }
}