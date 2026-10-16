namespace Snail.MCP.Obsidian.Domain;

/// <summary>An Excalidraw file split into byte-exact segments: everything outside the drawing payload is preserved verbatim.</summary>
public sealed record ExcalidrawDocument
{
    /// <summary>Everything before the drawing payload, up to and including its opening fence.</summary>
    public required string Prefix { get; init; }

    /// <summary>The drawing payload as stored, JSON or compressed.</summary>
    public required string Drawing { get; init; }

    /// <summary>Everything after the drawing payload, from its closing fence on.</summary>
    public required string Suffix { get; init; }

    /// <summary>How the payload is stored.</summary>
    public required DrawingFormat Format { get; init; }
}
