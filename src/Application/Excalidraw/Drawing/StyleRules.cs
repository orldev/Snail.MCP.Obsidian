using System.Text.RegularExpressions;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw.Drawing;

/// <summary>The values Excalidraw accepts, and the size a block needs for its caption.</summary>
internal static partial class StyleRules
{
    public static void ValidateNode(INodeStyle style)
    {
        Expect(StyleVocabulary.Shapes, style.Shape, "shape");
        ExpectColor(style.StrokeColor, "strokeColor");
        ExpectColor(style.BackgroundColor, "backgroundColor");
        ExpectColor(style.TextColor, "textColor");
        Expect(StyleVocabulary.FillStyles, style.FillStyle, "fillStyle");
        Expect(StyleVocabulary.StrokeStyles, style.StrokeStyle, "strokeStyle");
        ExpectWidth(style.StrokeWidth);
        ExpectFont(style.Font);
        ExpectFontSize(style.FontSize, "fontSize");
    }

    public static void ExpectFontSize(int? value, string field)
    {
        if (value is { } size && size is < SchemeLimits.SmallestFontSize or > SchemeLimits.LargestFontSize)
            throw new ArgumentException($"The {field} {size} is outside {SchemeLimits.SmallestFontSize} to {SchemeLimits.LargestFontSize} px.");
    }

    public static void ValidateEdge(IEdgeStyle style)
    {
        ExpectColor(style.Color, "color");
        Expect(StyleVocabulary.StrokeStyles, style.StrokeStyle, "strokeStyle");
        ExpectWidth(style.StrokeWidth);
        Expect(StyleVocabulary.Arrowheads, style.StartArrowhead, "startArrowhead");
        Expect(StyleVocabulary.Arrowheads, style.EndArrowhead, "endArrowhead");
    }

    public static void ExpectColor(string? value, string field)
    {
        if (value is not null && value != "transparent" && !ColorPattern().IsMatch(value))
            throw new ArgumentException($"The {field} '{value}' is not a hex color like #1971c2 or 'transparent'.");
    }

    public static void ExpectStrokeStyle(string? value) => Expect(StyleVocabulary.StrokeStyles, value, "strokeStyle");

    public static void ExpectFont(string? value) => Expect(StyleVocabulary.Fonts, value, "font");

    public static void ExpectSide(string value, string field) => Expect(StyleVocabulary.Sides, value, field);

    /// <summary>How big a node has to be to hold its own caption comfortably.</summary>
    public static (double Width, double Height) Measure(NodeSpec node) => Measure(node, node.Text);

    /// <summary>How big a block has to be to hold its caption comfortably.</summary>
    public static (double Width, double Height) Measure(INodeStyle style, string text)
    {
        var fontSize = style.FontSize ?? ElementFactory.DefaultFontSize;
        var padding = style.Shape switch { "diamond" => 1.6, "ellipse" => 1.3, _ => 1 };
        var longestLine = (double)text.Split('\n').Max(line => line.Length);
        var lines = text.Split('\n').Length;

        return (
            Math.Max(160, longestLine * fontSize * 0.55 + 40) * padding,
            Math.Max(70, lines * fontSize * ElementFactory.LineHeight + 40) * padding);
    }

    private static void Expect(IReadOnlyList<string> allowed, string? value, string field)
    {
        if (value is not null && !allowed.Contains(value))
            throw new ArgumentException($"Unknown {field} '{value}'; allowed: {string.Join(", ", allowed)}.");
    }

    private static void ExpectWidth(int? value)
    {
        if (value is { } width && !StyleVocabulary.StrokeWidths.Contains(width))
            throw new ArgumentException($"Unknown strokeWidth '{value}'; allowed: {string.Join(", ", StyleVocabulary.StrokeWidths)}.");
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$")]
    private static partial Regex ColorPattern();
}
