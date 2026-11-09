using Snail.MCP.Obsidian.Adapters.FileSystem;
using Snail.MCP.Obsidian.Application;
using Snail.MCP.Obsidian.Application.Excalidraw.Drawing;
using Snail.MCP.Obsidian.Application.Ports;
using Snail.MCP.Obsidian.Application.Schemes;
using Snail.MCP.Obsidian.Configuration;
using Snail.MCP.Obsidian.Domain;

namespace Snail.MCP.Obsidian.Tests.Chaos;

public sealed class SchemeCatalogChaosTests : IDisposable
{
    private readonly ChaosVault _vault = new();

    public void Dispose() => _vault.Dispose();

    /// <summary>
    /// Breaks the listing on a live vault. Obsidian, a sync client or the user deletes a note between the enumeration
    /// and the read; List reads every file without a guard, so one vanished note fails scheme_list and vault_status
    /// for the whole vault.
    /// </summary>
    [Fact]
    public void List_FileVanishingDuringListing_ListsTheRest()
    {
        var catalog = new SchemeCatalog(new VanishingVault(), new SceneComposer(new Random(1)), new SceneDrafter(new Random(1)));

        var listed = Record.Exception(() => catalog.List());

        Assert.Null(listed);
    }

    /// <summary>
    /// Breaks the cost of the listing. List needs the first 512 characters of each note to find the plugin's marker,
    /// yet reads every note whole: a vault of large notes is read in full on every scheme_list and vault_status.
    /// </summary>
    [Fact]
    public void List_LargeNotes_ReadsOnlyTheirHeads()
    {
        var note = new string('x', 2 * 1024 * 1024);
        for (var index = 0; index < 10; index++)
            File.WriteAllText(Path.Combine(_vault.Root, $"Note {index}.md"), note);

        var catalog = Catalog();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var listed = catalog.List();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Empty(listed);
        Assert.True(allocated < 1024 * 1024, $"listing 10 notes of 2 MB allocated {allocated / (1024 * 1024)} MB");
    }

    /// <summary>
    /// Tries to break the shared composer: one SceneComposer and one SceneDrafter are singletons, and the MCP SDK may
    /// run tool calls side by side. Every scheme drawn in parallel must still read back whole.
    /// </summary>
    [Fact]
    public async Task Create_ThirtyTwoInParallelOnDistinctPaths_AllReadBack()
    {
        var catalog = Catalog();
        var spec = new SchemeSpec
        {
            Nodes = [new NodeSpec { Key = "a", Text = "A", Group = "One" }, new NodeSpec { Key = "b", Text = "B" }],
            Edges = [new EdgeSpec { From = "a", To = "b" }],
        };

        var created = await Task.WhenAll(Enumerable.Range(0, 32).Select(index => Task.Run(() => catalog.Create($"Parallel/{index}", spec))));
        var nodeCounts = created.Select(path => catalog.Read(path).Nodes.Count);

        Assert.All(nodeCounts, count => Assert.Equal(2, count));
    }

    /// <summary>
    /// Breaks measuring at the extremes. fontSize takes any int: text width multiplies two ints before it becomes a
    /// double, so int.MaxValue wraps to a negative width, and a negative size is drawn as given.
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(-20)]
    public void Compose_ExtremeFontSize_IsRefusedOrMeasuredNonNegative(int fontSize)
    {
        var spec = new SchemeSpec { Nodes = [new NodeSpec { Key = "a", Text = "Ab", FontSize = fontSize }] };

        ComposedScene? composed = null;

        var refusal = Record.Exception(() => composed = new SceneComposer(new Random(1)).Compose(spec));
        if (refusal is DrawingRefusal)
            return;

        var widths = composed!.Scene["elements"]!.AsArray().Select(element => element!["width"]!.GetValue<double>());

        Assert.Null(refusal);
        Assert.All(widths, width => Assert.True(width >= 0, $"an element is {width} wide"));
    }

    /// <summary>
    /// Breaks the element index. Fractional indices are written as "a" plus one digit, then "b" plus two, and nothing
    /// after: element 3907 indexes past the alphabet, so five hundred captioned boxes with two thousand labelled
    /// arrows — within any sensible limit — fail to draw.
    /// </summary>
    [Fact]
    public void Draft_FiveThousandElements_IsDrawn()
    {
        var boxes = Enumerable.Range(0, 500)
            .Select(index => new DrawBox { Key = $"b{index}", Text = $"{index}", X = index % 25 * 300, Y = index / 25 * 200 })
            .ToList();
        var arrows = Enumerable.Range(0, 2000)
            .Select(index => new DrawArrow { From = $"b{index % 500}", To = $"b{(index + 1) % 500}", Label = "next" })
            .ToList();

        var drawing = Record.Exception(() => new SceneDrafter(new Random(1)).Draft(new DrawSpec { Boxes = boxes, Arrows = arrows }));

        Assert.Null(drawing);
    }

    private SchemeCatalog Catalog() =>
        new(new FileSystemVaultStore(new VaultConfig { RootPath = _vault.Root }), new SceneComposer(new Random(1)), new SceneDrafter(new Random(1)));

    private sealed class VanishingVault : IVaultStore
    {
        public IEnumerable<string> MarkdownFiles() => ["A.md", "Gone.md", "B.md"];

        public string Read(string relativePath) =>
            relativePath == "Gone.md" ? throw new FileNotFoundException("Deleted by Obsidian a moment ago.", relativePath) : "# Note\n";

        public string? ReadHead(string relativePath) => relativePath == "Gone.md" ? null : "# Note\n";

        public bool Exists(string relativePath) => relativePath != "Gone.md";

        public bool TryWrite(string relativePath, string content, bool overwrite) => throw new NotSupportedException();
    }
}
