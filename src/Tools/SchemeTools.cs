using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Obsidian.Application.Schemes;
using Snail.MCP.Obsidian.Configuration;
using Snail.MCP.Obsidian.Domain;
using Snail.Sdk.Mcp.Hosting.Answers;

namespace Snail.MCP.Obsidian.Tools;

/// <summary>The MCP tools that read and draw Excalidraw schemes in the vault.</summary>
/// <remarks>
/// The two that draw say they are destructive: with overwrite=true they replace a whole drawing. Every tool answers in
/// the envelope of <c>ToolAnswer</c>; a refusal the vault throws is answered by <c>ExplainFailures</c> in the words
/// <see cref="VaultFailures"/> gives it.
/// </remarks>
[McpServerToolType]
public sealed class SchemeTools(SchemeCatalog catalog, VaultConfig config)
{
    /// <summary>Where the vault is and how many schemes it holds.</summary>
    [McpServerTool(Name = "vault_status", ReadOnly = true)]
    [Description(ToolDescriptions.VaultStatus)]
    public CallToolResult Status() => ToolAnswer.Ok(new VaultStatus(config.Root, catalog.List().Count), VaultJson.Default.VaultStatus);

    /// <summary>The paths of every scheme.</summary>
    [McpServerTool(Name = "scheme_list", ReadOnly = true)]
    [Description(ToolDescriptions.SchemeList)]
    public CallToolResult List() => ToolAnswer.Ok(catalog.List(), VaultJson.Default.IReadOnlyListString);

    /// <summary>A scheme as its logical graph.</summary>
    [McpServerTool(Name = "scheme_read", ReadOnly = true)]
    [Description(ToolDescriptions.SchemeRead)]
    public CallToolResult Read(
        [Description(ToolDescriptions.ReadPath)] string path) =>
        ToolAnswer.Ok(catalog.Read(path), VaultJson.Default.SchemeGraph);

    /// <summary>Lays out and writes a scheme from nodes and edges.</summary>
    [McpServerTool(Name = "scheme_create", ReadOnly = false, Destructive = true)]
    [Description(ToolDescriptions.SchemeCreate)]
    public CallToolResult Create(
        [Description(ToolDescriptions.WritePath)] string path,
        [Description(ToolDescriptions.Nodes)] IReadOnlyList<NodeSpec> nodes,
        [Description(ToolDescriptions.Edges)] IReadOnlyList<EdgeSpec>? edges = null,
        [Description(ToolDescriptions.CreateOverwrite)] bool overwrite = false)
    {
        var spec = new SchemeSpec { Nodes = nodes, Edges = edges ?? [] };
        var created = new CreatedScheme(catalog.Create(path, spec, overwrite), nodes.Count, edges?.Count ?? 0);

        return ToolAnswer.Ok(created, VaultJson.Default.CreatedScheme);
    }

    /// <summary>Renders and writes a scheme the agent composed.</summary>
    [McpServerTool(Name = "scheme_draw", ReadOnly = false, Destructive = true)]
    [Description(ToolDescriptions.SchemeDraw)]
    public CallToolResult Draw(
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
        var (writtenPath, warnings) = catalog.Draw(path, spec, overwrite);
        var drawn = new DrawnScheme(writtenPath, boxes.Count, arrows?.Count ?? 0, warnings);

        return ToolAnswer.Ok(drawn, VaultJson.Default.DrawnScheme);
    }
}
