using System.Text.Json.Nodes;
using Snail.MCP.Vault.Application.Excalidraw.Format;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw.Reading;

/// <summary>Projects a raw Excalidraw scene into the logical graph of nodes, edges and labels.</summary>
public static class GraphProjection
{
    private static readonly string[] NodeShapes = ["rectangle", "diamond", "ellipse", "image"];

    /// <summary>The nodes, edges and labels of a scene, leaving out deleted elements and the server's own decorations.</summary>
    public static SchemeGraph Project(JsonNode scene)
    {
        var elements = LivingElements(scene);
        var captions = CaptionsByContainer(elements);
        var plates = GroupPlates(elements);

        List<SchemeNode> nodes = [];
        List<SchemeEdge> edges = [];
        List<SchemeLabel> labels = [];

        foreach (var element in elements)
        {
            var type = Text(element["type"]) ?? "";
            if (Marker(element) is not null)
                continue;

            if (NodeShapes.Contains(type))
                nodes.Add(ToNode(element, type, captions, plates));
            else if (type is "arrow")
                edges.Add(ToEdge(element, captions));
            else if (type is "text" && Text(element["containerId"]) is null)
                labels.Add(ToLabel(element));
        }

        return new SchemeGraph { Nodes = nodes, Edges = edges, Labels = labels };
    }

    /// <summary>Group backdrops this server drew, as (name, rectangle) — the source of each node's group.</summary>
    private static List<(string Name, double X, double Y, double Width, double Height)> GroupPlates(List<JsonObject> elements)
    {
        List<(string, double, double, double, double)> plates = [];
        foreach (var element in elements)
        {
            if (Marker(element) is not JsonObject marker || Text(marker["role"]) != SceneMarkers.GroupPlate)
                continue;

            plates.Add((Text(marker["group"]) ?? "", Number(element["x"]), Number(element["y"]),
                Number(element["width"]), Number(element["height"])));
        }

        return plates;
    }

    private static JsonNode? Marker(JsonObject element) => Field(element["customData"], SceneMarkers.Namespace);

    /// <summary>The elements not deleted.</summary>
    /// <remarks>
    /// Every field is read by its kind and an element that is not an object is skipped: one element another tool wrote
    /// with a field of an unexpected kind made the whole scheme unreadable.
    /// </remarks>
    private static List<JsonObject> LivingElements(JsonNode scene)
    {
        List<JsonObject> elements = [];
        if (Field(scene, "elements") is not JsonArray all)
            return elements;

        foreach (var element in all)
        {
            if (element is JsonObject living && Flag(living["isDeleted"]) != true)
                elements.Add(living);
        }

        return elements;
    }

    private static Dictionary<string, string> CaptionsByContainer(List<JsonObject> elements)
    {
        Dictionary<string, string> captions = [];
        foreach (var element in elements)
        {
            if (Text(element["type"]) != "text" || Text(element["containerId"]) is not { } container)
                continue;

            var caption = Text(element["originalText"]) ?? Text(element["text"]) ?? "";
            captions[container] = captions.TryGetValue(container, out var existing) ? $"{existing}\n{caption}" : caption;
        }

        return captions;
    }

    private static SchemeNode ToNode(
        JsonObject element,
        string shape,
        Dictionary<string, string> captions,
        List<(string Name, double X, double Y, double Width, double Height)> plates)
    {
        var id = Text(element["id"]) ?? "";
        var centreX = Number(element["x"]) + Number(element["width"]) / 2;
        var centreY = Number(element["y"]) + Number(element["height"]) / 2;

        return new SchemeNode
        {
            Id = id,
            Shape = shape,
            Text = captions.GetValueOrDefault(id, ""),
            Group = plates.FirstOrDefault(plate =>
                centreX >= plate.X && centreX <= plate.X + plate.Width &&
                centreY >= plate.Y && centreY <= plate.Y + plate.Height).Name,
            X = Number(element["x"]),
            Y = Number(element["y"]),
            Width = Number(element["width"]),
            Height = Number(element["height"]),
        };
    }

    private static SchemeEdge ToEdge(JsonObject element, Dictionary<string, string> captions)
    {
        var id = Text(element["id"]) ?? "";

        return new SchemeEdge
        {
            Id = id,
            FromId = Text(Field(element["startBinding"], "elementId")),
            ToId = Text(Field(element["endBinding"], "elementId")),
            Label = captions.GetValueOrDefault(id, ""),
        };
    }

    private static SchemeLabel ToLabel(JsonObject element) =>
        new()
        {
            Id = Text(element["id"]) ?? "",
            Text = Text(element["originalText"]) ?? Text(element["text"]) ?? "",
            X = Number(element["x"]),
            Y = Number(element["y"]),
        };

    private static JsonNode? Field(JsonNode? node, string name) =>
        node is JsonObject owner ? owner[name] : null;

    private static bool? Flag(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<bool>(out var flag) ? flag : null;

    private static string? Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static double Number(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<double>(out var number) ? number : 0;
}
