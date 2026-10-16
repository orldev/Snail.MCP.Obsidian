namespace Snail.MCP.Vault.Application.Excalidraw.Drawing;

/// <summary>One caption text paired with the element id that anchors it as an Obsidian block reference.</summary>
public sealed record SchemeCaption
{
    /// <summary>The caption text.</summary>
    public required string Text { get; init; }

    /// <summary>Id of the element the caption belongs to.</summary>
    public required string Id { get; init; }
}
