namespace Snail.MCP.Vault.Application.Ports;

/// <summary>Access to the vault's markdown files by vault-relative path.</summary>
public interface IVaultStore
{
    IEnumerable<string> MarkdownFiles();

    string Read(string relativePath);

    bool Exists(string relativePath);

    void Write(string relativePath, string content);
}
