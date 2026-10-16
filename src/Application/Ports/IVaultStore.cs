namespace Snail.MCP.Obsidian.Application.Ports;

/// <summary>Access to the vault's markdown files by vault-relative path.</summary>
public interface IVaultStore
{
    /// <summary>Vault-relative paths of every Markdown file outside service folders.</summary>
    IEnumerable<string> MarkdownFiles();

    /// <summary>The text of a file.</summary>
    string Read(string relativePath);

    /// <summary>The first characters of a file, enough to recognise it; <c>null</c> when it is gone.</summary>
    string? ReadHead(string relativePath);

    /// <summary>Whether a file is there.</summary>
    bool Exists(string relativePath);

    /// <summary>Writes a file whole or not at all, creating its folder; <c>false</c> when it exists and <paramref name="overwrite"/> is not set.</summary>
    bool TryWrite(string relativePath, string content, bool overwrite);
}
