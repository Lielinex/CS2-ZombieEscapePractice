using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using PanoramaVote;

namespace ZombieEscapePractice
{
    public class ChallengePlugin : BasePlugin
    {
        public override string ModuleName => "ZombieEscapePractice";
        public override string ModuleDescription => "僵尸逃跑弹幕图练习插件";
        public override string ModuleAuthor => "Lielinex";
        public override string ModuleVersion => "1.0.1";

        public string Prefix = $" {ChatColors.Gold}[{ChatColors.Green}ZEP{ChatColors.Gold}]";

        private PluginConfig _pluginConfig;
        private MapConfigLoader _configManager;
        private Blocks _blockManager;
        private PracticeManager? _practiceManager;
        private VoteManager? _voteManager;
        private CPanoramaVote? _voteHandler;

        public override void Load(bool hotReload)
        {
            Console.WriteLine("[ZombieEscapePractice] 正在加载...");
            string configPath = Path.Combine(ModuleDirectory, "..", "..", "configs", "plugins", "ZombieEscapePractice", "ZEPconfig.json");
            _pluginConfig = PluginConfig.Load(configPath);
            Console.WriteLine($"[ZombieEscapePractice] 配置加载完成: Practice={_pluginConfig.EnablePractice}, Vote={_pluginConfig.EnableVote}, Debug={_pluginConfig.EnableDebug}");

            _configManager = new MapConfigLoader();
            _configManager.Load(ModuleDirectory);

            _blockManager = new Blocks(this, _configManager, _pluginConfig);
            AddCommand("css_zep", "控制训练命令块 (enable/disable/trigger <targetname>)", (player, info) => _blockManager.CommandZep(player, info));

            if (_pluginConfig.EnablePractice)
            {
                _practiceManager = new PracticeManager(this, _configManager);
                AddCommand("css_practice", "打开训练菜单", (player, info) => _practiceManager.CommandPractice(player, info));
                AddCommand("css_prac", "打开训练菜单", (player, info) => _practiceManager.CommandPractice(player, info));
            }

            if (_pluginConfig.EnableVote)
            {
                _voteManager = new VoteManager(this, _pluginConfig);
                AddCommand("css_vote_for", "发起一个投票，格式: !vote_for <内容>", (player, info) => _voteManager.CommandVoteFor(player, info));
                AddCommand("css_vote_execute", "发起一个执行命令的投票，格式: !vote_execute \"命令\" [说明]", (player, info) => _voteManager.CommandVoteExecute(player, info));
                AddCommand("css_vote_restart", "发起一个重启回合投票，格式: !vote_restart", (player, info) => _voteManager.CommandVoteRestart(player, info));
            }

            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventVoteCast>((@event, info) =>
            {
                _voteHandler.VoteCast(@event);
                return HookResult.Continue;
            });

            Console.WriteLine("[ZombieEscapePractice] 加载完成！");
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _blockManager?.OnRoundStart();
            _practiceManager?.OnRoundStart();
            Timer.CancelAll();
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            _blockManager?.OnRoundEnd();
            _practiceManager?.OnRoundEnd();
            Timer.CancelAll();
            return HookResult.Continue;
        }

        public override void Unload(bool hotReload)
        {
            Timer.CancelAll();
            base.Unload(hotReload);
        }
    }
}