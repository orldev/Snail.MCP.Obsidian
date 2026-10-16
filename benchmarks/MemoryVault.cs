using Snail.MCP.Obsidian.Application.Ports;

namespace Snail.MCP.Obsidian.Benchmarks;

/// <summary>A vault held in memory, so that a benchmark measures the drawing and not the disk.</summary>
public sealed class MemoryVault : IVaultStore
{
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

    public IEnumerable<string> MarkdownFiles() => _files.Keys;

    public string Read(string relativePath) => _files[relativePath];

    public string? ReadHead(string relativePath) =>
        _files.TryGetValue(relativePath, out var content) ? content[..Math.Min(512, content.Length)] : null;

    public bool Exists(string relativePath) => _files.ContainsKey(relativePath);

    public bool TryWrite(string relativePath, string content, bool overwrite)
    {
        if (!overwrite && _files.ContainsKey(relativePath))
            return false;

        _files[relativePath] = content;

        return true;
    }
}
