namespace Snail.MCP.Vault.Application.Excalidraw.Layout;

/// <summary>The shapes an arrow takes between two placed blocks: straight between neighbours, a bracket around everything else.</summary>
internal static class EdgeRoutes
{
    /// <summary>How far below the drawing each successive loop or backward bracket runs.</summary>
    public const int ArcGap = 88;

    /// <summary>A square bracket clearing the diagram: out of the top (or bottom), along <paramref name="level"/>, back in.</summary>
    public static EdgeRoute Arc(Rect from, Rect to, double level, bool above)
    {
        var startX = from.X + from.Width / 2;
        var endX = to.X + to.Width / 2;
        var side = above ? 0d : 1d;

        return new EdgeRoute(
            (startX, above ? from.Y : from.Y + from.Height),
            (endX, above ? to.Y : to.Y + to.Height),
            [(startX, level), (endX, level)],
            (0.5001, side),
            (0.5001, side));
    }

    /// <summary>An arrow from a block back to itself, dipping to <paramref name="dip"/> below it.</summary>
    public static EdgeRoute SelfLoop(Rect node, double dip)
    {
        var centreX = node.X + node.Width / 2;

        return new EdgeRoute(
            (centreX, node.Y + node.Height),
            (centreX, node.Y + node.Height),
            [(centreX - 40, dip), (centreX + 40, dip)],
            (0.5001, 1),
            (0.5001, 1));
    }

    /// <summary>Which face of a cell its neighbour touches, or null when the two cells are not adjacent.</summary>
    public static Side? Adjacent((int Row, int Column) from, (int Row, int Column) to) =>
        (to.Row - from.Row, to.Column - from.Column) switch
        {
            (0, 1) => Side.Right,
            (0, -1) => Side.Left,
            (1, 0) => Side.Below,
            (-1, 0) => Side.Above,
            _ => null,
        };

    /// <summary>A straight arrow between blocks of neighbouring cells, through the faces that look at each other.</summary>
    public static EdgeRoute Neighbour(Rect from, Rect to, Side side) =>
        side switch
        {
            Side.Right => new EdgeRoute((from.X + from.Width, from.Y + from.Height / 2), (to.X, to.Y + to.Height / 2), [], (1, 0.5001), (0, 0.5001)),
            Side.Left => new EdgeRoute((from.X, from.Y + from.Height / 2), (to.X + to.Width, to.Y + to.Height / 2), [], (0, 0.5001), (1, 0.5001)),
            Side.Below => Stacked(from, to, downwards: true),
            _ => Stacked(from, to, downwards: false),
        };

    /// <summary>A vertical arrow between blocks stacked one above the other.</summary>
    public static EdgeRoute Stacked(Rect from, Rect to, bool downwards) =>
        downwards
            ? new EdgeRoute((from.X + from.Width / 2, from.Y + from.Height), (to.X + to.Width / 2, to.Y), [], (0.5001, 1), (0.5001, 0))
            : new EdgeRoute((from.X + from.Width / 2, from.Y), (to.X + to.Width / 2, to.Y + to.Height), [], (0.5001, 0), (0.5001, 1));

    /// <summary>A bracket running in a free vertical lane, leaving and entering both blocks from the same side.</summary>
    public static EdgeRoute Lane(Rect from, Rect to, double lane, bool onLeft)
    {
        var (fromY, toY) = (from.Y + from.Height / 2, to.Y + to.Height / 2);
        var side = onLeft ? 0d : 1d;

        return new EdgeRoute(
            (onLeft ? from.X : from.X + from.Width, fromY),
            (onLeft ? to.X : to.X + to.Width, toY),
            [(lane, fromY), (lane, toY)],
            (side, 0.5001),
            (side, 0.5001));
    }

    /// <summary>A face of a grid cell.</summary>
    public enum Side { Left, Right, Above, Below }
}
