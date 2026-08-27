using System.Text.Json.Nodes;
using Snail.MCP.Vault.Application.Excalidraw;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Tests.Application.Schemes;

public class SceneDraftingTests
{
    private static readonly DrawSpec Spec = new()
    {
        Zones = [new DrawZone { X = 0, Y = 0, Width = 400, Height = 300, Title = "Клиент" }],
        Boxes =
        [
            new DrawBox { Key = "app", Text = "App", X = 40, Y = 60 },
            new DrawBox { Key = "api", Text = "API", X = 600, Y = 60, BackgroundColor = "#b2f2bb" },
        ],
        Texts = [new DrawText { Text = "Легенда: зелёный — готово", X = 0, Y = 400, FontSize = 16 }],
        Arrows = [new DrawArrow { From = "app", To = "api", Label = "HTTPS" }],
    };

    [Fact]
    public void DrawnSceneKeepsTheGivenPlacesAndReadsBack()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var (path, warnings) = catalog.Draw("Free", Spec);
        var graph = catalog.Read(path);

        Assert.Equal("Free.md", path);
        Assert.Empty(warnings);

        // blocks stay exactly where they were put
        var app = Assert.Single(graph.Nodes, node => node.Text == "App");
        Assert.Equal(40, app.X);
        Assert.Equal(60, app.Y);

        // the zone is a block of the drawing too, and the arrow binds both ends
        Assert.Equal(3, graph.Nodes.Count);
        var arrow = Assert.Single(graph.Edges);
        Assert.Equal("HTTPS", arrow.Label);
        Assert.Equal(app.Id, arrow.FromId);
        Assert.Equal(graph.Nodes.Single(node => node.Text == "API").Id, arrow.ToId);

        // the zone title and the legend live as free-standing texts
        Assert.Equal(["Клиент", "Легенда: зелёный — готово"], graph.Labels.Select(label => label.Text).Order());
    }

    [Fact]
    public void ArrowLeavesAndEntersThroughTheFacesFacingEachOther()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);

        var (path, _) = catalog.Draw("Sides", Spec);

        var scene = JsonNode.Parse(ExcalidrawCodec.DecodeDrawing(ExcalidrawCodec.Parse(files[path])))!;
        var arrow = Assert.Single(scene["elements"]!.AsArray(), element => element!["type"]!.GetValue<string>() == "arrow");

        // API sits to the right of App, so the arrow leaves the right face and enters the left one
        Assert.Equal(1, arrow!["startBinding"]!["fixedPoint"]![0]!.GetValue<double>());
        Assert.Equal(0, arrow["endBinding"]!["fixedPoint"]![0]!.GetValue<double>());
    }

    [Fact]
    public void BendsAreFollowedExactly()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);
        var withBends = Spec with
        {
            Arrows = [new DrawArrow { From = "app", To = "api", FromSide = "bottom", ToSide = "bottom", Bends = [[240, 500], [700, 500]] }],
        };

        var (path, _) = catalog.Draw("Bends", withBends);

        var scene = JsonNode.Parse(ExcalidrawCodec.DecodeDrawing(ExcalidrawCodec.Parse(files[path])))!;
        var arrow = Assert.Single(scene["elements"]!.AsArray(), element => element!["type"]!.GetValue<string>() == "arrow");
        var points = arrow!["points"]!.AsArray();
        var originX = arrow["x"]!.GetValue<double>();
        var originY = arrow["y"]!.GetValue<double>();

        Assert.Equal(4, points.Count);
        Assert.Equal(240, originX + points[1]![0]!.GetValue<double>());
        Assert.Equal(500, originY + points[1]![1]!.GetValue<double>());
        Assert.Equal(700, originX + points[2]![0]!.GetValue<double>());
    }

    [Fact]
    public void OverlappingBlocksAreReportedButStillDrawn()
    {
        Dictionary<string, string> files = [];
        var catalog = SchemeCatalogTests.Catalog(files);
        var overlapping = Spec with
        {
            Boxes = [.. Spec.Boxes, new DrawBox { Key = "clash", Text = "Clash", X = 60, Y = 80 }],
            Arrows = [],
        };

        var (path, warnings) = catalog.Draw("Clash", overlapping);

        Assert.Contains("app", Assert.Single(warnings));
        Assert.Equal(4, catalog.Read(path).Nodes.Count);
    }

    [Fact]
    public void RefusesArrowsToUnknownBoxes()
    {
        var catalog = SchemeCatalogTests.Catalog([]);
        var broken = Spec with { Arrows = [new DrawArrow { From = "app", To = "missing" }] };

        Assert.Throws<ArgumentException>(() => catalog.Draw("Broken", broken));
    }
}
