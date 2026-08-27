namespace Snail.MCP.Vault.Domain;

/// <summary>A shape on the scheme together with the caption bound into it.</summary>
public sealed record SchemeNode
{
    public required string Id { get; init; }

    public required string Shape { get; init; }

    public required string Text { get; init; }

    /// <summary>Name of the group this node was drawn into, when the scheme uses grouping.</summary>
    public string? Group { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}
