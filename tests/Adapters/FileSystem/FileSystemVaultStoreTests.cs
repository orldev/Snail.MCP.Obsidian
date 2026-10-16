using Snail.MCP.Vault.Adapters.FileSystem;
using Snail.MCP.Vault.Application;
using Snail.MCP.Vault.Configuration;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Vault.Tests.Adapters.FileSystem;

public class FileSystemVaultStoreTests : IDisposable
{
    private readonly string _root = TestFolder.Create();

    [Fact]
    public void MarkdownFiles_ServiceFolderBeside_ListsOnlyNotesOutsideIt()
    {
        WriteFile("Note.md", "note");
        WriteFile("Sub/Deep.md", "deep");
        WriteFile(".obsidian/config.md", "service");
        WriteFile("Image.png", "binary");

        var files = Store().MarkdownFiles().OrderBy(p => p).ToList();

        Assert.Equal(["Note.md", "Sub/Deep.md"], files);
    }

    [Fact]
    public void Read_RelativePath_GivesTheText()
    {
        WriteFile("Note.md", "the content");

        Assert.Equal("the content", Store().Read("Note.md"));
    }

    [Fact]
    public void ReadHead_LongNote_GivesItsFirst512Characters()
    {
        WriteFile("Long.md", new string('x', 4096));

        Assert.Equal(512, Store().ReadHead("Long.md")!.Length);
    }

    /// <summary>A note deleted after the listing enumerated it is gone, not a failure of the listing.</summary>
    [Fact]
    public void ReadHead_NoteDeletedMeanwhile_IsNull()
    {
        Assert.Null(Store().ReadHead("Deleted/Meanwhile.md"));
    }

    [Fact]
    public void Read_PathEscapingTheVault_IsRefused()
    {
        Assert.Throws<UnauthorizedAccessException>(() => Store().Read("../outside.md"));
    }

    [Fact]
    public void MarkdownFiles_WithoutAVault_IsRefusedWithTheInstruction()
    {
        var store = new FileSystemVaultStore(new VaultConfig { Problem = "Name the vault." });

        var refusal = Assert.Throws<VaultRefusal>(() => store.MarkdownFiles().ToList());

        Assert.Equal("Name the vault.", refusal.Message);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private FileSystemVaultStore Store() => new(new VaultConfig { RootPath = _root });

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}
