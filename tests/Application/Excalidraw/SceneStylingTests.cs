using System.Text.Json.Nodes;
using Snail.MCP.Obsidian.Application;
using Snail.MCP.Obsidian.Application.Excalidraw.Drawing;
using Snail.MCP.Obsidian.Domain;

namespace Snail.MCP.Obsidian.Tests.Application.Excalidraw;

public class SceneStylingTests
{
    [Fact]
    public void Compose_StyledNode_StylesShapeAndCaption()
    {
        var scene = Compose(new SchemeSpec
        {
            Nodes =
            [
                new NodeSpec
                {
                    Key = "a",
                    Text = "Styled",
                    Shape = "diamond",
                    StrokeColor = "#e03131",
                    BackgroundColor = "#ffc9c9",
                    FillStyle = "cross-hatch",
                    StrokeStyle = "dashed",
                    StrokeWidth = 4,
                    TextColor = "#1971c2",
                    FontSize = 28,
                    Font = "comic",
                },
            ],
        });

        var shape = Element(scene, "diamond");
        Assert.Equal("#e03131", (string?)shape["strokeColor"]);
        Assert.Equal("#ffc9c9", (string?)shape["backgroundColor"]);
        Assert.Equal("cross-hatch", (string?)shape["fillStyle"]);
        Assert.Equal("dashed", (string?)shape["strokeStyle"]);
        Assert.Equal(4, (int?)shape["strokeWidth"]);
        Assert.Null(shape["roundness"]);

        var caption = Element(scene, "text");
        Assert.Equal("#1971c2", (string?)caption["strokeColor"]);
        Assert.Equal(28, (int?)caption["fontSize"]);
        Assert.Equal(8, (int?)caption["fontFamily"]);
    }

    [Fact]
    public void Compose_StyledEdge_StylesArrowAndHeads()
    {
        var scene = Compose(new SchemeSpec
        {
            Nodes = [new NodeSpec { Key = "a", Text = "A" }, new NodeSpec { Key = "b", Text = "B" }],
            Edges =
            [
                new EdgeSpec
                {
                    From = "a",
                    To = "b",
                    Color = "#2f9e44",
                    StrokeStyle = "dotted",
                    StartArrowhead = "dot",
                    EndArrowhead = "triangle",
                },
            ],
        });

        var arrow = Element(scene, "arrow");
        Assert.Equal("#2f9e44", (string?)arrow["strokeColor"]);
        Assert.Equal("dotted", (string?)arrow["strokeStyle"]);
        Assert.Equal("dot", (string?)arrow["startArrowhead"]);
        Assert.Equal("triangle", (string?)arrow["endArrowhead"]);
    }

    [Fact]
    public void Compose_ArrowheadNone_IsWrittenAsNull()
    {
        var scene = Compose(new SchemeSpec
        {
            Nodes = [new NodeSpec { Key = "a", Text = "A" }, new NodeSpec { Key = "b", Text = "B" }],
            Edges = [new EdgeSpec { From = "a", To = "b", EndArrowhead = "none" }],
        });

        Assert.Null(Element(scene, "arrow")["endArrowhead"]);
    }

    [Theory]
    [InlineData("shape", "star")]
    [InlineData("fillStyle", "plaid")]
    [InlineData("strokeStyle", "wavy")]
    [InlineData("font", "arial")]
    [InlineData("strokeColor", "red")]
    public void Compose_UnknownStyleValue_IsRefused(string field, string value)
    {
        var node = new NodeSpec
        {
            Key = "a",
            Text = "A",
            Shape = field == "shape" ? value : "rectangle",
            FillStyle = field == "fillStyle" ? value : null,
            StrokeStyle = field == "strokeStyle" ? value : null,
            Font = field == "font" ? value : null,
            StrokeColor = field == "strokeColor" ? value : null,
        };

        Assert.Throws<DrawingRefusal>(() => Compose(new SchemeSpec { Nodes = [node] }));
    }

    private static JsonObject Compose(SchemeSpec spec) =>
        (JsonObject)new SceneComposer(new Random(7)).Compose(spec).Scene;

    private static JsonObject Element(JsonObject scene, string type) =>
        scene["elements"]!.AsArray().Cast<JsonObject>().First(e => (string?)e["type"] == type);
}
