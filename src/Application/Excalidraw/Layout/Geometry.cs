namespace Snail.MCP.Vault.Application.Excalidraw.Layout;

/// <summary>A box on the canvas.</summary>
internal readonly record struct Rect(double X, double Y, double Width, double Height)
{
    public double CentreX => X + Width / 2;

    public double CentreY => Y + Height / 2;

    public double Right => X + Width;

    public double Bottom => Y + Height;
}

/// <summary>
/// Geometry of one arrow: absolute endpoints, the corners it passes through, and where it attaches to the
/// blocks — an anchor is a fraction of the block's own box, so (0, 0.5) is the middle of its left edge.
/// </summary>
internal readonly record struct EdgeRoute(
    (double X, double Y) Start,
    (double X, double Y) End,
    IReadOnlyList<(double X, double Y)> Bends,
    (double X, double Y) StartAnchor,
    (double X, double Y) EndAnchor);
