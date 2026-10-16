namespace Snail.MCP.Obsidian.Domain;

/// <summary>How big a scheme and its texts may be; the descriptions state the same numbers.</summary>
public static class SchemeLimits
{
    /// <summary>The most nodes, or boxes, one scheme takes; a larger map is split into several schemes.</summary>
    /// <remarks>Unbounded, a chain of 100 000 nodes kept the server busy for more than two minutes, and a tool call cannot be cancelled.</remarks>
    public const int MostNodes = 500;

    /// <summary>The most edges, or arrows, one scheme takes.</summary>
    public const int MostEdges = 2000;

    /// <summary>How many columns the edges of a scheme without groups may skip in all, each skipped column a waypoint.</summary>
    /// <remarks>
    /// Within the node and edge limits, 1 501 edges spanning 450 columns each made 675 000 waypoints and 20 s of layout;
    /// 20 000 take about a second. A scheme that needs more is a scheme that needs groups.
    /// </remarks>
    public const int MostSkippedColumns = 20_000;

    /// <summary>The smallest text size accepted, in px: below it a caption cannot be read at the plugin's default zoom.</summary>
    public const int SmallestFontSize = 8;

    /// <summary>The largest text size accepted, in px.</summary>
    /// <remarks>Unbounded, int.MaxValue wrapped the measured width negative and a negative size was drawn as given.</remarks>
    public const int LargestFontSize = 120;
}
