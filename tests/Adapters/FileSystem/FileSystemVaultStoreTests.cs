using Snail.MCP.Obsidian.Adapters.FileSystem;
using Snail.MCP.Obsidian.Application;
using Snail.MCP.Obsidian.Configuration;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Obsidian.Tests.Adapters.FileSystem;

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
        Assert.Throws<VaultEscape>(() => Store().Read("../outside.md"));
    }

    /// <summary>
    /// A folder named as a scheme is the caller's mistake: the system's UnauthorizedAccessException, which the refusal of
    /// a path outside the vault was once named by, said "stay inside the vault" to a path that was inside.
    /// </summary>
    [Fact]
    public void Read_AFolder_IsRefusedAsNoScheme()
    {
        WriteFile("Excalidraw/Note.md", "note");

        var refusal = Assert.Throws<VaultRefusal>(() => Store().Read("Excalidraw"));

        Assert.Contains("folder", refusal.Message, StringComparison.Ordinal);
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
