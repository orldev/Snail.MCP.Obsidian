using System.ComponentModel;
using ModelContextProtocol.Server;
using Snail.MCP.Vault.Application.Schemes;
using Snail.MCP.Vault.Configuration;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Tools;

/// <summary>The MCP tools that read and draw Excalidraw schemes in the vault.</summary>
/// <remarks>The two that draw say they are destructive: with overwrite=true they replace a whole drawing.</remarks>
[McpServerToolType]
public sealed class SchemeTools(SchemeCatalog catalog, VaultConfig config)
{
    /// <summary>Where the vault is and how many schemes it holds.</summary>
    [McpServerTool(Name = "vault_status", ReadOnly = true)]
    [Description(ToolDescriptions.VaultStatus)]
    public VaultStatus Status() => Refusals.Shown(() => new VaultStatus(config.Root, catalog.List().Count));

    /// <summary>The paths of every scheme.</summary>
    [McpServerTool(Name = "scheme_list", ReadOnly = true)]
    [Description(ToolDescriptions.SchemeList)]
    public string[] List() => [.. Refusals.Shown(catalog.List)];

    /// <summary>A scheme as its logical graph.</summary>
    [McpServerTool(Name = "scheme_read", ReadOnly = true)]
    [Description(ToolDescriptions.SchemeRead)]
    public SchemeGraph Read(
        [Description(ToolDescriptions.ReadPath)] string path) =>
        Refusals.Shown(() => catalog.Read(path));

    /// <summary>Lays out and writes a scheme from nodes and edges.</summary>
    [McpServerTool(Name = "scheme_create", ReadOnly = false, Destructive = true)]
    [Description(ToolDescriptions.SchemeCreate)]
    public CreatedScheme Create(
        [Description(ToolDescriptions.WritePath)] string path,
        [Description(ToolDescriptions.Nodes)] IReadOnlyList<NodeSpec> nodes,
        [Description(ToolDescriptions.Edges)] IReadOnlyList<EdgeSpec>? edges = null,
        [Description(ToolDescriptions.CreateOverwrite)] bool overwrite = false)
    {
        var spec = new SchemeSpec { Nodes = nodes, Edges = edges ?? [] };
        var createdPath = Refusals.Shown(() => catalog.Create(path, spec, overwrite));

        return new CreatedScheme(createdPath, nodes.Count, edges?.Count ?? 0);
    }

    /// <summary>Renders and writes a scheme the agent composed.</summary>
    [McpServerTool(Name = "scheme_draw", ReadOnly = false, Destructive = true)]
    [Description(ToolDescriptions.SchemeDraw)]
    public DrawnScheme Draw(
        [Description(ToolDescriptions.WritePath)] string path,
        [Description(ToolDescriptions.Boxes)] IReadOnlyList<DrawBox> boxes,
        [Description(ToolDescriptions.Zones)] IReadOnlyList<DrawZone>? zones = null,
        [Description(ToolDescriptions.Texts)] IReadOnlyList<DrawText>? texts = null,
        [Description(ToolDescriptions.Arrows)] IReadOnlyList<DrawArrow>? arrows = null,
        [Description(ToolDescriptions.DrawOverwrite)] bool overwrite = false)
    {
        var spec = new DrawSpec
        {
            Boxes = boxes,
            Zones = zones ?? [],
            Texts = texts ?? [],
            Arrows = arrows ?? [],
        };
        var (writtenPath, warnings) = Refusals.Shown(() => catalog.Draw(path, spec, overwrite));

        return new DrawnScheme(writtenPath, boxes.Count, arrows?.Count ?? 0, warnings);
    }
}
