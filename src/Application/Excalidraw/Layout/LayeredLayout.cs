using Snail.MCP.Vault.Application.Excalidraw.Drawing;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw.Layout;

/// <summary>A scheme without groups in columns by rank, left to right; a long edge passes through waypoints in the columns it crosses.</summary>
internal static class LayeredLayout
{
    private const int ColumnGap = 200;
    private const int RowGap = 64;
    private const double WaypointSize = 24;

    /// <summary>Places the nodes of a specification without groups and routes its edges.</summary>
    public static LayoutPlan Place(SchemeSpec spec)
    {
        var ranks = Ranking.Ranks([.. spec.Nodes.Select(node => node.Key)], [.. spec.Edges.Select(edge => (edge.From, edge.To))]);
        ExpectFewSkippedColumns(spec, ranks);

        var order = Order(spec, ranks, out var chains);

        Dictionary<string, Rect> frames = [];
        Dictionary<string, (double X, double Y)> waypoints = [];
        var nodesByKey = spec.Nodes.ToDictionary(node => node.Key);

        double x = 0;
        foreach (var column in order)
        {
            var sized = column.Select(id => nodesByKey.TryGetValue(id, out var node)
                ? (Id: id, Width: StyleRules.Measure(node).Width, Height: StyleRules.Measure(node).Height)
                : (Id: id, Width: WaypointSize, Height: WaypointSize)).ToList();
            var columnWidth = sized.Max(slot => slot.Width);
            var columnHeight = sized.Sum(slot => slot.Height) + RowGap * (sized.Count - 1);

            var y = -columnHeight / 2;
            foreach (var slot in sized)
            {
                if (nodesByKey.ContainsKey(slot.Id))
                    frames[slot.Id] = new Rect(x + (columnWidth - slot.Width) / 2, y, slot.Width, slot.Height);
                else
                    waypoints[slot.Id] = (x + columnWidth / 2, y + slot.Height / 2);

                y += slot.Height + RowGap;
            }

            x += columnWidth + ColumnGap;
        }

        return new LayoutPlan(frames, Routes(spec, frames, waypoints, chains), []);
    }

    /// <summary>Refuses a scheme whose forward edges skip more columns in all than the layout routes in about a second.</summary>
    private static void ExpectFewSkippedColumns(SchemeSpec spec, Dictionary<string, int> ranks)
    {
        var skipped = spec.Edges.Sum(edge => (long)Math.Max(0, ranks[edge.To] - ranks[edge.From] - 1));
        if (skipped > SchemeLimits.MostSkippedColumns)
            throw new ArgumentException($"Without groups the edges of this scheme skip {skipped} columns in all, more than {SchemeLimits.MostSkippedColumns}; give the nodes groups, or split the scheme.");
    }

    /// <summary>Slot ids per rank, crossing-minimized: real nodes mixed with waypoint slots of edges passing through the rank.</summary>
    private static List<List<string>> Order(SchemeSpec spec, Dictionary<string, int> ranks, out List<string>?[] chains)
    {
        var rankCount = ranks.Values.Max() + 1;
        var order = Enumerable.Range(0, rankCount).Select(_ => new List<string>()).ToList();
        foreach (var node in spec.Nodes)
            order[ranks[node.Key]].Add(node.Key);

        Dictionary<string, List<string>> above = [], below = [];
        chains = new List<string>?[spec.Edges.Count];
        for (var i = 0; i < spec.Edges.Count; i++)
        {
            var edge = spec.Edges[i];
            if (ranks[edge.To] <= ranks[edge.From])
                continue;

            List<string> chain = [edge.From];
            for (var rank = ranks[edge.From] + 1; rank < ranks[edge.To]; rank++)
            {
                var id = $"\n{i}@{rank}";
                order[rank].Add(id);
                chain.Add(id);
            }

            chain.Add(edge.To);
            chains[i] = chain;

            for (var step = 1; step < chain.Count; step++)
            {
                CrossingOrder.Link(above, chain[step], chain[step - 1]);
                CrossingOrder.Link(below, chain[step - 1], chain[step]);
            }
        }

        CrossingOrder.Minimize(order, above, below);

        return order;
    }

    private static List<EdgeRoute> Routes(
        SchemeSpec spec,
        Dictionary<string, Rect> frames,
        Dictionary<string, (double X, double Y)> waypoints,
        List<string>?[] chains)
    {
        List<EdgeRoute> routes = [];
        var bottom = frames.Values.Max(frame => frame.Y + frame.Height);
        var backEdges = 0;

        for (var i = 0; i < spec.Edges.Count; i++)
        {
            var edge = spec.Edges[i];
            var from = frames[edge.From];
            var to = frames[edge.To];

            if (chains[i] is { } chain)
            {
                routes.Add(new EdgeRoute(
                    (from.X + from.Width, from.Y + from.Height / 2),
                    (to.X, to.Y + to.Height / 2),
                    [.. chain[1..^1].Select(id => waypoints[id])],
                    (1, 0.5001),
                    (0, 0.5001)));
                continue;
            }

            var dip = bottom + EdgeRoutes.ArcGap * ++backEdges;
            routes.Add(edge.From == edge.To ? EdgeRoutes.SelfLoop(from, dip) : EdgeRoutes.Arc(from, to, dip, above: false));
        }

        return routes;
    }
}
