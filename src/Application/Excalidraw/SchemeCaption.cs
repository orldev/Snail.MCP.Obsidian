namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>One caption text paired with the element id that anchors it as an Obsidian block reference.</summary>
public sealed record SchemeCaption
{
    public required string Text { get; init; }

    public required string Id { get; init; }
}
