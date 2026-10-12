namespace Snail.MCP.Vault.Domain;

/// <summary>Logical view of an Excalidraw scene: shapes with their captions, connections between them, and free-standing labels.</summary>
public sealed record SchemeGraph
{
    /// <summary>Shapes with their captions.</summary>
    public required IReadOnlyList<SchemeNode> Nodes { get; init; }

    /// <summary>Arrows between the shapes.</summary>
    public required IReadOnlyList<SchemeEdge> Edges { get; init; }

    /// <summary>Texts bound to no shape or arrow.</summary>
    public required IReadOnlyList<SchemeLabel> Labels { get; init; }
}
