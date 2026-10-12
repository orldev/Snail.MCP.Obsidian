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
    [Description("Shows the vault root path and how many Excalidraw schemes it contains.")]
    public VaultStatus Status() => new(config.RootPath, catalog.List().Count);

    /// <summary>The paths of every scheme.</summary>
    [McpServerTool(Name = "scheme_list", ReadOnly = true)]
    [Description("Lists vault-relative paths of all Excalidraw scheme files.")]
    public string[] List() => [.. catalog.List()];

    /// <summary>A scheme as its logical graph.</summary>
    [McpServerTool(Name = "scheme_read", ReadOnly = true)]
    [Description("Reads an Excalidraw scheme as a logical graph: nodes are shapes with their captions, edges are arrows between them, labels are free-standing texts.")]
    public SchemeGraph Read(
        [Description("Vault-relative path of the scheme file, e.g. 'Excalidraw/RAG.md'.")] string path) =>
        catalog.Read(path);

    /// <summary>Lays out and writes a scheme from nodes and edges.</summary>
    [McpServerTool(Name = "scheme_create", ReadOnly = false, Destructive = true)]
    [Description(
        "Draws an Excalidraw scheme from a logical description: you list what exists and what connects to what, " +
        "the server computes the arrangement, ids and bindings. THE DEFAULT CHOICE — use it whenever the point is " +
        "the information rather than the picture: an architecture drawn from the code, a quick map of a system, a " +
        "first draft to refine later, or any request that does not say how the drawing should look. The layout is " +
        "guaranteed readable: groups become labeled areas packed into a grid, and nothing overlaps. " +
        "When the composition itself has to carry meaning — lanes, a legend, emphasis, a deliberate arrangement — " +
        "use scheme_draw instead. To rework an existing scheme, call this with the SAME path, overwrite=true and " +
        "the FULL new spec — never invent a neighbouring file name.")]
    public CreatedScheme Create(
        [Description("Vault-relative path of the scheme file, e.g. 'Excalidraw/Payments.md'.")] string path,
        [Description("Nodes to draw; 'key' names the node for edges, 'text' is the caption, optional 'shape' is rectangle (default), diamond or ellipse. Optional 'group' stacks related nodes inside a labeled frame — prefer groups for schemes with more than ~8 nodes.")] IReadOnlyList<NodeSpec> nodes,
        [Description("Directed connections between node keys, each with an optional label.")] IReadOnlyList<EdgeSpec>? edges = null,
        [Description("Set true to redraw an existing file in place; the whole drawing is replaced by this spec. Without it an existing path is refused.")] bool overwrite = false)
    {
        var createdPath = catalog.Create(path, new SchemeSpec { Nodes = nodes, Edges = edges ?? [] }, overwrite);

        return new CreatedScheme(createdPath, nodes.Count, edges?.Count ?? 0);
    }

    /// <summary>Renders and writes a scheme the agent composed.</summary>
    [McpServerTool(Name = "scheme_draw", ReadOnly = false, Destructive = true)]
    [Description(
        "Draws a scheme you compose yourself: you place every block, zone and label on the canvas, the server only " +
        "renders it — measuring captions, generating ids, binding arrows and warning about overlaps. " +
        "USE THIS when the composition itself carries meaning and automatic layout cannot express it: swimlanes per " +
        "team or stage, nested or overlapping areas, a legend explaining what the colours mean, deliberate emphasis " +
        "through size or colour, a boundary band, a radial or timeline arrangement — or when the drawing is meant to " +
        "be presented (a status review, a report, a slide) rather than merely read. Also use it to polish a scheme " +
        "that scheme_create laid out: read it back, keep what works, place the rest by hand. " +
        "Canvas units are pixels: a block is roughly 200-450 wide and 70-150 tall, 60-120 of space between blocks " +
        "reads well, and a zone needs about 60 above its first block for the title. Compose deliberately — this tool " +
        "does nothing to save a careless arrangement. For an ordinary graph where any sensible layout will do, " +
        "prefer scheme_create.")]
    public DrawnScheme Draw(
        [Description("Vault-relative path of the scheme file, e.g. 'Excalidraw/Payments.md'.")] string path,
        [Description("Blocks with captions; 'key' names the block for arrows.")] IReadOnlyList<DrawBox> boxes,
        [Description("Background areas drawn under everything else — lanes, sections, highlighted regions.")] IReadOnlyList<DrawZone>? zones = null,
        [Description("Free-standing texts: titles, legends, annotations.")] IReadOnlyList<DrawText>? texts = null,
        [Description("Connections between blocks, each optionally routed through explicit bends.")] IReadOnlyList<DrawArrow>? arrows = null,
        [Description("Set true to redraw an existing file in place; the whole drawing is replaced. Without it an existing path is refused.")] bool overwrite = false)
    {
        var spec = new DrawSpec
        {
            Boxes = boxes,
            Zones = zones ?? [],
            Texts = texts ?? [],
            Arrows = arrows ?? [],
        };
        var (writtenPath, warnings) = catalog.Draw(path, spec, overwrite);

        return new DrawnScheme(writtenPath, boxes.Count, arrows?.Count ?? 0, warnings);
    }
}
