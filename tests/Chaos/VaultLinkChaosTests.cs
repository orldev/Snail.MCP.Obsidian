using Snail.MCP.Obsidian.Adapters.FileSystem;
using Snail.MCP.Obsidian.Configuration;

namespace Snail.MCP.Obsidian.Tests.Chaos;

/// <summary>The vault boundary against symbolic links: a link is a door, and the boundary is where the door leads.</summary>
public sealed class VaultLinkChaosTests : IDisposable
{
    private readonly ChaosVault _vault = new();

    private readonly string _outside;

    public VaultLinkChaosTests()
    {
        _outside = Directory.CreateDirectory(Path.Combine(_vault.Parent, "Outside")).FullName;
        File.WriteAllText(Path.Combine(_outside, "Secret.md"), "excalidraw-plugin: parsed\nsecret");
    }

    public void Dispose() => _vault.Dispose();

    /// <summary>
    /// Breaks the boundary on write. The path is checked as text, "Linked/Stolen.md" lies inside the vault as text,
    /// and the file system follows the link out of it.
    /// </summary>
    [Fact]
    public void TryWrite_ThroughDirectoryLinkLeadingOut_IsRefused()
    {
        Directory.CreateSymbolicLink(Path.Combine(_vault.Root, "Linked"), _outside);

        var refusal = Record.Exception(() => Store().TryWrite("Linked/Stolen.md", "outside", overwrite: false));

        Assert.IsType<UnauthorizedAccessException>(refusal);
        Assert.False(File.Exists(Path.Combine(_outside, "Stolen.md")), "the file was written outside the vault");
    }

    /// <summary>Breaks the boundary on read: a note that is a link to a file outside reads that file.</summary>
    [Fact]
    public void Read_FileLinkLeadingOut_IsRefused()
    {
        File.CreateSymbolicLink(Path.Combine(_vault.Root, "Secret.md"), Path.Combine(_outside, "Secret.md"));

        var refusal = Record.Exception(() => Store().Read("Secret.md"));

        Assert.IsType<UnauthorizedAccessException>(refusal);
    }

    /// <summary>Breaks the listing: notes behind a directory link leading out are offered as notes of the vault.</summary>
    [Fact]
    public void MarkdownFiles_DirectoryLinkLeadingOut_ListsNothingBehindIt()
    {
        Directory.CreateSymbolicLink(Path.Combine(_vault.Root, "Linked"), _outside);
        File.WriteAllText(Path.Combine(_vault.Root, "Note.md"), "note");

        var listed = Store().MarkdownFiles().ToList();

        Assert.Equal(["Note.md"], listed);
    }

    /// <summary>Breaks the listing on a loop: a link to the vault's own root inside it must not be walked forever.</summary>
    [Fact]
    public async Task MarkdownFiles_LinkLoopToTheRoot_FinishesWithEachNoteOnce()
    {
        Directory.CreateSymbolicLink(Path.Combine(_vault.Root, "Loop"), _vault.Root);
        File.WriteAllText(Path.Combine(_vault.Root, "Note.md"), "note");

        var listing = Task.Run(() => Store().MarkdownFiles().ToList());
        var listed = await listing.WaitAsync(TimeSpan.FromSeconds(20));

        Assert.Equal(["Note.md"], listed);
    }

    /// <summary>A link that stays inside the vault is part of it: writing through it lands where it leads.</summary>
    [Fact]
    public void TryWrite_ThroughDirectoryLinkStayingInside_WritesWhereItLeads()
    {
        var real = Directory.CreateDirectory(Path.Combine(_vault.Root, "Real")).FullName;
        Directory.CreateSymbolicLink(Path.Combine(_vault.Root, "Alias"), real);

        var isWritten = Store().TryWrite("Alias/Scheme.md", "inside", overwrite: false);

        Assert.True(isWritten);
        Assert.Equal("inside", File.ReadAllText(Path.Combine(real, "Scheme.md")));
    }

    /// <summary>A vault named through a link — on macOS /var itself is one — is still the vault.</summary>
    [Fact]
    public void TryWrite_RootNamedThroughALink_Writes()
    {
        var alias = Path.Combine(_vault.Parent, "VaultAlias");
        Directory.CreateSymbolicLink(alias, _vault.Root);
        var store = new FileSystemVaultStore(new VaultConfig { RootPath = alias });

        var isWritten = store.TryWrite("Excalidraw/Scheme.md", "inside", overwrite: false);

        Assert.True(isWritten);
        Assert.Equal("inside", store.Read("Excalidraw/Scheme.md"));
    }

    private FileSystemVaultStore Store() => new(new VaultConfig { RootPath = _vault.Root });
}
