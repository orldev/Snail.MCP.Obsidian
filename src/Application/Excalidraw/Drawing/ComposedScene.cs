using System.Text.Json.Nodes;

namespace Snail.MCP.Obsidian.Application.Excalidraw.Drawing;

/// <summary>A freshly composed scene together with the caption texts that go into the Text Elements section.</summary>
public sealed record ComposedScene
{
    /// <summary>The whole Excalidraw scene.</summary>
    public required JsonObject Scene { get; init; }

    /// <summary>The captions to list in the Text Elements section.</summary>
    public required IReadOnlyList<SchemeCaption> Captions { get; init; }

    /// <summary>Things worth telling the author about the drawing — overlapping blocks and the like.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
