namespace Snail.MCP.Vault.Domain;

/// <summary>Logical view of an Excalidraw scene: shapes with their captions, connections between them, and free-standing labels.</summary>
public sealed record SchemeGraph
{
    public required IReadOnlyList<SchemeNode> Nodes { get; init; }

    public required IReadOnlyList<SchemeEdge> Edges { get; init; }

    public required IReadOnlyList<SchemeLabel> Labels { get; init; }
}
