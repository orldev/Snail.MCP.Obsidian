namespace Snail.MCP.Vault.Domain;

/// <summary>A directed connection drawn as an arrow; either endpoint may be unbound.</summary>
public sealed record SchemeEdge
{
    /// <summary>Excalidraw id of the arrow.</summary>
    public required string Id { get; init; }

    /// <summary>Id of the shape the arrow starts at; <c>null</c> when that end is unbound.</summary>
    public required string? FromId { get; init; }

    /// <summary>Id of the shape the arrow ends at; <c>null</c> when that end is unbound.</summary>
    public required string? ToId { get; init; }

    /// <summary>Text bound to the arrow; empty when it has none.</summary>
    public required string Label { get; init; }
}
