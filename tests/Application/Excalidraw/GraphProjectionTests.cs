using System.Text.Json.Nodes;
using Snail.MCP.Vault.Application.Excalidraw.Reading;

namespace Snail.MCP.Vault.Tests.Application.Excalidraw;

public class GraphProjectionTests
{
    [Fact]
    public void Project_ShapeWithBoundCaption_IsANodeWithItsText()
    {
        var graph = GraphProjection.Project(Scene());

        var node = Assert.Single(graph.Nodes, n => n.Id == "rect1");
        Assert.Equal("Frontend", node.Text);
        Assert.Equal("rectangle", node.Shape);
        Assert.Equal(100, node.X);
    }

    [Fact]
    public void Project_ForeignElements_KeepsTheNodesAroundThem()
    {
        var scene = JsonNode.Parse("""
            {"elements":[
              {"id":"a","type":"rectangle","x":0,"y":0,"width":10,"height":10},
              42,
              {"id":"x","type":7},
              {"id":"y","type":"rectangle","isDeleted":"false","customData":"plain"},
              {"id":"z","type":"arrow","startBinding":"a"}
            ]}
            """)!;

        var graph = GraphProjection.Project(scene);

        Assert.Equal(["a", "y"], graph.Nodes.Select(node => node.Id));
        Assert.Null(Assert.Single(graph.Edges).FromId);
    }

    [Fact]
    public void Project_BoundArrow_IsADirectedEdge()
    {
        var graph = GraphProjection.Project(Scene());

        var edge = Assert.Single(graph.Edges);
        Assert.Equal("rect1", edge.FromId);
        Assert.Equal("rect2", edge.ToId);
        Assert.Equal("HTTP", edge.Label);
    }

    [Fact]
    public void Project_FreeText_IsALabel()
    {
        var graph = GraphProjection.Project(Scene());

        var label = Assert.Single(graph.Labels);
        Assert.Equal("Architecture overview", label.Text);
    }

    [Fact]
    public void Project_DeletedElement_IsLeftOut()
    {
        var graph = GraphProjection.Project(Scene());

        Assert.DoesNotContain(graph.Nodes, n => n.Id == "ghost");
    }

    [Fact]
    public void Project_BoundCaption_IsNotAlsoALabel()
    {
        var graph = GraphProjection.Project(Scene());

        Assert.DoesNotContain(graph.Labels, l => l.Text is "Frontend" or "API" or "HTTP");
    }

    private static JsonNode Scene() =>
        JsonNode.Parse("""
        {
            "type": "excalidraw",
            "version": 2,
            "elements": [
                {"id": "rect1", "type": "rectangle", "x": 100, "y": 50, "width": 200, "height": 80, "isDeleted": false,
                 "boundElements": [{"id": "cap1", "type": "text"}, {"id": "arrow1", "type": "arrow"}]},
                {"id": "cap1", "type": "text", "x": 110, "y": 60, "width": 80, "height": 20, "isDeleted": false,
                 "text": "Frontend", "originalText": "Frontend", "containerId": "rect1"},
                {"id": "rect2", "type": "rectangle", "x": 500, "y": 50, "width": 200, "height": 80, "isDeleted": false,
                 "boundElements": [{"id": "cap2", "type": "text"}]},
                {"id": "cap2", "type": "text", "x": 510, "y": 60, "width": 40, "height": 20, "isDeleted": false,
                 "text": "API", "originalText": "API", "containerId": "rect2"},
                {"id": "arrow1", "type": "arrow", "x": 300, "y": 90, "width": 200, "height": 0, "isDeleted": false,
                 "points": [[0, 0], [200, 0]],
                 "startBinding": {"elementId": "rect1", "focus": 0, "gap": 1},
                 "endBinding": {"elementId": "rect2", "focus": 0, "gap": 1},
                 "boundElements": [{"id": "cap3", "type": "text"}]},
                {"id": "cap3", "type": "text", "x": 380, "y": 70, "width": 50, "height": 20, "isDeleted": false,
                 "text": "HTTP", "originalText": "HTTP", "containerId": "arrow1"},
                {"id": "note1", "type": "text", "x": 0, "y": -100, "width": 300, "height": 30, "isDeleted": false,
                 "text": "Architecture overview", "originalText": "Architecture overview", "containerId": null},
                {"id": "ghost", "type": "rectangle", "x": 0, "y": 0, "width": 10, "height": 10, "isDeleted": true}
            ],
            "appState": {},
            "files": {}
        }
        """)!;
}
