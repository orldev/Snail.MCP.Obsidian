using System.ComponentModel;

namespace Snail.MCP.Vault.Domain;

/// <summary>A drawing composed by the caller: it decides the whole layout, the server only renders it.</summary>
public sealed record DrawSpec
{
    /// <summary>Background areas drawn first, under everything else — lanes, sections, highlighted regions.</summary>
    [Description("Background areas drawn first, under everything else — lanes, sections, highlighted regions.")]
    public IReadOnlyList<DrawZone> Zones { get; set; } = [];

    /// <summary>Blocks with captions.</summary>
    [Description("Blocks with captions.")]
    public IReadOnlyList<DrawBox> Boxes { get; set; } = [];

    /// <summary>Free-standing texts: titles, legends, annotations.</summary>
    [Description("Free-standing texts: titles, legends, annotations.")]
    public IReadOnlyList<DrawText> Texts { get; set; } = [];

    /// <summary>Connections between blocks.</summary>
    [Description("Connections between blocks.")]
    public IReadOnlyList<DrawArrow> Arrows { get; set; } = [];
}

/// <summary>A background area; its title is drawn in the top-left corner.</summary>
public sealed record DrawZone
{
    /// <summary>Left edge in canvas units.</summary>
    [Description("Left edge in canvas units.")]
    public required double X { get; init; }

    /// <summary>Top edge in canvas units.</summary>
    [Description("Top edge in canvas units.")]
    public required double Y { get; init; }

    /// <summary>Width in canvas units.</summary>
    public required double Width { get; init; }

    /// <summary>Height in canvas units.</summary>
    public required double Height { get; init; }

    /// <summary>Caption drawn inside the top-left corner. Empty for a bare area.</summary>
    [Description("Caption drawn inside the top-left corner. Empty for a bare area.")]
    public string Title { get; set; } = "";

    /// <summary>Fill color as hex, e.g. #f8f9fa, or 'transparent'. Default #f8f9fa.</summary>
    [Description("Fill color as hex, e.g. #f8f9fa, or 'transparent'. Default #f8f9fa.")]
    public string? BackgroundColor { get; init; }

    /// <summary>Outline color as hex. Default #ced4da.</summary>
    [Description("Outline color as hex. Default #ced4da.")]
    public string? StrokeColor { get; init; }

    /// <summary>Outline style: solid (default), dashed or dotted.</summary>
    [Description("Outline style: solid (default), dashed or dotted.")]
    public string? StrokeStyle { get; init; }

    /// <summary>Title color as hex. Default #868e96.</summary>
    [Description("Title color as hex. Default #868e96.")]
    public string? TitleColor { get; init; }

    /// <summary>Title size in px. Default 20.</summary>
    [Description("Title size in px. Default 20.")]
    public int? TitleSize { get; init; }
}

/// <summary>A block placed by the caller; the size is computed from the text unless given.</summary>
public sealed record DrawBox : INodeStyle
{
    /// <summary>Identifier used by arrows to refer to this block.</summary>
    [Description("Identifier used by arrows to refer to this block.")]
    public required string Key { get; init; }

    /// <summary>Caption drawn inside; use \n for multiple lines.</summary>
    [Description("Caption drawn inside; use \\n for multiple lines.")]
    public required string Text { get; init; }

    /// <summary>Left edge in canvas units.</summary>
    [Description("Left edge in canvas units.")]
    public required double X { get; init; }

    /// <summary>Top edge in canvas units.</summary>
    [Description("Top edge in canvas units.")]
    public required double Y { get; init; }

    /// <summary>Width in canvas units; computed from the text when omitted.</summary>
    [Description("Width in canvas units; computed from the text when omitted.")]
    public double? Width { get; init; }

    /// <summary>Height in canvas units; computed from the text when omitted.</summary>
    [Description("Height in canvas units; computed from the text when omitted.")]
    public double? Height { get; init; }

    /// <summary>rectangle (default), diamond or ellipse.</summary>
    [Description("rectangle (default), diamond or ellipse.")]
    public string Shape { get; set; } = "rectangle";

    /// <summary>Outline color as hex, e.g. #1971c2. Default #1e1e1e.</summary>
    [Description("Outline color as hex, e.g. #1971c2. Default #1e1e1e.")]
    public string? StrokeColor { get; init; }

    /// <summary>Fill color as hex, e.g. #a5d8ff. Default transparent.</summary>
    [Description("Fill color as hex, e.g. #a5d8ff. Default transparent.")]
    public string? BackgroundColor { get; init; }

    /// <summary>How the fill is painted: solid (default), hachure, cross-hatch or zigzag.</summary>
    [Description("How the fill is painted: solid (default), hachure, cross-hatch or zigzag.")]
    public string? FillStyle { get; init; }

    /// <summary>Outline style: solid (default), dashed or dotted.</summary>
    [Description("Outline style: solid (default), dashed or dotted.")]
    public string? StrokeStyle { get; init; }

    /// <summary>Outline width: 1 thin, 2 default, 4 extra bold.</summary>
    [Description("Outline width: 1 thin, 2 default, 4 extra bold.")]
    public int? StrokeWidth { get; init; }

    /// <summary>Caption color as hex. Default #1e1e1e.</summary>
    [Description("Caption color as hex. Default #1e1e1e.")]
    public string? TextColor { get; init; }

    /// <summary>Caption size in px. Default 20.</summary>
    [Description("Caption size in px. Default 20.")]
    public int? FontSize { get; init; }

    /// <summary>Caption font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.</summary>
    [Description("Caption font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.")]
    public string? Font { get; init; }
}

/// <summary>A free-standing text: a title, a legend entry, an annotation.</summary>
public sealed record DrawText
{
    /// <summary>The text to draw.</summary>
    public required string Text { get; init; }

    /// <summary>Left edge in canvas units.</summary>
    [Description("Left edge in canvas units.")]
    public required double X { get; init; }

    /// <summary>Top edge in canvas units.</summary>
    [Description("Top edge in canvas units.")]
    public required double Y { get; init; }

    /// <summary>Size in px. Default 20.</summary>
    [Description("Size in px. Default 20.")]
    public int? FontSize { get; init; }

    /// <summary>Color as hex. Default #1e1e1e.</summary>
    [Description("Color as hex. Default #1e1e1e.")]
    public string? Color { get; init; }

    /// <summary>Font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.</summary>
    [Description("Font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.")]
    public string? Font { get; init; }
}

/// <summary>A connection between two blocks, routed exactly as the caller asks.</summary>
public sealed record DrawArrow : IEdgeStyle
{
    /// <summary>Key of the source block.</summary>
    [Description("Key of the source block.")]
    public required string From { get; init; }

    /// <summary>Key of the target block.</summary>
    [Description("Key of the target block.")]
    public required string To { get; init; }

    /// <summary>Face of the source block the arrow leaves from: auto (default), left, right, top or bottom.</summary>
    [Description("Face of the source block the arrow leaves from: auto (default), left, right, top or bottom.")]
    public string FromSide { get; set; } = "auto";

    /// <summary>Face of the target block the arrow enters: auto (default), left, right, top or bottom.</summary>
    [Description("Face of the target block the arrow enters: auto (default), left, right, top or bottom.")]
    public string ToSide { get; set; } = "auto";

    /// <summary>Corner points the arrow passes through, as [x, y] pairs in canvas units. Empty for a straight line.</summary>
    [Description("Corner points the arrow passes through, as [x, y] pairs in canvas units. Empty for a straight line.")]
    public IReadOnlyList<IReadOnlyList<double>> Bends { get; set; } = [];

    /// <summary>Text drawn on the arrow. Empty for no label.</summary>
    [Description("Text drawn on the arrow. Empty for no label.")]
    public string Label { get; set; } = "";

    /// <summary>Arrow color as hex, e.g. #e03131. Default #1e1e1e.</summary>
    [Description("Arrow color as hex, e.g. #e03131. Default #1e1e1e.")]
    public string? Color { get; init; }

    /// <summary>Line style: solid (default), dashed or dotted.</summary>
    [Description("Line style: solid (default), dashed or dotted.")]
    public string? StrokeStyle { get; init; }

    /// <summary>Line width: 1 thin, 2 default, 4 extra bold.</summary>
    [Description("Line width: 1 thin, 2 default, 4 extra bold.")]
    public int? StrokeWidth { get; init; }

    /// <summary>Head at the source end: none (default), arrow, triangle, bar, dot or diamond.</summary>
    [Description("Head at the source end: none (default), arrow, triangle, bar, dot or diamond.")]
    public string? StartArrowhead { get; init; }

    /// <summary>Head at the target end: arrow (default), none, triangle, bar, dot or diamond.</summary>
    [Description("Head at the target end: arrow (default), none, triangle, bar, dot or diamond.")]
    public string? EndArrowhead { get; init; }
}
