// Minimal API doubles let routing regressions run without loading a CS2 server.
namespace CounterStrikeSharp.API.Core
{
    public enum HookMode { Pre, Post }
    public enum HookResult { Continue, Handled }
    public sealed class CommandDefinition(string name)
    {
        public string Name { get; } = name;
    }
    public sealed class BasePlugin
    {
        public List<CommandDefinition> CommandDefinitions { get; } = [];
        public Dictionary<string, Modules.Commands.CommandInfo.CommandListenerCallback> Listeners { get; } = [];
        public void AddCommandListener(string name,
            Modules.Commands.CommandInfo.CommandListenerCallback callback, HookMode mode)
        {
            if (mode != HookMode.Pre) throw new Exception("Chat must be intercepted before native dispatch");
            Listeners.Add(name, callback);
        }
    }
    public sealed class CCSPlayerController
    {
        public bool IsValid { get; set; } = true;
        public List<string> DispatchedCommands { get; } = [];
        public void ExecuteClientCommandFromServer(string command) => DispatchedCommands.Add(command);
    }
    public static class CoreConfig
    {
        public static IEnumerable<string> SilentChatTrigger { get; set; } = ["/"];
    }
}

namespace CounterStrikeSharp.API.Modules.Commands
{
    public sealed class CommandInfo(string arguments, int argCount = 2)
    {
        public delegate Core.HookResult CommandListenerCallback(Core.CCSPlayerController? player, CommandInfo info);
        public string ArgString { get; } = arguments;
        public int ArgCount { get; } = argCount;
    }
}
