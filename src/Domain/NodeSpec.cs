using System.ComponentModel;

namespace Snail.MCP.Obsidian.Domain;

/// <summary>One node to draw; the key is how edges refer to it.</summary>
public sealed record NodeSpec : INodeStyle
{
    /// <summary>Identifier used by edges to refer to this node.</summary>
    [Description("Identifier used by edges to refer to this node.")]
    public required string Key { get; init; }

    /// <summary>Caption drawn inside the shape; use \n for multiple lines.</summary>
    [Description("Caption drawn inside the shape; use \\n for multiple lines.")]
    public required string Text { get; init; }

    /// <summary>rectangle (default), diamond or ellipse.</summary>
    [Description("rectangle (default), diamond or ellipse.")]
    public string Shape { get; set; } = "rectangle";

    /// <summary>Optional group name, e.g. 'Clients' or 'Backend'. Nodes sharing a group are stacked vertically inside a labeled frame; groups are laid out as columns by dependency flow. Grouping keeps big schemes compact and readable.</summary>
    [Description("Optional group name, e.g. 'Clients' or 'Backend'. Nodes sharing a group are stacked vertically inside a labeled frame; groups are laid out as columns by dependency flow. Grouping keeps big schemes compact and readable.")]
    public string? Group { get; init; }

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

    /// <summary>Caption font size in px, 8 to 120. Default 20.</summary>
    [Description("Caption font size in px, 8 to 120. Default 20.")]
    public int? FontSize { get; init; }

    /// <summary>Caption font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.</summary>
    [Description("Caption font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.")]
    public string? Font { get; init; }
}
