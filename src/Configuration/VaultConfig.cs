using Microsoft.Extensions.Configuration;

namespace Snail.MCP.Vault.Configuration;

/// <summary>Server settings: the settings file of Snail.Sdk's chain, then the <c>SNAIL_MCP_VAULT_</c> variables, which win.</summary>
public sealed record VaultConfig
{
    /// <summary>The variable naming the vault root.</summary>
    public const string PathVariable = "SNAIL_MCP_VAULT_PATH";

    /// <summary>The same setting as a field of the settings file.</summary>
    public const string PathKey = "Path";

    /// <summary>Absolute path of the vault root.</summary>
    public required string RootPath { get; init; }

    /// <summary>What the settings say; throws when the vault root is not set or does not exist.</summary>
    public static VaultConfig Load(IConfiguration settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var root = settings[PathKey];
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException($"Set {PathVariable}, or \"{PathKey}\" in the settings file, to the absolute path of the Obsidian vault root.");

        var fullPath = Path.GetFullPath(root);
        if (!Directory.Exists(fullPath))
            throw new InvalidOperationException($"{PathVariable} points to '{fullPath}', which does not exist.");

        return new VaultConfig { RootPath = fullPath };
    }
}
