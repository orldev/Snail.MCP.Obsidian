namespace Snail.MCP.Vault.Domain;

/// <summary>A shape on the scheme together with the caption bound into it.</summary>
public sealed record SchemeNode
{
    /// <summary>Excalidraw id of the shape.</summary>
    public required string Id { get; init; }

    /// <summary>Excalidraw type of the shape: rectangle, diamond, ellipse or image.</summary>
    public required string Shape { get; init; }

    /// <summary>The caption bound into the shape; empty when it has none.</summary>
    public required string Text { get; init; }

    /// <summary>Name of the group this node was drawn into, when the scheme uses grouping.</summary>
    public string? Group { get; init; }

    /// <summary>Left edge in canvas units.</summary>
    public required double X { get; init; }

    /// <summary>Top edge in canvas units.</summary>
    public required double Y { get; init; }

    /// <summary>Width in canvas units.</summary>
    public required double Width { get; init; }

    /// <summary>Height in canvas units.</summary>
    public required double Height { get; init; }
}
