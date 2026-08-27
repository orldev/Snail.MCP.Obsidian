using System.Text.RegularExpressions;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>The values Excalidraw accepts, and the size a block needs for its caption.</summary>
internal static partial class StyleRules
{
    private static readonly string[] Shapes = ["rectangle", "diamond", "ellipse"];
    private static readonly string[] FillStyles = ["solid", "hachure", "cross-hatch", "zigzag"];
    private static readonly string[] StrokeStyles = ["solid", "dashed", "dotted"];
    private static readonly int[] StrokeWidths = [1, 2, 4];
    private static readonly string[] Arrowheads = ["none", "arrow", "triangle", "bar", "dot", "diamond"];

    public static void ValidateNode(INodeStyle style)
    {
        Expect(Shapes, style.Shape, "shape");
        ExpectColor(style.StrokeColor, "strokeColor");
        ExpectColor(style.BackgroundColor, "backgroundColor");
        ExpectColor(style.TextColor, "textColor");
        Expect(FillStyles, style.FillStyle, "fillStyle");
        Expect(StrokeStyles, style.StrokeStyle, "strokeStyle");
        ExpectWidth(style.StrokeWidth);
        ExpectFont(style.Font);
    }

    public static void ValidateEdge(IEdgeStyle style)
    {
        ExpectColor(style.Color, "color");
        Expect(StrokeStyles, style.StrokeStyle, "strokeStyle");
        ExpectWidth(style.StrokeWidth);
        Expect(Arrowheads, style.StartArrowhead, "startArrowhead");
        Expect(Arrowheads, style.EndArrowhead, "endArrowhead");
    }

    public static void ExpectColor(string? value, string field)
    {
        if (value is not null && value != "transparent" && !ColorPattern().IsMatch(value))
            throw new ArgumentException($"The {field} '{value}' is not a hex color like #1971c2 or 'transparent'.");
    }

    public static void ExpectStrokeStyle(string? value) => Expect(StrokeStyles, value, "strokeStyle");

    public static void ExpectFont(string? value)
    {
        if (value is not null && !ElementFactory.Fonts.ContainsKey(value))
            throw new ArgumentException($"Unknown font '{value}'; allowed: {string.Join(", ", ElementFactory.Fonts.Keys)}.");
    }

    /// <summary>How big a block has to be to hold its caption comfortably.</summary>
    public static (double Width, double Height) Measure(INodeStyle style, string text)
    {
        var fontSize = style.FontSize ?? ElementFactory.DefaultFontSize;
        var padding = style.Shape switch { "diamond" => 1.6, "ellipse" => 1.3, _ => 1 };
        var longestLine = text.Split('\n').Max(line => line.Length);
        var lines = text.Split('\n').Length;

        return (
            Math.Max(160, longestLine * fontSize * 0.55 + 40) * padding,
            Math.Max(70, lines * fontSize * ElementFactory.LineHeight + 40) * padding);
    }

    private static void Expect(IEnumerable<string> allowed, string? value, string field)
    {
        if (value is not null && !allowed.Contains(value))
            throw new ArgumentException($"Unknown {field} '{value}'; allowed: {string.Join(", ", allowed)}.");
    }

    private static void ExpectWidth(int? value)
    {
        if (value is { } width && !StrokeWidths.Contains(width))
            throw new ArgumentException($"Unknown strokeWidth '{value}'; allowed: 1, 2, 4.");
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$")]
    private static partial Regex ColorPattern();
}
