namespace Snail.MCP.Vault.Configuration;

/// <summary>Server settings resolved from environment variables.</summary>
public sealed record VaultConfig
{
    public const string PathVariable = "SNAIL_MCP_VAULT_PATH";

    public required string RootPath { get; init; }

    public static VaultConfig Load()
    {
        var root = Environment.GetEnvironmentVariable(PathVariable);
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException($"Set {PathVariable} to the absolute path of the Obsidian vault root.");

        var fullPath = Path.GetFullPath(root);
        if (!Directory.Exists(fullPath))
            throw new InvalidOperationException($"{PathVariable} points to '{fullPath}', which does not exist.");

        return new VaultConfig { RootPath = fullPath };
    }
}
