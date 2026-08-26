using System.ComponentModel;
using ModelContextProtocol.Server;
using Snail.MCP.Vault.Application.Schemes;
using Snail.MCP.Vault.Configuration;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Tools;

[McpServerToolType]
public sealed class SchemeTools(SchemeCatalog catalog, VaultConfig config)
{
    [McpServerTool(Name = "vault_status")]
    [Description("Shows the vault root path and how many Excalidraw schemes it contains.")]
    public object Status() =>
        new { vaultPath = config.RootPath, schemeCount = catalog.List().Count };

    [McpServerTool(Name = "scheme_list")]
    [Description("Lists vault-relative paths of all Excalidraw scheme files.")]
    public IReadOnlyList<string> List() => catalog.List();

    [McpServerTool(Name = "scheme_read")]
    [Description("Reads an Excalidraw scheme as a logical graph: nodes are shapes with their captions, edges are arrows between them, labels are free-standing texts.")]
    public SchemeGraph Read(
        [Description("Vault-relative path of the scheme file, e.g. 'Excalidraw/RAG.md'.")] string path) =>
        catalog.Read(path);

    [McpServerTool(Name = "scheme_create")]
    [Description("Draws a new Excalidraw scheme from a logical description; the server computes the layout, ids and bindings itself. Never overwrites an existing file.")]
    public object Create(
        [Description("Vault-relative path for the new file, e.g. 'Excalidraw/Payments.md'.")] string path,
        [Description("Nodes to draw; 'key' names the node for edges, 'text' is the caption, optional 'shape' is rectangle (default), diamond or ellipse.")] IReadOnlyList<NodeSpec> nodes,
        [Description("Directed connections between node keys, each with an optional label.")] IReadOnlyList<EdgeSpec>? edges = null)
    {
        var createdPath = catalog.Create(path, new SchemeSpec { Nodes = nodes, Edges = edges ?? [] });

        return new { path = createdPath, nodeCount = nodes.Count, edgeCount = edges?.Count ?? 0 };
    }
}
