using Snail.MCP.Vault.Application.Excalidraw;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Tests.Application.Schemes;

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
    public void CreatedSchemeReadsBackAsTheSameGraph()
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
    public void CreatedFileIsDiscoverableAndByteStable()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var path = catalog.Create("Demo", Spec);

        Assert.Equal([path], catalog.List());
        Assert.Equal(files[path], ExcalidrawCodec.Serialize(ExcalidrawCodec.Parse(files[path])));
    }

    [Fact]
    public void CaptionsLandInTextElementsSection()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var path = catalog.Create("Demo", Spec);

        Assert.Contains("## Text Elements\nFrontend ^", files[path]);
        Assert.Contains("HTTP ^", files[path]);
    }

    [Fact]
    public void EdgesArrangeNodesIntoOrderedColumns()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var graph = catalog.Read(catalog.Create("Demo", Spec));

        var byText = graph.Nodes.ToDictionary(n => n.Text);
        Assert.True(byText["Frontend"].X < byText["API"].X);
        Assert.True(byText["API"].X < byText["Database"].X);
    }

    [Fact]
    public void RefusesToOverwriteAnExistingFile()
    {
        var catalog = SchemeCatalogTests.Catalog(new() { ["Demo.md"] = "existing" });

        Assert.Throws<InvalidOperationException>(() => catalog.Create("Demo", Spec));
    }

    [Fact]
    public void RefusesEdgesToUnknownNodes()
    {
        var catalog = SchemeCatalogTests.Catalog([]);
        var broken = new SchemeSpec
        {
            Nodes = [new NodeSpec { Key = "a", Text = "A" }],
            Edges = [new EdgeSpec { From = "a", To = "missing" }],
        };

        Assert.Throws<ArgumentException>(() => catalog.Create("Broken", broken));
    }
}
