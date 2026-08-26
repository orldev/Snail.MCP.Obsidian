namespace Snail.MCP.Vault.Domain;

/// <summary>Free-standing text that is not bound into any shape or arrow.</summary>
public sealed record SchemeLabel
{
    public required string Id { get; init; }

    public required string Text { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }
}
