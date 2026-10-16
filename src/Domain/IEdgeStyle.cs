namespace Snail.MCP.Obsidian.Domain;

/// <summary>Everything that decides how a connection looks, shared by laid-out schemes and free drawings.</summary>
public interface IEdgeStyle
{
    /// <summary>Text drawn on the arrow; empty for none.</summary>
    string Label { get; }

    /// <summary>Arrow color as hex; <c>null</c> for the default.</summary>
    string? Color { get; }

    /// <summary>Line style: solid, dashed or dotted; <c>null</c> for solid.</summary>
    string? StrokeStyle { get; }

    /// <summary>Line width: 1 thin, 2 default, 4 extra bold.</summary>
    int? StrokeWidth { get; }

    /// <summary>Head at the source end; <c>null</c> for none.</summary>
    string? StartArrowhead { get; }

    /// <summary>Head at the target end; <c>null</c> for an arrow.</summary>
    string? EndArrowhead { get; }
}
