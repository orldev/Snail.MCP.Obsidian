using System.ComponentModel;

namespace Snail.MCP.Obsidian.Domain;

/// <summary>One directed connection to draw between two node keys.</summary>
public sealed record EdgeSpec : IEdgeStyle
{
    /// <summary>Key of the source node.</summary>
    [Description("Key of the source node.")]
    public required string From { get; init; }

    /// <summary>Key of the target node.</summary>
    [Description("Key of the target node.")]
    public required string To { get; init; }

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
