using System.ComponentModel;

namespace Snail.MCP.Vault.Domain;

/// <summary>One node to draw; the key is how edges refer to it.</summary>
public sealed record NodeSpec
{
    [Description("Identifier used by edges to refer to this node.")]
    public required string Key { get; init; }

    [Description("Caption drawn inside the shape; use \\n for multiple lines.")]
    public required string Text { get; init; }

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

    [Description("Caption font size in px. Default 20.")]
    public int? FontSize { get; init; }

    [Description("Caption font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.")]
    public string? Font { get; init; }
}
