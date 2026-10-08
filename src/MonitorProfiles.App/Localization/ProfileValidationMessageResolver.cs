namespace MonitorProfiles.App.Localization;

public sealed record LocalizedProfileError(string ResourceKey, IReadOnlyList<object?> Arguments);

public static class ProfileValidationMessageResolver
{
    public static string Resolve(IEnumerable<string> errors)
        => ResolveMessage(errors).ResourceKey;

    public static LocalizedProfileError ResolveMessage(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var messages = errors.ToArray();

        var unavailableMode = messages.FirstOrDefault(message =>
            message.Contains("The requested mode is unavailable on '", StringComparison.OrdinalIgnoreCase));
        if (unavailableMode is not null && TryReadQuotedValue(unavailableMode, "The requested mode is unavailable on '", out var modeDisplay))
        {
            return new LocalizedProfileError("Error.ProfileModeUnavailable", [modeDisplay]);
        }

        var disconnectedDisplay = messages.FirstOrDefault(message =>
            message.Contains("The configured display '", StringComparison.OrdinalIgnoreCase));
        if (disconnectedDisplay is not null && TryReadQuotedValue(disconnectedDisplay, "The configured display '", out var display))
        {
            return new LocalizedProfileError("Error.DisplayUnavailable", [display]);
        }

        if (messages.Any(message => message.Contains("profile name cannot be empty", StringComparison.OrdinalIgnoreCase)))
        {
            return new LocalizedProfileError("Editor.NameRequired", Array.Empty<object?>());
        }

        if (messages.Any(message => message.Contains("already exists", StringComparison.OrdinalIgnoreCase)))
        {
            return new LocalizedProfileError("Editor.DuplicateName", Array.Empty<object?>());
        }

        return new LocalizedProfileError("Error.InvalidProfile", Array.Empty<object?>());
    }

    private static bool TryReadQuotedValue(string message, string prefix, out string value)
    {
        var start = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            value = string.Empty;
            return false;
        }

        start += prefix.Length;
        var end = message.IndexOf('\'', start);
        if (end <= start)
        {
            value = string.Empty;
            return false;
        }

        value = message[start..end];
        return true;
    }
}
