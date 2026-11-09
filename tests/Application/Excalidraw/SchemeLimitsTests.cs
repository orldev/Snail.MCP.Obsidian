using Snail.MCP.Obsidian.Application;
using Snail.MCP.Obsidian.Application.Excalidraw.Drawing;
using Snail.MCP.Obsidian.Domain;

namespace Snail.MCP.Obsidian.Tests.Application.Excalidraw;

public class SchemeLimitsTests
{
    [Fact]
    public void Compose_ChainAtTheNodeLimit_IsLaidOut()
    {
        var composed = new SceneComposer(new Random(1)).Compose(Chain(SchemeLimits.MostNodes));

        Assert.NotEmpty(composed.Scene["elements"]!.AsArray());
    }

    [Fact]
    public void Compose_OneNodeOverTheLimit_IsRefused()
    {
        var refusal = Assert.Throws<DrawingRefusal>(() => new SceneComposer(new Random(1)).Compose(Chain(SchemeLimits.MostNodes + 1)));

        Assert.Contains($"{SchemeLimits.MostNodes}", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Draft_OneBoxOverTheLimit_IsRefused()
    {
        var boxes = Enumerable.Range(0, SchemeLimits.MostNodes + 1)
            .Select(index => new DrawBox { Key = $"b{index}", Text = "B", X = index * 300, Y = 0 })
            .ToList();

        Assert.Throws<DrawingRefusal>(() => new SceneDrafter(new Random(1)).Draft(new DrawSpec { Boxes = boxes }));
    }

    /// <summary>Excalidraw stacks elements by their fractional index, so past "b" the indices must still sort in drawing order.</summary>
    [Fact]
    public void Draft_AtTheLimits_IndicesSortInDrawingOrder()
    {
        var boxes = Enumerable.Range(0, SchemeLimits.MostNodes)
            .Select(index => new DrawBox { Key = $"b{index}", Text = "B", X = index % 25 * 300, Y = index / 25 * 200 })
            .ToList();
        var arrows = Enumerable.Range(0, SchemeLimits.MostEdges)
            .Select(index => new DrawArrow { From = $"b{index % boxes.Count}", To = $"b{(index + 1) % boxes.Count}", Label = "next" })
            .ToList();

        var drafted = new SceneDrafter(new Random(1)).Draft(new DrawSpec { Boxes = boxes, Arrows = arrows });
        var indices = drafted.Scene["elements"]!.AsArray().Select(element => element!["index"]!.GetValue<string>()).ToList();

        Assert.Equal(indices.Order(StringComparer.Ordinal), indices);
        Assert.Equal(indices.Count, indices.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>Within the node and edge limits, edges spanning hundreds of columns each made 675 000 waypoints and 20 s of layout.</summary>
    [Fact]
    public void Compose_EdgesSkippingTooManyColumns_IsRefusedQuickly()
    {
        var chain = Chain(SchemeLimits.MostNodes);
        var longEdges = Enumerable.Range(0, 60).Select(index => new EdgeSpec { From = $"n{index}", To = $"n{SchemeLimits.MostNodes - 1 - index}" });
        var spec = new SchemeSpec { Nodes = chain.Nodes, Edges = [.. chain.Edges, .. longEdges] };

        var refusal = Assert.Throws<DrawingRefusal>(() => new SceneComposer(new Random(1)).Compose(spec));

        Assert.Contains("groups", refusal.Message, StringComparison.Ordinal);
    }

    private static SchemeSpec Chain(int length)
    {
        var nodes = Enumerable.Range(0, length).Select(index => new NodeSpec { Key = $"n{index}", Text = $"N{index}" }).ToList();
        var edges = Enumerable.Range(1, length - 1).Select(index => new EdgeSpec { From = $"n{index - 1}", To = $"n{index}" }).ToList();

        return new SchemeSpec { Nodes = nodes, Edges = edges };
    }
}
