namespace Snail.MCP.Vault.Domain;

/// <summary>Free-standing text that is not bound into any shape or arrow.</summary>
public sealed record SchemeLabel
{
    /// <summary>Excalidraw id of the text.</summary>
    public required string Id { get; init; }

    /// <summary>What the text says.</summary>
    public required string Text { get; init; }

    /// <summary>Left edge in canvas units.</summary>
    public required double X { get; init; }

    /// <summary>Top edge in canvas units.</summary>
    public required double Y { get; init; }
}
