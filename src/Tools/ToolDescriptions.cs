namespace Snail.MCP.Vault.Tools;

/// <summary>What a model reads about each tool and parameter: instructions and limits that steer the agent, changed with care.</summary>
/// <remarks>
/// Kept apart from the code that obeys them so a change of wording shows as a change of contract. The guard tests hold
/// every limit and every allowed value named here to the code: SchemeLimits and StyleVocabulary.
/// </remarks>
internal static class ToolDescriptions
{
    public const string VaultStatus = "Shows the vault root path and how many Excalidraw schemes it contains.";

    public const string SchemeList = "Lists vault-relative paths of all Excalidraw scheme files.";

    public const string SchemeRead =
        "Reads an Excalidraw scheme as a logical graph: nodes are shapes with their captions, edges are arrows between them, labels are free-standing texts.";

    public const string SchemeCreate =
        "Draws an Excalidraw scheme from a logical description: you list what exists and what connects to what, " +
        "the server computes the arrangement, ids and bindings. THE DEFAULT CHOICE — use it whenever the point is " +
        "the information rather than the picture: an architecture drawn from the code, a quick map of a system, a " +
        "first draft to refine later, or any request that does not say how the drawing should look. The layout is " +
        "guaranteed readable: groups become labeled areas packed into a grid, and nothing overlaps. " +
        "When the composition itself has to carry meaning — lanes, a legend, emphasis, a deliberate arrangement — " +
        "use scheme_draw instead. To rework an existing scheme, call this with the SAME path, overwrite=true and " +
        "the FULL new spec — never invent a neighbouring file name.";

    public const string SchemeDraw =
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
        "prefer scheme_create.";

    public const string ReadPath = "Vault-relative path of the scheme file, e.g. 'Excalidraw/RAG.md'.";

    public const string WritePath = "Vault-relative path of the scheme file, e.g. 'Excalidraw/Payments.md'.";

    public const string Nodes =
        "Nodes to draw; 'key' names the node for edges, 'text' is the caption, optional 'shape' is rectangle (default), " +
        "diamond or ellipse. Optional 'group' stacks related nodes inside a labeled frame — prefer groups for schemes " +
        "with more than ~8 nodes. At most 500 nodes.";

    public const string Edges =
        "Directed connections between node keys, each with an optional label. Without groups, the columns all edges " +
        "skip add up to at most 20000 — give big schemes groups. At most 2000 edges.";

    public const string CreateOverwrite =
        "Set true to redraw an existing file in place; the whole drawing is replaced by this spec. Without it an existing path is refused.";

    public const string Boxes = "Blocks with captions; 'key' names the block for arrows. At most 500 boxes.";

    public const string Zones = "Background areas drawn under everything else — lanes, sections, highlighted regions.";

    public const string Texts = "Free-standing texts: titles, legends, annotations.";

    public const string Arrows = "Connections between blocks, each optionally routed through explicit bends. At most 2000 arrows.";

    public const string DrawOverwrite =
        "Set true to redraw an existing file in place; the whole drawing is replaced. Without it an existing path is refused.";
}
