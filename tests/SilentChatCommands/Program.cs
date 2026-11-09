using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using ZombieEscapePractice;

var plugin = new BasePlugin();
string[] commands = ["css_prac", "css_practice", "css_zep", "css_vote_for", "css_vote_execute",
    "css_vote_restart", "css_cancelvote", "css_revote"];
plugin.CommandDefinitions.AddRange(commands.Select(name => new CommandDefinition(name)));
_ = new SilentChatCommands(plugin);
var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}

Check(plugin.Listeners.Count == 2, "Both global and team chat must be registered");
Check(plugin.Listeners["say"] != plugin.Listeners["say_team"], "Listeners need distinct delegates for cleanup");
foreach (var channel in new[] { "say", "say_team" })
{
    var listener = plugin.Listeners[channel];
    foreach (var command in commands)
    foreach (var quoted in new[] { false, true })
    {
        var text = "/" + command[4..];
        var player = new CCSPlayerController();
        var result = listener(player, new CommandInfo(quoted ? $"\"{text}\"" : text));
        Check(result == HookResult.Handled, $"{channel}: {text} must be silent and stop native dispatch");
        Check(player.DispatchedCommands.SequenceEqual([command]), $"{channel}: {text} must dispatch once as this player");
    }

    (string Text, string Expected)[] argumentCases = [
        ("/zep trigger block_name", "css_zep trigger block_name"),
        ("\"/zep enable block_name\"", "css_zep enable block_name"),
        ("/vote_for \"hello world\"", "css_vote_for \"hello world\""),
        ("\"/vote_for \"hello world\"\"", "css_vote_for \"hello world\""),
        ("/vote_execute \"say hello; say world\" description", "css_vote_execute \"say hello; say world\" description"),
        ("\"/vote_execute \"say hello\" description\"", "css_vote_execute \"say hello\" description"),
        ("/PrAc", "css_prac"),
        ("/css_prac", "css_prac"),
        ("/vote_for\tcontent", "css_vote_for\tcontent")
    ];
    foreach (var (text, expected) in argumentCases)
    {
        var player = new CCSPlayerController();
        Check(listener(player, new CommandInfo(text)) == HookResult.Handled, $"{channel}: handle {text}");
        Check(player.DispatchedCommands.SequenceEqual([expected]), $"{channel}: preserve arguments: {text}");
    }

    foreach (var text in new[] { "hello", "!prac", "/", "", "\"\"", "/unknown", "/practice_extra", "/ prac", "/prac;quit" })
    {
        var player = new CCSPlayerController();
        Check(listener(player, new CommandInfo(text)) == HookResult.Continue, $"Leave unrelated chat alone: {text}");
        Check(player.DispatchedCommands.Count == 0, $"Do not dispatch unrelated chat: {text}");
    }
    Check(listener(null, new CommandInfo("/prac")) == HookResult.Continue, "Ignore server chat");
    var invalid = new CCSPlayerController { IsValid = false };
    Check(listener(invalid, new CommandInfo("/prac")) == HookResult.Continue, "Ignore invalid players");
    Check(invalid.DispatchedCommands.Count == 0, "Invalid players cannot dispatch");
    Check(listener(new CCSPlayerController(), new CommandInfo("/prac", 1)) == HookResult.Continue, "Ignore missing chat arguments");
}

CoreConfig.SilentChatTrigger = ["", "/", "//", "#"];
foreach (var text in new[] { "//prac", "#prac" })
{
    var player = new CCSPlayerController();
    Check(plugin.Listeners["say"](player, new CommandInfo(text)) == HookResult.Handled, "Use configured triggers, longest first");
    Check(player.DispatchedCommands.SequenceEqual(["css_prac"]), "Strip the complete configured prefix");
}
CoreConfig.SilentChatTrigger = [];
Check(plugin.Listeners["say"](new CCSPlayerController(), new CommandInfo("/prac")) == HookResult.Continue,
    "Honor disabled silent triggers");
CoreConfig.SilentChatTrigger = ["/"];
plugin.CommandDefinitions.RemoveAll(definition => definition.Name == "css_prac");
Check(plugin.Listeners["say"](new CCSPlayerController(), new CommandInfo("/prac")) == HookResult.Continue,
    "Do not route commands belonging to disabled features");
Console.WriteLine($"Passed {checks} silent chat routing checks.");
