namespace Snail.MCP.Vault.Application.Ports;

/// <summary>Access to the vault's markdown files by vault-relative path.</summary>
public interface IVaultStore
{
    /// <summary>Vault-relative paths of every Markdown file outside service folders.</summary>
    IEnumerable<string> MarkdownFiles();

    /// <summary>The text of a file.</summary>
    string Read(string relativePath);

    /// <summary>Whether a file is there.</summary>
    bool Exists(string relativePath);

    /// <summary>Writes a file, creating its folder when missing.</summary>
    void Write(string relativePath, string content);
}
