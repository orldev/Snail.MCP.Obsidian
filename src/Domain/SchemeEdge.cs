namespace Snail.MCP.Vault.Domain;

/// <summary>A directed connection drawn as an arrow; either endpoint may be unbound.</summary>
public sealed record SchemeEdge
{
    public required string Id { get; init; }

    public required string? FromId { get; init; }

    public required string? ToId { get; init; }

    public required string Label { get; init; }
}
