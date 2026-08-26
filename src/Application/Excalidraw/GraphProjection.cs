using System.Text.Json.Nodes;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>Projects a raw Excalidraw scene into the logical graph of nodes, edges and labels.</summary>
public static class GraphProjection
{
    private static readonly string[] NodeShapes = ["rectangle", "diamond", "ellipse", "image"];

    public static SchemeGraph Project(JsonNode scene)
    {
        var elements = LivingElements(scene);
        var captions = CaptionsByContainer(elements);

        List<SchemeNode> nodes = [];
        List<SchemeEdge> edges = [];
        List<SchemeLabel> labels = [];

        foreach (var element in elements)
        {
            var type = element["type"]?.GetValue<string>() ?? "";
            if (NodeShapes.Contains(type))
                nodes.Add(ToNode(element, type, captions));
            else if (type is "arrow")
                edges.Add(ToEdge(element, captions));
            else if (type is "text" && Text(element["containerId"]) is null)
                labels.Add(ToLabel(element));
        }

        return new SchemeGraph { Nodes = nodes, Edges = edges, Labels = labels };
    }

    private static List<JsonNode> LivingElements(JsonNode scene)
    {
        List<JsonNode> elements = [];
        foreach (var element in scene["elements"]?.AsArray() ?? [])
        {
            if (element is not null && element["isDeleted"]?.GetValue<bool>() != true)
                elements.Add(element);
        }

        return elements;
    }

    private static Dictionary<string, string> CaptionsByContainer(List<JsonNode> elements)
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

    private static SchemeNode ToNode(JsonNode element, string shape, Dictionary<string, string> captions)
    {
        var id = Text(element["id"]) ?? "";

        return new SchemeNode
        {
            Id = id,
            Shape = shape,
            Text = captions.GetValueOrDefault(id, ""),
            X = Number(element["x"]),
            Y = Number(element["y"]),
            Width = Number(element["width"]),
            Height = Number(element["height"]),
        };
    }

    private static SchemeEdge ToEdge(JsonNode element, Dictionary<string, string> captions)
    {
        var id = Text(element["id"]) ?? "";

        return new SchemeEdge
        {
            Id = id,
            FromId = Text(element["startBinding"]?["elementId"]),
            ToId = Text(element["endBinding"]?["elementId"]),
            Label = captions.GetValueOrDefault(id, ""),
        };
    }

    private static SchemeLabel ToLabel(JsonNode element) =>
        new()
        {
            Id = Text(element["id"]) ?? "",
            Text = Text(element["originalText"]) ?? Text(element["text"]) ?? "",
            X = Number(element["x"]),
            Y = Number(element["y"]),
        };

    private static string? Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static double Number(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<double>(out var number) ? number : 0;
}
