using System.ComponentModel;

namespace Snail.MCP.Vault.Domain;

/// <summary>A drawing composed by the caller: it decides the whole layout, the server only renders it.</summary>
public sealed record DrawSpec
{
    [Description("Background areas drawn first, under everything else — lanes, sections, highlighted regions.")]
    public IReadOnlyList<DrawZone> Zones { get; init; } = [];

    [Description("Blocks with captions.")]
    public IReadOnlyList<DrawBox> Boxes { get; init; } = [];

    [Description("Free-standing texts: titles, legends, annotations.")]
    public IReadOnlyList<DrawText> Texts { get; init; } = [];

    [Description("Connections between blocks.")]
    public IReadOnlyList<DrawArrow> Arrows { get; init; } = [];
}

/// <summary>A background area; its title is drawn in the top-left corner.</summary>
public sealed record DrawZone
{
    [Description("Left edge in canvas units.")]
    public required double X { get; init; }

    [Description("Top edge in canvas units.")]
    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    [Description("Caption drawn inside the top-left corner. Empty for a bare area.")]
    public string Title { get; init; } = "";

    [Description("Fill color as hex, e.g. #f8f9fa, or 'transparent'. Default #f8f9fa.")]
    public string? BackgroundColor { get; init; }

    [Description("Outline color as hex. Default #ced4da.")]
    public string? StrokeColor { get; init; }

    [Description("Outline style: solid (default), dashed or dotted.")]
    public string? StrokeStyle { get; init; }

    [Description("Title color as hex. Default #868e96.")]
    public string? TitleColor { get; init; }

    [Description("Title size in px. Default 20.")]
    public int? TitleSize { get; init; }
}

/// <summary>A block placed by the caller; the size is computed from the text unless given.</summary>
public sealed record DrawBox : INodeStyle
{
    [Description("Identifier used by arrows to refer to this block.")]
    public required string Key { get; init; }

    [Description("Caption drawn inside; use \\n for multiple lines.")]
    public required string Text { get; init; }

    [Description("Left edge in canvas units.")]
    public required double X { get; init; }

    [Description("Top edge in canvas units.")]
    public required double Y { get; init; }

    [Description("Width in canvas units; computed from the text when omitted.")]
    public double? Width { get; init; }

    [Description("Height in canvas units; computed from the text when omitted.")]
    public double? Height { get; init; }

    [Description("rectangle (default), diamond or ellipse.")]
    public string Shape { get; init; } = "rectangle";

    [Description("Outline color as hex, e.g. #1971c2. Default #1e1e1e.")]
    public string? StrokeColor { get; init; }

    [Description("Fill color as hex, e.g. #a5d8ff. Default transparent.")]
    public string? BackgroundColor { get; init; }

    [Description("How the fill is painted: solid (default), hachure, cross-hatch or zigzag.")]
    public string? FillStyle { get; init; }

    [Description("Outline style: solid (default), dashed or dotted.")]
    public string? StrokeStyle { get; init; }

    [Description("Outline width: 1 thin, 2 default, 4 extra bold.")]
    public int? StrokeWidth { get; init; }

    [Description("Caption color as hex. Default #1e1e1e.")]
    public string? TextColor { get; init; }

    [Description("Caption size in px. Default 20.")]
    public int? FontSize { get; init; }

    [Description("Caption font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.")]
    public string? Font { get; init; }
}

/// <summary>A free-standing text: a title, a legend entry, an annotation.</summary>
public sealed record DrawText
{
    public required string Text { get; init; }

    [Description("Left edge in canvas units.")]
    public required double X { get; init; }

    [Description("Top edge in canvas units.")]
    public required double Y { get; init; }

    [Description("Size in px. Default 20.")]
    public int? FontSize { get; init; }

    [Description("Color as hex. Default #1e1e1e.")]
    public string? Color { get; init; }

    [Description("Font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.")]
    public string? Font { get; init; }
}

/// <summary>A connection between two blocks, routed exactly as the caller asks.</summary>
public sealed record DrawArrow : IEdgeStyle
{
    [Description("Key of the source block.")]
    public required string From { get; init; }

    [Description("Key of the target block.")]
    public required string To { get; init; }

    [Description("Face of the source block the arrow leaves from: auto (default), left, right, top or bottom.")]
    public string FromSide { get; init; } = "auto";

    [Description("Face of the target block the arrow enters: auto (default), left, right, top or bottom.")]
    public string ToSide { get; init; } = "auto";

    [Description("Corner points the arrow passes through, as [x, y] pairs in canvas units. Empty for a straight line.")]
    public IReadOnlyList<IReadOnlyList<double>> Bends { get; init; } = [];

    [Description("Text drawn on the arrow. Empty for no label.")]
    public string Label { get; init; } = "";

    [Description("Arrow color as hex, e.g. #e03131. Default #1e1e1e.")]
    public string? Color { get; init; }

    [Description("Line style: solid (default), dashed or dotted.")]
    public string? StrokeStyle { get; init; }

    [Description("Line width: 1 thin, 2 default, 4 extra bold.")]
    public int? StrokeWidth { get; init; }

    [Description("Head at the source end: none (default), arrow, triangle, bar, dot or diamond.")]
    public string? StartArrowhead { get; init; }

    [Description("Head at the target end: arrow (default), none, triangle, bar, dot or diamond.")]
    public string? EndArrowhead { get; init; }
}
