namespace Snail.MCP.Obsidian.Domain;

/// <summary>Declarative description of a scheme to draw: what the nodes say and how they connect.</summary>
public sealed record SchemeSpec
{
    /// <summary>The nodes to draw.</summary>
    public required IReadOnlyList<NodeSpec> Nodes { get; init; }

    /// <summary>The connections between node keys; none by default.</summary>
    public IReadOnlyList<EdgeSpec> Edges { get; set; } = [];
}
