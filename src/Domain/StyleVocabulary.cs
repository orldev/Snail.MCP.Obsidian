namespace Snail.MCP.Vault.Domain;

/// <summary>Every value a style field of a scheme accepts, the words its description must name.</summary>
/// <remarks>
/// The descriptions are a contract a model obeys, written by hand beside the code; the guard tests hold each of them
/// to these lists, so a value added here fails the build until the description that offers it names it.
/// </remarks>
public static class StyleVocabulary
{
    /// <summary>The shapes a node or a box takes.</summary>
    public static IReadOnlyList<string> Shapes { get; } = ["rectangle", "diamond", "ellipse"];

    /// <summary>How a fill is painted.</summary>
    public static IReadOnlyList<string> FillStyles { get; } = ["solid", "hachure", "cross-hatch", "zigzag"];

    /// <summary>How an outline or a line is drawn.</summary>
    public static IReadOnlyList<string> StrokeStyles { get; } = ["solid", "dashed", "dotted"];

    /// <summary>The widths of an outline or a line.</summary>
    public static IReadOnlyList<int> StrokeWidths { get; } = [1, 2, 4];

    /// <summary>The heads at either end of an arrow.</summary>
    public static IReadOnlyList<string> Arrowheads { get; } = ["none", "arrow", "triangle", "bar", "dot", "diamond"];

    /// <summary>The faces of a box an arrow leaves from or enters.</summary>
    public static IReadOnlyList<string> Sides { get; } = ["auto", "left", "right", "top", "bottom"];

    /// <summary>The fonts of a caption or a text, by the names the plugin's fonts go by.</summary>
    public static IReadOnlyList<string> Fonts { get; } = ["excalifont", "nunito", "lilita", "comic", "virgil", "helvetica", "cascadia"];
}
