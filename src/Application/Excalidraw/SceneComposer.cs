using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>Turns a scheme specification into a full Excalidraw scene: grid layout, styling, ids and bindings.</summary>
public sealed partial class SceneComposer(Random? random = null)
{
    private const string IdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    private const string IndexDigits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int DefaultFontSize = 20;
    private const double LineHeight = 1.25;
    private const int ColumnGap = 160;
    private const int RowGap = 48;

    private static readonly Dictionary<string, int> Fonts = new()
    {
        ["virgil"] = 1,
        ["helvetica"] = 2,
        ["cascadia"] = 3,
        ["excalifont"] = 5,
        ["nunito"] = 6,
        ["lilita"] = 7,
        ["comic"] = 8,
    };

    private static readonly string[] Shapes = ["rectangle", "diamond", "ellipse"];
    private static readonly string[] FillStyles = ["solid", "hachure", "cross-hatch", "zigzag"];
    private static readonly string[] StrokeStyles = ["solid", "dashed", "dotted"];
    private static readonly int[] StrokeWidths = [1, 2, 4];
    private static readonly string[] Arrowheads = ["none", "arrow", "triangle", "bar", "dot", "diamond"];

    private readonly Random _random = random ?? Random.Shared;

    public ComposedScene Compose(SchemeSpec spec)
    {
        Validate(spec);

        var frames = PlaceNodes(spec);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var elementIndex = 0;

        JsonArray elements = [];
        List<SchemeCaption> captions = [];
        Dictionary<string, JsonObject> shapesByKey = [];

        foreach (var node in spec.Nodes)
        {
            var frame = frames[node.Key];
            var shape = Shape(node, frame, NextIndex(ref elementIndex), timestamp);
            shapesByKey[node.Key] = shape;
            elements.Add(shape);

            var caption = Caption(node, shape, frame, NextIndex(ref elementIndex), timestamp);
            Bind(shape, caption, "text");
            captions.Add(new SchemeCaption { Text = node.Text, Id = Id(caption) });
            elements.Add(caption);
        }

        foreach (var edge in spec.Edges)
        {
            var arrow = Arrow(edge, shapesByKey[edge.From], shapesByKey[edge.To], frames[edge.From], frames[edge.To], NextIndex(ref elementIndex), timestamp);
            Bind(shapesByKey[edge.From], arrow, "arrow");
            Bind(shapesByKey[edge.To], arrow, "arrow");
            elements.Add(arrow);

            if (edge.Label.Length == 0)
                continue;

            var label = ArrowLabel(edge.Label, arrow, NextIndex(ref elementIndex), timestamp);
            Bind(arrow, label, "text");
            captions.Add(new SchemeCaption { Text = edge.Label, Id = Id(label) });
            elements.Add(label);
        }

        var scene = new JsonObject
        {
            ["type"] = "excalidraw",
            ["version"] = 2,
            ["source"] = "https://github.com/zsviczian/obsidian-excalidraw-plugin/releases/tag/2.26.4",
            ["elements"] = elements,
            ["appState"] = new JsonObject
            {
                ["theme"] = "light",
                ["viewBackgroundColor"] = "#ffffff",
                ["gridSize"] = 20,
                ["gridStep"] = 5,
                ["gridModeEnabled"] = false,
            },
            ["files"] = new JsonObject(),
        };

        return new ComposedScene { Scene = scene, Captions = captions };
    }

    private static void Validate(SchemeSpec spec)
    {
        if (spec.Nodes.Count == 0)
            throw new ArgumentException("A scheme needs at least one node.");

        HashSet<string> keys = [];
        foreach (var node in spec.Nodes)
        {
            if (!keys.Add(node.Key))
                throw new ArgumentException($"Node key '{node.Key}' is used twice.");

            Expect(Shapes, node.Shape, "shape");
            ExpectColor(node.StrokeColor, "strokeColor");
            ExpectColor(node.BackgroundColor, "backgroundColor");
            ExpectColor(node.TextColor, "textColor");
            Expect(FillStyles, node.FillStyle, "fillStyle");
            Expect(StrokeStyles, node.StrokeStyle, "strokeStyle");
            ExpectWidth(node.StrokeWidth);
            ExpectFont(node.Font);
        }

        foreach (var edge in spec.Edges)
        {
            if (!keys.Contains(edge.From))
                throw new ArgumentException($"Edge refers to unknown node key '{edge.From}'.");

            if (!keys.Contains(edge.To))
                throw new ArgumentException($"Edge refers to unknown node key '{edge.To}'.");

            ExpectColor(edge.Color, "color");
            Expect(StrokeStyles, edge.StrokeStyle, "strokeStyle");
            ExpectWidth(edge.StrokeWidth);
            Expect(Arrowheads, edge.StartArrowhead, "startArrowhead");
            Expect(Arrowheads, edge.EndArrowhead, "endArrowhead");
        }
    }

    private static void Expect(IEnumerable<string> allowed, string? value, string field)
    {
        if (value is not null && !allowed.Contains(value))
            throw new ArgumentException($"Unknown {field} '{value}'; allowed: {string.Join(", ", allowed)}.");
    }

    private static void ExpectColor(string? value, string field)
    {
        if (value is not null && value != "transparent" && !ColorPattern().IsMatch(value))
            throw new ArgumentException($"The {field} '{value}' is not a hex color like #1971c2 or 'transparent'.");
    }

    private static void ExpectWidth(int? value)
    {
        if (value is { } width && !StrokeWidths.Contains(width))
            throw new ArgumentException($"Unknown strokeWidth '{value}'; allowed: 1, 2, 4.");
    }

    private static void ExpectFont(string? value)
    {
        if (value is not null && !Fonts.ContainsKey(value))
            throw new ArgumentException($"Unknown font '{value}'; allowed: {string.Join(", ", Fonts.Keys)}.");
    }

    private static Dictionary<string, NodeFrame> PlaceNodes(SchemeSpec spec)
    {
        var layers = Layers(spec);
        var columns = spec.Nodes.GroupBy(node => layers[node.Key]).OrderBy(column => column.Key).ToList();
        Dictionary<string, NodeFrame> frames = [];

        double x = 0;
        foreach (var column in columns)
        {
            var sized = column.Select(node => (node.Key, Width: NodeWidth(node), Height: NodeHeight(node))).ToList();
            var columnWidth = sized.Max(node => node.Width);
            var columnHeight = sized.Sum(node => node.Height) + RowGap * (sized.Count - 1);

            var y = -columnHeight / 2;
            foreach (var node in sized)
            {
                frames[node.Key] = new NodeFrame(x + (columnWidth - node.Width) / 2, y, node.Width, node.Height);
                y += node.Height + RowGap;
            }

            x += columnWidth + ColumnGap;
        }

        return frames;
    }

    private static Dictionary<string, int> Layers(SchemeSpec spec)
    {
        var layers = spec.Nodes.ToDictionary(node => node.Key, _ => 0);

        for (var pass = 0; pass < spec.Nodes.Count; pass++)
        {
            var changed = false;
            foreach (var edge in spec.Edges)
            {
                if (layers[edge.To] < layers[edge.From] + 1)
                {
                    layers[edge.To] = layers[edge.From] + 1;
                    changed = true;
                }
            }

            if (!changed)
                break;
        }

        return layers;
    }

    private static double ShapePadding(string shape) =>
        shape switch { "diamond" => 1.6, "ellipse" => 1.3, _ => 1 };

    private static double NodeWidth(NodeSpec node)
    {
        var fontSize = node.FontSize ?? DefaultFontSize;
        var longestLine = node.Text.Split('\n').Max(line => line.Length);

        return Math.Max(160, longestLine * fontSize * 0.55 + 40) * ShapePadding(node.Shape);
    }

    private static double NodeHeight(NodeSpec node)
    {
        var fontSize = node.FontSize ?? DefaultFontSize;
        var lines = node.Text.Split('\n').Length;

        return Math.Max(70, lines * fontSize * LineHeight + 40) * ShapePadding(node.Shape);
    }

    private JsonObject Shape(NodeSpec node, NodeFrame frame, string index, long timestamp) =>
        new()
        {
            ["id"] = NewId(21),
            ["type"] = node.Shape,
            ["x"] = frame.X,
            ["y"] = frame.Y,
            ["width"] = frame.Width,
            ["height"] = frame.Height,
            ["angle"] = 0,
            ["strokeColor"] = node.StrokeColor ?? "#1e1e1e",
            ["backgroundColor"] = node.BackgroundColor ?? "transparent",
            ["fillStyle"] = node.FillStyle ?? "solid",
            ["strokeWidth"] = node.StrokeWidth ?? 2,
            ["strokeStyle"] = node.StrokeStyle ?? "solid",
            ["roughness"] = 1,
            ["opacity"] = 100,
            ["groupIds"] = new JsonArray(),
            ["frameId"] = null,
            ["index"] = index,
            ["roundness"] = node.Shape == "rectangle" ? new JsonObject { ["type"] = 3 } : null,
            ["seed"] = _random.Next(1, int.MaxValue),
            ["version"] = 1,
            ["versionNonce"] = _random.Next(1, int.MaxValue),
            ["isDeleted"] = false,
            ["boundElements"] = new JsonArray(),
            ["updated"] = timestamp,
            ["link"] = null,
            ["locked"] = false,
        };

    private JsonObject Caption(NodeSpec node, JsonObject container, NodeFrame frame, string index, long timestamp)
    {
        var fontSize = node.FontSize ?? DefaultFontSize;
        var width = TextWidth(node.Text, fontSize);
        var height = TextHeight(node.Text, fontSize);

        var caption = Text(node.Text, node.TextColor ?? "#1e1e1e", fontSize, Fonts[node.Font ?? "excalifont"], index, timestamp);
        caption["x"] = frame.X + (frame.Width - width) / 2;
        caption["y"] = frame.Y + (frame.Height - height) / 2;
        caption["width"] = width;
        caption["height"] = height;
        caption["containerId"] = Id(container);
        return caption;
    }

    private JsonObject ArrowLabel(string text, JsonObject arrow, string index, long timestamp)
    {
        var width = TextWidth(text, DefaultFontSize);
        var height = TextHeight(text, DefaultFontSize);
        var points = arrow["points"]!.AsArray();
        var lastPoint = points[^1]!.AsArray();

        var label = Text(text, "#1e1e1e", DefaultFontSize, Fonts["excalifont"], index, timestamp);
        label["x"] = arrow["x"]!.GetValue<double>() + lastPoint[0]!.GetValue<double>() / 2 - width / 2;
        label["y"] = arrow["y"]!.GetValue<double>() + lastPoint[1]!.GetValue<double>() / 2 - height / 2;
        label["width"] = width;
        label["height"] = height;
        label["containerId"] = Id(arrow);
        return label;
    }

    private JsonObject Text(string text, string color, int fontSize, int fontFamily, string index, long timestamp) =>
        new()
        {
            ["id"] = NewId(8),
            ["type"] = "text",
            ["x"] = 0,
            ["y"] = 0,
            ["width"] = 0,
            ["height"] = 0,
            ["angle"] = 0,
            ["strokeColor"] = color,
            ["backgroundColor"] = "transparent",
            ["fillStyle"] = "solid",
            ["strokeWidth"] = 2,
            ["strokeStyle"] = "solid",
            ["roughness"] = 1,
            ["opacity"] = 100,
            ["groupIds"] = new JsonArray(),
            ["frameId"] = null,
            ["index"] = index,
            ["roundness"] = null,
            ["seed"] = _random.Next(1, int.MaxValue),
            ["version"] = 1,
            ["versionNonce"] = _random.Next(1, int.MaxValue),
            ["isDeleted"] = false,
            ["boundElements"] = new JsonArray(),
            ["updated"] = timestamp,
            ["link"] = null,
            ["locked"] = false,
            ["text"] = text,
            ["rawText"] = text,
            ["fontSize"] = fontSize,
            ["fontFamily"] = fontFamily,
            ["textAlign"] = "center",
            ["verticalAlign"] = "middle",
            ["containerId"] = null,
            ["originalText"] = text,
            ["autoResize"] = true,
            ["lineHeight"] = LineHeight,
        };

    private JsonObject Arrow(EdgeSpec edge, JsonObject from, JsonObject to, NodeFrame fromFrame, NodeFrame toFrame, string index, long timestamp)
    {
        var startX = fromFrame.X + fromFrame.Width;
        var startY = fromFrame.Y + fromFrame.Height / 2;
        var endX = toFrame.X;
        var endY = toFrame.Y + toFrame.Height / 2;

        return new JsonObject
        {
            ["id"] = NewId(21),
            ["type"] = "arrow",
            ["x"] = startX,
            ["y"] = startY,
            ["width"] = Math.Abs(endX - startX),
            ["height"] = Math.Abs(endY - startY),
            ["angle"] = 0,
            ["strokeColor"] = edge.Color ?? "#1e1e1e",
            ["backgroundColor"] = "transparent",
            ["fillStyle"] = "solid",
            ["strokeWidth"] = edge.StrokeWidth ?? 2,
            ["strokeStyle"] = edge.StrokeStyle ?? "solid",
            ["roughness"] = 1,
            ["opacity"] = 100,
            ["groupIds"] = new JsonArray(),
            ["frameId"] = null,
            ["index"] = index,
            ["roundness"] = new JsonObject { ["type"] = 2 },
            ["seed"] = _random.Next(1, int.MaxValue),
            ["version"] = 1,
            ["versionNonce"] = _random.Next(1, int.MaxValue),
            ["isDeleted"] = false,
            ["boundElements"] = new JsonArray(),
            ["updated"] = timestamp,
            ["link"] = null,
            ["locked"] = false,
            ["points"] = new JsonArray(new JsonArray(0, 0), new JsonArray(endX - startX, endY - startY)),
            ["startBinding"] = Binding(Id(from), 1),
            ["endBinding"] = Binding(Id(to), 0),
            ["startArrowhead"] = Arrowhead(edge.StartArrowhead, null),
            ["endArrowhead"] = Arrowhead(edge.EndArrowhead, "arrow"),
            ["elbowed"] = false,
        };
    }

    private static JsonNode? Arrowhead(string? requested, string? fallback) =>
        requested switch
        {
            null => fallback,
            "none" => null,
            _ => requested,
        };

    private static JsonObject Binding(string elementId, double horizontalAnchor) =>
        new()
        {
            ["elementId"] = elementId,
            ["mode"] = "orbit",
            ["fixedPoint"] = new JsonArray(horizontalAnchor, 0.5001),
        };

    private static void Bind(JsonObject host, JsonObject bound, string type) =>
        host["boundElements"]!.AsArray().Add(new JsonObject { ["id"] = Id(bound), ["type"] = type });

    private static string Id(JsonObject element) => element["id"]!.GetValue<string>();

    private static double TextWidth(string text, int fontSize) =>
        text.Split('\n').Max(line => line.Length) * fontSize * 0.55;

    private static double TextHeight(string text, int fontSize) =>
        text.Split('\n').Length * fontSize * LineHeight;

    private string NewId(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(_ => IdAlphabet[_random.Next(IdAlphabet.Length)]));

    private static string NextIndex(ref int counter)
    {
        var position = counter++;
        if (position < IndexDigits.Length)
            return $"a{IndexDigits[position]}";

        var rest = position - IndexDigits.Length;
        return $"b{IndexDigits[rest / IndexDigits.Length]}{IndexDigits[rest % IndexDigits.Length]}";
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$")]
    private static partial Regex ColorPattern();

    private readonly record struct NodeFrame(double X, double Y, double Width, double Height);
}
