namespace Snail.MCP.Vault.Application.Ports;

/// <summary>Read access to the vault's markdown files by vault-relative path.</summary>
public interface IVaultStore
{
    IEnumerable<string> MarkdownFiles();

    string Read(string relativePath);
}
