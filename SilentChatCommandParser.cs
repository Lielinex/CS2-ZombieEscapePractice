namespace ZombieEscapePractice;

internal static class SilentChatCommandParser
{
    public static bool TryResolve(string arguments, IEnumerable<string> triggers,
        IEnumerable<string> registeredCommands, out string command)
    {
        command = string.Empty;
        // Remove only the chat envelope, preserving quotes inside command arguments.
        var message = arguments.Trim();
        if (message.Length >= 2 && message[0] == '"' && message[^1] == '"')
            message = message[1..^1];

        var trigger = triggers
            .Where(prefix => !string.IsNullOrEmpty(prefix) && message.StartsWith(prefix, StringComparison.Ordinal))
            .OrderByDescending(prefix => prefix.Length)
            .FirstOrDefault();
        if (trigger == null) return false;

        var text = message[trigger.Length..];
        var nameLength = 0;
        while (nameLength < text.Length && !char.IsWhiteSpace(text[nameLength])) nameLength++;
        if (nameLength == 0) return false;

        var name = text[..nameLength];
        if (!name.StartsWith("css_", StringComparison.OrdinalIgnoreCase)) name = "css_" + name;
        var registeredName = registeredCommands.FirstOrDefault(
            registered => string.Equals(registered, name, StringComparison.OrdinalIgnoreCase));
        if (registeredName == null) return false;

        command = registeredName + text[nameLength..];
        return true;
    }
}
