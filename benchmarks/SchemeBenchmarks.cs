using BenchmarkDotNet.Attributes;
using Snail.MCP.Vault.Application.Excalidraw.Drawing;
using Snail.MCP.Vault.Application.Schemes;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Benchmarks;

/// <summary>What scheme_create, scheme_draw and scheme_read do between the tool call and the file, on a vault in memory.</summary>
[MemoryDiagnoser]
public class SchemeBenchmarks
{
    private const string Created = "Excalidraw/Created.md";

    private SchemeCatalog _catalog = null!;

    private SchemeSpec _scheme = null!;

    private DrawSpec _drawing = null!;

    [GlobalSetup]
    public void Setup()
    {
        _catalog = new SchemeCatalog(new MemoryVault(), new SceneComposer(new Random(7)), new SceneDrafter(new Random(7)));

        string[] groups = ["Clients", "Edge", "Services", "Storage"];

        _scheme = new SchemeSpec
        {
            Nodes = [.. Enumerable.Range(0, 24).Select(index => new NodeSpec { Key = $"n{index}", Text = $"Component {index}\nwith a second line", Group = groups[index % groups.Length], Shape = index % 7 == 0 ? "ellipse" : "rectangle" })],
            Edges = [.. Enumerable.Range(1, 30).Select(index => new EdgeSpec { From = $"n{(index - 1) % 24}", To = $"n{(index * 5) % 24}", Label = index % 3 == 0 ? "calls" : "" })]
        };

        _drawing = new DrawSpec
        {
            Zones = [.. Enumerable.Range(0, 3).Select(index => new DrawZone { X = index * 700, Y = 0, Width = 640, Height = 1200, Title = $"Lane {index}" })],
            Boxes = [.. Enumerable.Range(0, 18).Select(index => new DrawBox { Key = $"b{index}", Text = $"Block {index}", X = index % 3 * 700 + 60, Y = index / 3 * 190 + 80 })],
            Texts = [new DrawText { Text = "Legend: blue is ours", X = 0, Y = 1260 }],
            Arrows = [.. Enumerable.Range(1, 17).Select(index => new DrawArrow { From = $"b{index - 1}", To = $"b{index}" })]
        };

        _catalog.Create(Created, _scheme);
    }

    /// <summary>Twenty-four nodes in four groups and thirty edges laid out, compressed and written.</summary>
    [Benchmark]
    public string CreateScheme() => _catalog.Create(Created, _scheme, overwrite: true);

    /// <summary>Eighteen blocks in three lanes with seventeen arrows rendered, compressed and written.</summary>
    [Benchmark]
    public int DrawScheme() => _catalog.Draw("Excalidraw/Drawn.md", _drawing, overwrite: true).Warnings.Count;

    /// <summary>The created scheme decompressed and projected into its graph.</summary>
    [Benchmark]
    public int ReadScheme() => _catalog.Read(Created).Nodes.Count;
}
