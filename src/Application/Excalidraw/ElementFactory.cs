using System.Text.Json.Nodes;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>
/// Builds the raw Excalidraw elements — every field the plugin expects, ids, seeds and fractional indices
/// included — so that both the automatic layout and free drawings produce scenes the plugin accepts.
/// </summary>
internal sealed class ElementFactory(Random random)
{
    public const int DefaultFontSize = 20;
    public const double LineHeight = 1.25;

    private const string IdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    private const string IndexDigits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static readonly Dictionary<string, int> Fonts = new()
    {
        ["virgil"] = 1,
        ["helvetica"] = 2,
        ["cascadia"] = 3,
        ["excalifont"] = 5,
        ["nunito"] = 6,
        ["lilita"] = 7,
        ["comic"] = 8,
    };

    private readonly long _timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private int _index;

    public JsonObject Shape(INodeStyle style, Rect rect) =>
        new()
        {
            ["id"] = NewId(21),
            ["type"] = style.Shape,
            ["x"] = rect.X,
            ["y"] = rect.Y,
            ["width"] = rect.Width,
            ["height"] = rect.Height,
            ["angle"] = 0,
            ["strokeColor"] = style.StrokeColor ?? "#1e1e1e",
            ["backgroundColor"] = style.BackgroundColor ?? "transparent",
            ["fillStyle"] = style.FillStyle ?? "solid",
            ["strokeWidth"] = style.StrokeWidth ?? 2,
            ["strokeStyle"] = style.StrokeStyle ?? "solid",
            ["roughness"] = 1,
            ["opacity"] = 100,
            ["groupIds"] = new JsonArray(),
            ["frameId"] = null,
            ["index"] = NextIndex(),
            ["roundness"] = style.Shape == "rectangle" ? new JsonObject { ["type"] = 3 } : null,
            ["seed"] = random.Next(1, int.MaxValue),
            ["version"] = 1,
            ["versionNonce"] = random.Next(1, int.MaxValue),
            ["isDeleted"] = false,
            ["boundElements"] = new JsonArray(),
            ["updated"] = _timestamp,
            ["link"] = null,
            ["locked"] = false,
        };

    /// <summary>A plain backdrop rectangle: the ground a group or a lane sits on.</summary>
    public JsonObject Plate(Rect rect, string fill, string stroke, string strokeStyle, JsonObject? customData = null)
    {
        var plate = new JsonObject
        {
            ["id"] = NewId(21),
            ["type"] = "rectangle",
            ["x"] = rect.X,
            ["y"] = rect.Y,
            ["width"] = rect.Width,
            ["height"] = rect.Height,
            ["angle"] = 0,
            ["strokeColor"] = stroke,
            ["backgroundColor"] = fill,
            ["fillStyle"] = "solid",
            ["strokeWidth"] = 1,
            ["strokeStyle"] = strokeStyle,
            ["roughness"] = 1,
            ["opacity"] = 100,
            ["groupIds"] = new JsonArray(),
            ["frameId"] = null,
            ["index"] = NextIndex(),
            ["roundness"] = new JsonObject { ["type"] = 3 },
            ["seed"] = random.Next(1, int.MaxValue),
            ["version"] = 1,
            ["versionNonce"] = random.Next(1, int.MaxValue),
            ["isDeleted"] = false,
            ["boundElements"] = new JsonArray(),
            ["updated"] = _timestamp,
            ["link"] = null,
            ["locked"] = false,
        };

        if (customData is not null)
            plate["customData"] = customData;

        return plate;
    }

    /// <summary>The caption bound inside a shape, centred on it.</summary>
    public JsonObject Caption(INodeStyle style, string text, JsonObject container, Rect rect)
    {
        var fontSize = style.FontSize ?? DefaultFontSize;
        var caption = Text(text, style.TextColor ?? "#1e1e1e", fontSize, Fonts[style.Font ?? "excalifont"]);
        Place(caption, rect.X + (rect.Width - TextWidth(text, fontSize)) / 2, rect.Y + (rect.Height - TextHeight(text, fontSize)) / 2, fontSize);
        caption["containerId"] = Id(container);

        return caption;
    }

    /// <summary>A free-standing text, positioned by its top-left corner.</summary>
    public JsonObject Label(string text, double x, double y, int fontSize, string color, string font, JsonObject? customData = null)
    {
        var label = Text(text, color, fontSize, Fonts[font]);
        Place(label, x, y, fontSize);
        label["textAlign"] = "left";
        label["verticalAlign"] = "top";

        if (customData is not null)
            label["customData"] = customData;

        return label;
    }

    public JsonObject ArrowLabel(string text, JsonObject arrow)
    {
        var width = TextWidth(text, DefaultFontSize);
        var height = TextHeight(text, DefaultFontSize);
        var middle = PolylineMiddle(arrow["points"]!.AsArray());

        var label = Text(text, "#1e1e1e", DefaultFontSize, Fonts["excalifont"]);
        Place(label, arrow["x"]!.GetValue<double>() + middle.X - width / 2, arrow["y"]!.GetValue<double>() + middle.Y - height / 2, DefaultFontSize);
        label["text"] = text;
        label["containerId"] = Id(arrow);

        return label;
    }

    public JsonObject Arrow(IEdgeStyle style, JsonObject from, JsonObject to, EdgeRoute route)
    {
        var (startX, startY) = route.Start;
        var offsets = route.Bends.Append(route.End).Select(point => (X: point.X - startX, Y: point.Y - startY)).ToList();

        var points = new JsonArray(new JsonArray(0d, 0d));
        foreach (var offset in offsets)
            points.Add(new JsonArray(offset.X, offset.Y));

        return new JsonObject
        {
            ["id"] = NewId(21),
            ["type"] = "arrow",
            ["x"] = startX,
            ["y"] = startY,
            ["width"] = Math.Max(0, offsets.Max(point => point.X)) - Math.Min(0, offsets.Min(point => point.X)),
            ["height"] = Math.Max(0, offsets.Max(point => point.Y)) - Math.Min(0, offsets.Min(point => point.Y)),
            ["angle"] = 0,
            ["strokeColor"] = style.Color ?? "#1e1e1e",
            ["backgroundColor"] = "transparent",
            ["fillStyle"] = "solid",
            ["strokeWidth"] = style.StrokeWidth ?? 2,
            ["strokeStyle"] = style.StrokeStyle ?? "solid",
            ["roughness"] = 1,
            ["opacity"] = 100,
            ["groupIds"] = new JsonArray(),
            ["frameId"] = null,
            ["index"] = NextIndex(),
            ["roundness"] = new JsonObject { ["type"] = 2 },
            ["seed"] = random.Next(1, int.MaxValue),
            ["version"] = 1,
            ["versionNonce"] = random.Next(1, int.MaxValue),
            ["isDeleted"] = false,
            ["boundElements"] = new JsonArray(),
            ["updated"] = _timestamp,
            ["link"] = null,
            ["locked"] = false,
            ["points"] = points,
            ["startBinding"] = Binding(Id(from), route.StartAnchor),
            ["endBinding"] = Binding(Id(to), route.EndAnchor),
            ["startArrowhead"] = Arrowhead(style.StartArrowhead, null),
            ["endArrowhead"] = Arrowhead(style.EndArrowhead, "arrow"),
            ["elbowed"] = false,
        };
    }

    public static JsonObject Scene(JsonArray elements) =>
        new()
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

    public static void Bind(JsonObject host, JsonObject bound, string type) =>
        host["boundElements"]!.AsArray().Add(new JsonObject { ["id"] = Id(bound), ["type"] = type });

    public static string Id(JsonObject element) => element["id"]!.GetValue<string>();

    public static double TextWidth(string text, int fontSize) =>
        text.Split('\n').Max(line => line.Length) * fontSize * 0.55;

    public static double TextHeight(string text, int fontSize) =>
        text.Split('\n').Length * fontSize * LineHeight;

    private JsonObject Text(string text, string color, int fontSize, int fontFamily) =>
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
            ["index"] = NextIndex(),
            ["roundness"] = null,
            ["seed"] = random.Next(1, int.MaxValue),
            ["version"] = 1,
            ["versionNonce"] = random.Next(1, int.MaxValue),
            ["isDeleted"] = false,
            ["boundElements"] = new JsonArray(),
            ["updated"] = _timestamp,
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

    private static void Place(JsonObject text, double x, double y, int fontSize)
    {
        var body = text["originalText"]!.GetValue<string>();
        text["x"] = x;
        text["y"] = y;
        text["width"] = TextWidth(body, fontSize);
        text["height"] = TextHeight(body, fontSize);
    }

    private static JsonNode? Arrowhead(string? requested, string? fallback) =>
        requested switch
        {
            null => fallback,
            "none" => null,
            _ => requested,
        };

    private static JsonObject Binding(string elementId, (double X, double Y) anchor) =>
        new()
        {
            ["elementId"] = elementId,
            ["mode"] = "orbit",
            ["fixedPoint"] = new JsonArray(anchor.X, anchor.Y),
        };

    private static (double X, double Y) PolylineMiddle(JsonArray points)
    {
        var before = Point(points[(points.Count - 1) / 2]!);
        var after = Point(points[points.Count / 2]!);

        return ((before.X + after.X) / 2, (before.Y + after.Y) / 2);
    }

    private static (double X, double Y) Point(JsonNode node)
    {
        var pair = node.AsArray();

        return (pair[0]!.GetValue<double>(), pair[1]!.GetValue<double>());
    }

    private string NewId(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(_ => IdAlphabet[random.Next(IdAlphabet.Length)]));

    private string NextIndex()
    {
        var position = _index++;
        if (position < IndexDigits.Length)
            return $"a{IndexDigits[position]}";

        var rest = position - IndexDigits.Length;

        return $"b{IndexDigits[rest / IndexDigits.Length]}{IndexDigits[rest % IndexDigits.Length]}";
    }
}
