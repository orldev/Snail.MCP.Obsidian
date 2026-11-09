namespace Snail.MCP.Obsidian.Tests.Application.Excalidraw;

/// <summary>A theory over the schemes of a real vault, run only when the developer names one; skipped, with how to run it, otherwise.</summary>
/// <remarks>
/// The tests read the user's own vault, ~/Documents/Obsidian Vault, whenever nothing was named, and passed in silence on
/// a machine without it: a suite read private notes by default and claimed a parity it never checked. A vault is now read
/// only when <see cref="Variable"/> names it.
/// </remarks>
public sealed class RealVaultTheoryAttribute : TheoryAttribute
{
    /// <summary>The variable naming the vault whose schemes the tests read.</summary>
    public const string Variable = "SNAIL_OBSIDIAN_REAL_VAULT";

    public RealVaultTheoryAttribute()
    {
        if (Path is null)
            Skip = $"Set {Variable} to a vault whose Excalidraw folders hold schemes the plugin wrote, to check the parity with them.";
    }

    /// <summary>The vault <see cref="Variable"/> names, or null when it names none.</summary>
    public static string? Path => Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } path ? path : null;
}
