namespace Snail.MCP.Vault.Domain;

/// <summary>An Excalidraw file split into byte-exact segments: everything outside the drawing payload is preserved verbatim.</summary>
public sealed record ExcalidrawDocument
{
    public required string Prefix { get; init; }

    public required string Drawing { get; init; }

    public required string Suffix { get; init; }

    public required DrawingFormat Format { get; init; }
}
