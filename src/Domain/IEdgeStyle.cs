namespace Snail.MCP.Vault.Domain;

/// <summary>Everything that decides how a connection looks, shared by laid-out schemes and free drawings.</summary>
public interface IEdgeStyle
{
    string Label { get; }

    string? Color { get; }

    string? StrokeStyle { get; }

    int? StrokeWidth { get; }

    string? StartArrowhead { get; }

    string? EndArrowhead { get; }
}
