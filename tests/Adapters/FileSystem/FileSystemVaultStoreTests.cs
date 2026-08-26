using Snail.MCP.Vault.Adapters.FileSystem;
using Snail.MCP.Vault.Configuration;

namespace Snail.MCP.Vault.Tests.Adapters.FileSystem;

public class FileSystemVaultStoreTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("vault-store-tests").FullName;

    [Fact]
    public void EnumeratesMarkdownFilesOutsideServiceFolders()
    {
        WriteFile("Note.md", "note");
        WriteFile("Sub/Deep.md", "deep");
        WriteFile(".obsidian/config.md", "service");
        WriteFile("Image.png", "binary");

        var files = Store().MarkdownFiles().OrderBy(p => p).ToList();

        Assert.Equal(["Note.md", Path.Combine("Sub", "Deep.md")], files);
    }

    [Fact]
    public void ReadsFileByRelativePath()
    {
        WriteFile("Note.md", "the content");

        Assert.Equal("the content", Store().Read("Note.md"));
    }

    [Fact]
    public void RefusesPathThatEscapesTheVault()
    {
        Assert.Throws<UnauthorizedAccessException>(() => Store().Read("../outside.md"));
    }

    public void Dispose() => Directory.Delete(_root, true);

    private FileSystemVaultStore Store() => new(new VaultConfig { RootPath = _root });

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}
