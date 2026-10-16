using Microsoft.Extensions.Configuration;
using Snail.MCP.Obsidian.Application;

namespace Snail.MCP.Obsidian.Configuration;

/// <summary>Server settings: the settings file of Snail.Sdk's chain, then the <c>SNAIL_MCP_OBSIDIAN_</c> variables, which win.</summary>
/// <remarks>
/// A vault not named, or named and not there, is reported rather than thrown: the server starts, and every tool tells
/// the agent how to name one. Thrown while the services were registered, it aborted the native program before the
/// client had connected, and Claude Code showed no reason at all.
/// </remarks>
public sealed record VaultConfig
{
    /// <summary>The variable naming the vault root.</summary>
    public const string PathVariable = "SNAIL_MCP_OBSIDIAN_PATH";

    /// <summary>The same setting as a field of the settings file.</summary>
    public const string PathKey = "Path";

    /// <summary>The settings file a machine keeps its vault in.</summary>
    public const string SettingsFile = "~/.config/s3l-mcp-obsidian/config.json";

    /// <summary>Absolute path of the vault root; <c>null</c> when the settings name none that exists.</summary>
    public string? RootPath { get; init; }

    /// <summary>Why there is no vault, said so that the agent can pass it on; <c>null</c> when there is one.</summary>
    public string? Problem { get; init; }

    /// <summary>The vault root, or a refusal that says how to name one.</summary>
    public string Root => RootPath ?? throw new VaultRefusal(Problem ?? NotNamed);

    private static string NotNamed =>
        $"No vault is named: set {PathVariable}, or \"{PathKey}\" in {SettingsFile}, to the absolute path of the Obsidian vault root.";

    /// <summary>What the settings say about the vault, a missing one included.</summary>
    public static VaultConfig Load(IConfiguration settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var root = settings[PathKey];
        if (string.IsNullOrWhiteSpace(root))
            return new VaultConfig { Problem = NotNamed };

        var fullPath = Path.GetFullPath(root);
        if (!Directory.Exists(fullPath))
            return new VaultConfig { Problem = $"The vault '{fullPath}' does not exist: set {PathVariable}, or \"{PathKey}\" in {SettingsFile}, to an existing folder." };

        return new VaultConfig { RootPath = fullPath };
    }
}
