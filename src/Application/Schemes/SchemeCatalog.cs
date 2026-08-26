using System.Text.Json.Nodes;
using Snail.MCP.Vault.Application.Excalidraw;
using Snail.MCP.Vault.Application.Ports;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Schemes;

/// <summary>Finds Excalidraw schemes in the vault and serves their logical graphs.</summary>
public sealed class SchemeCatalog(IVaultStore vault)
{
    private const string PluginMarker = "excalidraw-plugin:";

    public IReadOnlyList<string> List() =>
        [.. vault.MarkdownFiles().Where(path => Head(vault.Read(path)).Contains(PluginMarker))];

    public SchemeGraph Read(string relativePath)
    {
        var document = ExcalidrawCodec.Parse(vault.Read(relativePath));
        var scene = JsonNode.Parse(ExcalidrawCodec.DecodeDrawing(document))
            ?? throw new FormatException("The drawing payload is empty.");

        return GraphProjection.Project(scene);
    }

    private static string Head(string content) =>
        content.Length <= 512 ? content : content[..512];
}
