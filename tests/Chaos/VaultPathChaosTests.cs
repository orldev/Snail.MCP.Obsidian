using Snail.MCP.Obsidian.Adapters.FileSystem;
using Snail.MCP.Obsidian.Application;
using Snail.MCP.Obsidian.Application.Excalidraw.Drawing;
using Snail.MCP.Obsidian.Application.Schemes;
using Snail.MCP.Obsidian.Configuration;
using Snail.MCP.Obsidian.Domain;

namespace Snail.MCP.Obsidian.Tests.Chaos;

public sealed class VaultPathChaosTests : IDisposable
{
    private readonly ChaosVault _vault = new();

    public void Dispose() => _vault.Dispose();

    /// <summary>
    /// Breaks the vault boundary. Resolve checks that the full path starts with the root as a string, so a sibling
    /// folder whose name merely begins with the root's name — "Vault Backup" beside "Vault" — passes the check, and a
    /// tool that the README calls safe writes outside the vault.
    /// </summary>
    [Fact]
    public void Write_SiblingFolderSharingTheRootPrefix_IsRefused()
    {
        var sibling = Directory.CreateDirectory(Path.Combine(_vault.Parent, "Vault Backup")).FullName;
        var store = new FileSystemVaultStore(new VaultConfig { RootPath = _vault.Root });

        var refusal = Record.Exception(() => store.TryWrite("../Vault Backup/Stolen.md", "outside", overwrite: false));

        Assert.IsType<UnauthorizedAccessException>(refusal);
        Assert.False(File.Exists(Path.Combine(sibling, "Stolen.md")), "the file was written outside the vault");
    }

    /// <summary>
    /// Breaks "without overwrite an existing path is refused" at the file system. File.Move without overwrite checks
    /// and then renames, so writers racing for one new path could all pass: sixteen threads over 300 paths left two
    /// to five winners on 59 of them before the path was claimed by creating it.
    /// </summary>
    [Fact]
    public async Task TryWrite_SixteenWritersRacingForOnePath_ExactlyOneWins()
    {
        var store = new FileSystemVaultStore(new VaultConfig { RootPath = _vault.Root });
        List<int> winners = [];

        for (var round = 0; round < 200; round++)
        {
            var path = $"Raced/{round}.md";
            var writes = Enumerable.Range(0, 16).Select(_ => Task.Run(() => store.TryWrite(path, new string('x', 30_000), overwrite: false)));
            var outcomes = await Task.WhenAll(writes);

            winners.Add(outcomes.Count(isWritten => isWritten));
        }

        Assert.All(winners, count => Assert.Equal(1, count));
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(_vault.Root, "Raced"), "*.tmp", SearchOption.AllDirectories));
    }

    /// <summary>
    /// Breaks the promise that what was created can be found. A path ending in a slash becomes "folder/.md", a hidden
    /// file the listing skips as a service file: scheme_create answers with a path scheme_list never shows. Refusing
    /// the folder is as good as listing the file.
    /// </summary>
    [Fact]
    public void Create_PathEndingInSlash_IsRefusedOrListedAfterwards()
    {
        var catalog = new SchemeCatalog(
            new FileSystemVaultStore(new VaultConfig { RootPath = _vault.Root }),
            new SceneComposer(new Random(1)),
            new SceneDrafter(new Random(1)));
        var spec = new SchemeSpec { Nodes = [new NodeSpec { Key = "a", Text = "A" }] };
        string? path = null;

        var refusal = Record.Exception(() => path = catalog.Create("Excalidraw/", spec));
        if (refusal is VaultRefusal)
            return;

        Assert.Null(refusal);
        Assert.Contains(path!, catalog.List());
    }
}
