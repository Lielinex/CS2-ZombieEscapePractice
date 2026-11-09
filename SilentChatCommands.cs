using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ZombieEscapePractice;

internal sealed class SilentChatCommands
{
    private readonly BasePlugin _plugin;

    public SilentChatCommands(BasePlugin plugin)
    {
        _plugin = plugin;
        // BasePlugin tracks listeners by delegate; each command needs its own delegate.
        plugin.AddCommandListener("say", (player, info) => OnSay(player, info), HookMode.Pre);
        plugin.AddCommandListener("say_team", (player, info) => OnSay(player, info), HookMode.Pre);
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || info.ArgCount < 2) return HookResult.Continue;
        if (!SilentChatCommandParser.TryResolve(info.ArgString, CoreConfig.SilentChatTrigger,
                _plugin.CommandDefinitions.Select(definition => definition.Name), out var command))
            return HookResult.Continue;

        // Dispatch before Host_Say can trim silent chat arguments. Keep the player
        // context and framework permission checks instead of invoking callbacks directly.
        player.ExecuteClientCommandFromServer(command);
        // Suppress the original chat and its native dispatch to avoid executing twice.
        return HookResult.Handled;
    }
}
