// Adapted 2026-10-14 from iTerm2 VT100Terminal.m, exitCodeFromArgs:
// 069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e.
// Copyright George Nachman and contributors. GPL-3.0-or-later (elected from GPLv2-or-later).
using System.Globalization;

namespace Aster.Sessions.Protocols;

public static class ItermSemanticPrompt
{
    /// <summary>Attributes may appear before or after the positional exit code.</summary>
    public static int? ExitCode(string payload)
    {
        var args = payload.Split(';');
        if (args[0] != "D") return null;
        foreach (var arg in args.Skip(1))
        {
            if (arg.Length == 0 || arg.Contains('=')) continue;
            if (int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
                return code;
        }
        return null;
    }
}
