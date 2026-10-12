using Snail.MCP.Vault.Application.Ports;

namespace Snail.MCP.Vault.Benchmarks;

/// <summary>A vault held in memory, so that a benchmark measures the drawing and not the disk.</summary>
public sealed class MemoryVault : IVaultStore
{
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

    public IEnumerable<string> MarkdownFiles() => _files.Keys;

    public string Read(string relativePath) => _files[relativePath];

    public bool Exists(string relativePath) => _files.ContainsKey(relativePath);

    public void Write(string relativePath, string content) => _files[relativePath] = content;
}
