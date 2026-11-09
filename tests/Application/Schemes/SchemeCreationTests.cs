using System.Text.Json.Nodes;
using Snail.MCP.Obsidian.Application;
using Snail.MCP.Obsidian.Application.Excalidraw.Format;
using Snail.MCP.Obsidian.Domain;

namespace Snail.MCP.Obsidian.Tests.Application.Schemes;

public class SchemeCreationTests
{
    private static readonly SchemeSpec Spec = new()
    {
        Nodes =
        [
            new NodeSpec { Key = "front", Text = "Frontend" },
            new NodeSpec { Key = "api", Text = "API" },
            new NodeSpec { Key = "db", Text = "Database", Shape = "ellipse" },
        ],
        Edges =
        [
            new EdgeSpec { From = "front", To = "api", Label = "HTTP" },
            new EdgeSpec { From = "api", To = "db" },
        ],
    };

    [Fact]
    public void Create_Scheme_ReadsBackAsTheSameGraph()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var path = catalog.Create("Excalidraw/Demo", Spec);
        var graph = catalog.Read(path);

        Assert.Equal("Excalidraw/Demo.md", path);
        Assert.Equal(["Frontend", "API", "Database"], graph.Nodes.Select(n => n.Text));
        Assert.Equal("ellipse", graph.Nodes.Single(n => n.Text == "Database").Shape);

        var http = Assert.Single(graph.Edges, e => e.Label == "HTTP");
        Assert.Equal("Frontend", graph.Nodes.Single(n => n.Id == http.FromId).Text);
        Assert.Equal("API", graph.Nodes.Single(n => n.Id == http.ToId).Text);
    }

    [Fact]
    public void Create_Scheme_IsListedAndByteStable()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var path = catalog.Create("Demo", Spec);

        Assert.Equal([path], catalog.List());
        Assert.Equal(files[path], ExcalidrawCodec.Serialize(ExcalidrawCodec.Parse(files[path])));
    }

    [Fact]
    public void Create_Scheme_ListsCaptionsInTextElements()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var path = catalog.Create("Demo", Spec);

        Assert.Contains("## Text Elements\nFrontend ^", files[path]);
        Assert.Contains("HTTP ^", files[path]);
    }

    [Fact]
    public void Create_EdgesWithoutGroups_ArrangeNodesIntoOrderedColumns()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var graph = catalog.Read(catalog.Create("Demo", Spec));

        var byText = graph.Nodes.ToDictionary(n => n.Text);
        Assert.True(byText["Frontend"].X < byText["API"].X);
        Assert.True(byText["API"].X < byText["Database"].X);
    }

    [Fact]
    public void Create_ExistingPathWithoutOverwrite_IsRefused()
    {
        var catalog = SchemeCatalogTests.Catalog(new() { ["Demo.md"] = "existing" });

        Assert.Throws<VaultRefusal>(() => catalog.Create("Demo", Spec));
    }

    [Fact]
    public void Create_SuffixInCapitals_KeepsThePath()
    {
        var catalog = SchemeCatalogTests.Catalog([]);

        Assert.Equal("Excalidraw/Demo.MD", catalog.Create("Excalidraw/Demo.MD", Spec));
    }

    [Fact]
    public void Create_ExistingPathWithOverwrite_RedrawsInPlace()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);
        catalog.Create("Demo", Spec);

        var reworked = new SchemeSpec
        {
            Nodes = [new NodeSpec { Key = "solo", Text = "Reworked" }],
        };
        var path = catalog.Create("Demo", reworked, overwrite: true);

        Assert.Equal("Demo.md", path);
        Assert.Equal([path], files.Keys);
        Assert.Equal("Reworked", Assert.Single(catalog.Read(path).Nodes).Text);
    }

    /// <remarks>
    /// In the second column D follows its source A to the top, C follows B down — no crossing remains.
    /// </remarks>
    [Fact]
    public void Create_CrossingEdges_ReorderRowsWithinAColumn()
    {
        var crossing = new SchemeSpec
        {
            Nodes =
            [
                new NodeSpec { Key = "a", Text = "A" },
                new NodeSpec { Key = "b", Text = "B" },
                new NodeSpec { Key = "c", Text = "C" },
                new NodeSpec { Key = "d", Text = "D" },
            ],
            Edges =
            [
                new EdgeSpec { From = "a", To = "d" },
                new EdgeSpec { From = "b", To = "c" },
            ],
        };
        var catalog = SchemeCatalogTests.Catalog([]);

        var graph = catalog.Read(catalog.Create("Crossing", crossing));

        var byText = graph.Nodes.ToDictionary(n => n.Text);
        Assert.True(byText["A"].Y < byText["B"].Y);
        Assert.True(byText["D"].Y < byText["C"].Y);
    }

    /// <remarks>
    /// a→c bends around column B through a waypoint; c→a leaves as a square bracket under the diagram.
    /// </remarks>
    [Fact]
    public void Create_LongAndBackwardEdges_RouteAroundNodes()
    {
        var chained = new SchemeSpec
        {
            Nodes =
            [
                new NodeSpec { Key = "a", Text = "A" },
                new NodeSpec { Key = "b", Text = "B" },
                new NodeSpec { Key = "c", Text = "C" },
            ],
            Edges =
            [
                new EdgeSpec { From = "a", To = "b" },
                new EdgeSpec { From = "b", To = "c" },
                new EdgeSpec { From = "a", To = "c" },
                new EdgeSpec { From = "c", To = "a" },
            ],
        };
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var path = catalog.Create("Routes", chained);

        var scene = JsonNode.Parse(ExcalidrawCodec.DecodeDrawing(ExcalidrawCodec.Parse(files[path])))!;
        var arrows = scene["elements"]!.AsArray()
            .Where(element => element!["type"]!.GetValue<string>() == "arrow").ToList();

        Assert.Single(arrows, arrow => arrow!["points"]!.AsArray().Count == 3);

        var bracket = Assert.Single(arrows, arrow => arrow!["points"]!.AsArray().Count == 4);
        Assert.Equal(1, bracket!["startBinding"]!["fixedPoint"]![1]!.GetValue<double>());
        Assert.Equal(1, bracket["endBinding"]!["fixedPoint"]![1]!.GetValue<double>());

        var lane = bracket["y"]!.GetValue<double>() + bracket["points"]![1]![1]!.GetValue<double>();
        Assert.True(lane > catalog.Read(path).Nodes.Max(n => n.Y + n.Height), "the bracket must clear every node");
    }

    /// <remarks>
    /// Plates are drawn before the nodes they carry, so they stay underneath. Decorations stay out of the logical graph:
    /// only the four real nodes read back, each knowing its group. Group members stack vertically; the next group sits
    /// beside the previous one. DB follows API into the neighbouring cell — to its right, or on the row below.
    /// </remarks>
    [Fact]
    public void Create_GroupedNodes_LandInsideLabeledFrames()
    {
        var grouped = new SchemeSpec
        {
            Nodes =
            [
                new NodeSpec { Key = "app", Text = "App", Group = "Clients" },
                new NodeSpec { Key = "watch", Text = "Watch", Group = "Clients" },
                new NodeSpec { Key = "api", Text = "API", Group = "Backend" },
                new NodeSpec { Key = "db", Text = "DB" },
            ],
            Edges =
            [
                new EdgeSpec { From = "app", To = "api" },
                new EdgeSpec { From = "api", To = "db" },
            ],
        };
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var path = catalog.Create("Grouped", grouped);

        var scene = JsonNode.Parse(ExcalidrawCodec.DecodeDrawing(ExcalidrawCodec.Parse(files[path])))!;
        var elements = scene["elements"]!.AsArray();
        var marked = elements
            .Where(element => element!["customData"]?["snail"] is not null)
            .Select(element => (
                Role: element!["customData"]!["snail"]!["role"]!.GetValue<string>(),
                Group: element["customData"]!["snail"]!["group"]!.GetValue<string>(),
                Type: element["type"]!.GetValue<string>()))
            .ToList();

        Assert.Equal(["Backend", "Clients"], marked.Where(m => m.Role == "group").Select(m => m.Group).Order());
        Assert.Equal(["Backend", "Clients"], marked.Where(m => m.Role == "groupTitle").Select(m => m.Group).Order());
        Assert.All(marked.Where(m => m.Role == "group"), plate => Assert.Equal("rectangle", plate.Type));

        var types = elements.Select(element => element!["type"]!.GetValue<string>()).ToList();
        Assert.Equal(0, types.IndexOf("rectangle"));

        var graph = catalog.Read(path);
        Assert.Equal(4, graph.Nodes.Count);
        Assert.Empty(graph.Labels);

        var byText = graph.Nodes.ToDictionary(n => n.Text);
        Assert.Equal("Clients", byText["App"].Group);
        Assert.Equal("Backend", byText["API"].Group);
        Assert.Null(byText["DB"].Group);

        Assert.Equal(byText["App"].X, byText["Watch"].X, 3);
        Assert.True(byText["App"].Y < byText["Watch"].Y);
        Assert.True(byText["App"].X < byText["API"].X);

        Assert.True(byText["DB"].X > byText["API"].X || byText["DB"].Y > byText["API"].Y);
    }

    /// <remarks>
    /// Consecutive stages stay neighbours: each step is either sideways or one row down.
    /// </remarks>
    [Fact]
    public void Create_LongChainOfGroups_FoldsIntoAGrid()
    {
        var chain = Enumerable.Range(0, 8).ToList();
        var spec = new SchemeSpec
        {
            Nodes = [.. chain.Select(i => new NodeSpec { Key = $"n{i}", Text = $"Stage {i}", Group = $"G{i}" })],
            Edges = [.. chain.Skip(1).Select(i => new EdgeSpec { From = $"n{i - 1}", To = $"n{i}" })],
        };
        var catalog = SchemeCatalogTests.Catalog([]);

        var graph = catalog.Read(catalog.Create("Folded", spec));

        var byText = graph.Nodes.ToDictionary(n => n.Text);
        var width = graph.Nodes.Max(n => n.X + n.Width) - graph.Nodes.Min(n => n.X);
        var height = graph.Nodes.Max(n => n.Y + n.Height) - graph.Nodes.Min(n => n.Y);

        Assert.True(width / height < 3, $"a chain of 8 must not stay a flat ribbon, got {width:F0}x{height:F0}");
        Assert.True(byText["Stage 7"].Y > byText["Stage 0"].Y, "the flow has to wrap onto further rows");

        foreach (var i in chain.Skip(1))
        {
            var previous = byText[$"Stage {i - 1}"];
            var current = byText[$"Stage {i}"];
            var sideways = Math.Abs(current.Y - previous.Y) < 1 && Math.Abs(current.X - previous.X) < 900;
            var downwards = current.Y > previous.Y && Math.Abs(current.X - previous.X) < 200;

            Assert.True(sideways || downwards, $"Stage {i} drifted away from Stage {i - 1}");
        }
    }

    /// <remarks>
    /// a→c skips B inside the stack and a→far skips the middle column: both leave and enter their blocks sideways, each
    /// in a lane of its own, clear of every block.
    /// </remarks>
    [Fact]
    public void Create_EdgeAcrossBlocks_TakesItsOwnLane()
    {
        var spec = new SchemeSpec
        {
            Nodes =
            [
                new NodeSpec { Key = "a", Text = "A", Group = "Left" },
                new NodeSpec { Key = "b", Text = "B", Group = "Left" },
                new NodeSpec { Key = "c", Text = "C", Group = "Left" },
                new NodeSpec { Key = "mid", Text = "Mid", Group = "Middle" },
                new NodeSpec { Key = "far", Text = "Far", Group = "Right" },
            ],
            Edges =
            [
                new EdgeSpec { From = "a", To = "c" },
                new EdgeSpec { From = "a", To = "mid" },
                new EdgeSpec { From = "mid", To = "far" },
                new EdgeSpec { From = "a", To = "far" },
            ],
        };
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var path = catalog.Create("Lanes", spec);

        var scene = JsonNode.Parse(ExcalidrawCodec.DecodeDrawing(ExcalidrawCodec.Parse(files[path])))!;
        var brackets = scene["elements"]!.AsArray()
            .Where(element => element!["type"]!.GetValue<string>() == "arrow" && element["points"]!.AsArray().Count == 4)
            .ToList();
        Assert.Equal(2, brackets.Count);

        var graph = catalog.Read(path);

        var lanes = brackets
            .Select(arrow => arrow!["x"]!.GetValue<double>() + arrow["points"]![1]![0]!.GetValue<double>())
            .ToList();
        Assert.All(brackets, arrow => Assert.Equal(0, arrow!["startBinding"]!["fixedPoint"]![0]!.GetValue<double>()));
        Assert.All(lanes, lane => Assert.True(lane < graph.Nodes.Min(n => n.X), "a lane must sit outside the blocks"));
        Assert.Equal(2, lanes.Distinct().Count());
    }

    [Fact]
    public void Create_EdgeToUnknownNode_IsRefused()
    {
        var catalog = SchemeCatalogTests.Catalog([]);
        var broken = new SchemeSpec
        {
            Nodes = [new NodeSpec { Key = "a", Text = "A" }],
            Edges = [new EdgeSpec { From = "a", To = "missing" }],
        };

        Assert.Throws<DrawingRefusal>(() => catalog.Create("Broken", broken));
    }
}
