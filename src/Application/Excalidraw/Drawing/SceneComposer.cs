using System.Text.Json.Nodes;
using Snail.MCP.Obsidian.Application.Excalidraw.Format;
using Snail.MCP.Obsidian.Application.Excalidraw.Layout;
using Snail.MCP.Obsidian.Domain;

namespace Snail.MCP.Obsidian.Application.Excalidraw.Drawing;

/// <summary>Turns a scheme specification into a full Excalidraw scene: the layout it chooses, styling, ids and bindings.</summary>
public sealed class SceneComposer(Random? random = null)
{
    private const int GroupTitleSize = 20;

    private readonly Random _random = random ?? Random.Shared;

    /// <summary>Lays out, styles and binds every node and edge of a specification into a scene.</summary>
    /// <remarks>Group backdrops go in first: they must sit below the nodes they carry.</remarks>
    public ComposedScene Compose(SchemeSpec spec)
    {
        Validate(spec);

        var layout = Place(spec);
        var factory = new ElementFactory(_random);

        JsonArray elements = [];
        List<SchemeCaption> captions = [];
        Dictionary<string, JsonObject> shapesByKey = [];

        foreach (var container in layout.Containers)
            elements.Add((JsonNode)GroupPlate(factory, container));

        foreach (var container in layout.Containers)
        {
            var title = GroupTitle(factory, container);
            captions.Add(new SchemeCaption { Text = container.Title, Id = ElementFactory.Id(title) });
            elements.Add((JsonNode)title);
        }

        foreach (var node in spec.Nodes)
        {
            var frame = layout.Frames[node.Key];
            var shape = factory.Shape(node, frame);
            shapesByKey[node.Key] = shape;
            elements.Add((JsonNode)shape);

            var caption = factory.Caption(node, node.Text, shape, frame);
            ElementFactory.Bind(shape, caption, "text");
            captions.Add(new SchemeCaption { Text = node.Text, Id = ElementFactory.Id(caption) });
            elements.Add((JsonNode)caption);
        }

        for (var i = 0; i < spec.Edges.Count; i++)
        {
            var edge = spec.Edges[i];
            var arrow = factory.Arrow(edge, shapesByKey[edge.From], shapesByKey[edge.To], layout.Routes[i]);
            ElementFactory.Bind(shapesByKey[edge.From], arrow, "arrow");
            ElementFactory.Bind(shapesByKey[edge.To], arrow, "arrow");
            elements.Add((JsonNode)arrow);

            if (edge.Label.Length == 0)
                continue;

            var label = factory.ArrowLabel(edge.Label, arrow);
            ElementFactory.Bind(arrow, label, "text");
            captions.Add(new SchemeCaption { Text = edge.Label, Id = ElementFactory.Id(label) });
            elements.Add((JsonNode)label);
        }

        return new ComposedScene { Scene = ElementFactory.Scene(elements), Captions = captions };
    }

    private static void Validate(SchemeSpec spec)
    {
        if (spec.Nodes.Count == 0)
            throw new DrawingRefusal("A scheme needs at least one node.");

        if (spec.Nodes.Count > SchemeLimits.MostNodes || spec.Edges.Count > SchemeLimits.MostEdges)
            throw new DrawingRefusal($"A scheme takes at most {SchemeLimits.MostNodes} nodes and {SchemeLimits.MostEdges} edges; split it into several schemes.");

        HashSet<string> keys = [];
        foreach (var node in spec.Nodes)
        {
            if (!keys.Add(node.Key))
                throw new DrawingRefusal($"Node key '{node.Key}' is used twice.");

            StyleRules.ValidateNode(node);
        }

        foreach (var edge in spec.Edges)
        {
            if (!keys.Contains(edge.From))
                throw new DrawingRefusal($"Edge refers to unknown node key '{edge.From}'.");

            if (!keys.Contains(edge.To))
                throw new DrawingRefusal($"Edge refers to unknown node key '{edge.To}'.");

            StyleRules.ValidateEdge(edge);
        }
    }

    private static LayoutPlan Place(SchemeSpec spec) =>
        spec.Nodes.Any(node => GroupedLayout.GroupOf(node) is not null) ? GroupedLayout.Place(spec) : LayeredLayout.Place(spec);

    /// <summary>The backdrop a group sits on: a plain rounded rectangle drawn below its members.</summary>
    private static JsonObject GroupPlate(ElementFactory factory, GroupContainer container) =>
        factory.Plate(container.Rect, "#f8f9fa", "#ced4da", "solid", GroupMarker(SceneMarkers.GroupPlate, container.Title));

    private static JsonObject GroupTitle(ElementFactory factory, GroupContainer container) =>
        factory.Label(
            container.Title,
            container.Rect.X + GroupedLayout.FramePadding,
            container.Rect.Y + GroupedLayout.FramePadding / 2.0,
            GroupTitleSize,
            "#868e96",
            "excalifont",
            GroupMarker(SceneMarkers.GroupTitle, container.Title));

    private static JsonObject GroupMarker(string role, string group) =>
        new() { [SceneMarkers.Namespace] = new JsonObject { ["role"] = role, ["group"] = group } };
}
