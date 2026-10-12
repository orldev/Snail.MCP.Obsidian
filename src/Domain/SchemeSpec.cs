namespace Snail.MCP.Vault.Domain;

/// <summary>Declarative description of a scheme to draw: what the nodes say and how they connect.</summary>
public sealed record SchemeSpec
{
    public required IReadOnlyList<NodeSpec> Nodes { get; init; }

    public IReadOnlyList<EdgeSpec> Edges { get; set; } = [];
}
