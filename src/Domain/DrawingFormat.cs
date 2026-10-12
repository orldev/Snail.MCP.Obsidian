namespace Snail.MCP.Vault.Domain;

/// <summary>Storage format of the drawing block inside an Excalidraw markdown file.</summary>
public enum DrawingFormat
{
    /// <summary>Plain JSON in a <c>json</c> block.</summary>
    Json,
    /// <summary>LZ-String compressed Base64 in a <c>compressed-json</c> block.</summary>
    CompressedJson,
}
