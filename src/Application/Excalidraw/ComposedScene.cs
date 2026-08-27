using System.Text.Json.Nodes;

namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>A freshly composed scene together with the caption texts that go into the Text Elements section.</summary>
public sealed record ComposedScene
{
    public required JsonObject Scene { get; init; }

    public required IReadOnlyList<SchemeCaption> Captions { get; init; }

    /// <summary>Things worth telling the author about the drawing — overlapping blocks and the like.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
