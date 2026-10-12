using Snail.MCP.Vault.Application.Ports;
using Snail.MCP.Vault.Configuration;

namespace Snail.MCP.Vault.Adapters.FileSystem;

/// <summary>Serves vault files from disk, refusing paths that escape the vault root.</summary>
public sealed class FileSystemVaultStore(VaultConfig config) : IVaultStore
{
    /// <inheritdoc/>
    public IEnumerable<string> MarkdownFiles()
    {
        var enumeration = new EnumerationOptions { RecurseSubdirectories = true };

        foreach (var file in Directory.EnumerateFiles(config.RootPath, "*.md", enumeration))
        {
            var relative = Path.GetRelativePath(config.RootPath, file);
            if (!IsInsideServiceFolder(relative))
                yield return relative.Replace(Path.DirectorySeparatorChar, '/');
        }
    }

    /// <inheritdoc/>
    public string Read(string relativePath) => File.ReadAllText(Resolve(relativePath));

    /// <inheritdoc/>
    public bool Exists(string relativePath) => File.Exists(Resolve(relativePath));

    /// <inheritdoc/>
    public void Write(string relativePath, string content)
    {
        var fullPath = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private static bool IsInsideServiceFolder(string relativePath) =>
        relativePath.Split(Path.DirectorySeparatorChar).Any(segment => segment.StartsWith('.'));

    private string Resolve(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(config.RootPath, relativePath));
        if (!fullPath.StartsWith(config.RootPath, StringComparison.Ordinal))
            throw new UnauthorizedAccessException($"The path '{relativePath}' escapes the vault root.");

        return fullPath;
    }
}
