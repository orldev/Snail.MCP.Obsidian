using Snail.MCP.Vault.Application.Excalidraw.Drawing;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw.Layout;

/// <summary>Groups become labeled frames packed into a grid; members stack vertically inside their frame.</summary>
/// <remarks>
/// Stages are packed into a grid rather than one long row: a wide, flat diagram forces every connection into a long
/// haul, while a roughly square one keeps them local. An edge between stack members that are not neighbours, or
/// between cells the grid did not put side by side, travels in its own vertical lane clear of the blocks, entering
/// both ends from the side.
/// </remarks>
internal static class GroupedLayout
{
    /// <summary>Space between a frame's border and its members, and the inset of its title.</summary>
    public const int FramePadding = 28;

    private const int FrameTitleBand = 60;
    private const int FrameRowGap = 36;
    private const int FrameStackGap = 96;
    private const int SideLaneGap = 44;

    /// <summary>The group a node belongs to, or null when it names none.</summary>
    public static string? GroupOf(NodeSpec node) =>
        string.IsNullOrWhiteSpace(node.Group) ? null : node.Group;

    /// <summary>Places the nodes of a specification that uses groups and routes its edges.</summary>
    public static LayoutPlan Place(SchemeSpec spec)
    {
        var clusterOf = spec.Nodes.ToDictionary(node => node.Key, node => GroupOf(node) ?? $"\n@{node.Key}");
        List<string> clusters = [];
        foreach (var node in spec.Nodes)
        {
            if (!clusters.Contains(clusterOf[node.Key]))
                clusters.Add(clusterOf[node.Key]);
        }

        var members = clusters.ToDictionary(
            cluster => cluster,
            cluster => spec.Nodes.Where(node => clusterOf[node.Key] == cluster).ToList());
        var clusterEdges = spec.Edges
            .Where(edge => clusterOf[edge.From] != clusterOf[edge.To])
            .Select(edge => (From: clusterOf[edge.From], To: clusterOf[edge.To]))
            .ToList();

        var ranks = Ranking.Ranks(clusters, clusterEdges);
        var order = Enumerable.Range(0, ranks.Values.Max() + 1).Select(_ => new List<string>()).ToList();
        foreach (var cluster in clusters)
            order[ranks[cluster]].Add(cluster);

        Dictionary<string, List<string>> above = [], below = [];
        foreach (var (from, to) in clusterEdges)
        {
            if (ranks[to] != ranks[from] + 1)
                continue;

            CrossingOrder.Link(above, to, from);
            CrossingOrder.Link(below, from, to);
        }

        CrossingOrder.Minimize(order, above, below);

        var size = clusters.ToDictionary(cluster => cluster, cluster =>
        {
            var width = members[cluster].Max(node => StyleRules.Measure(node).Width);
            var height = members[cluster].Sum(node => StyleRules.Measure(node).Height) + FrameRowGap * (members[cluster].Count - 1);

            return cluster.StartsWith('\n')
                ? (Width: width, Height: height)
                : (Width: width + 2 * FramePadding, Height: height + FrameTitleBand + FramePadding);
        });

        var stages = order
            .Select(cell => (
                Width: cell.Max(cluster => size[cluster].Width),
                Height: cell.Sum(cluster => size[cluster].Height) + FrameStackGap * (cell.Count - 1)))
            .ToList();
        var grid = Grid.Pack(stages);

        Dictionary<string, Rect> frames = [];
        Dictionary<string, Rect> plates = [];
        Dictionary<string, int> stackRow = [];
        Dictionary<string, (int Row, int Column)> cellOf = [];
        List<GroupContainer> containers = [];

        for (var stage = 0; stage < order.Count; stage++)
        {
            var cell = grid.Cell(stage);
            var y = cell.Y;

            foreach (var cluster in order[stage])
            {
                var (width, height) = size[cluster];
                var left = cell.X + (cell.Width - width) / 2;
                var framed = !cluster.StartsWith('\n');
                var innerWidth = framed ? width - 2 * FramePadding : width;
                var innerY = y + (framed ? FrameTitleBand : 0);

                for (var row = 0; row < members[cluster].Count; row++)
                {
                    var node = members[cluster][row];
                    var (nodeWidth, nodeHeight) = StyleRules.Measure(node);
                    frames[node.Key] = new Rect(
                        left + (framed ? FramePadding : 0) + (innerWidth - nodeWidth) / 2, innerY, nodeWidth, nodeHeight);
                    stackRow[node.Key] = row;
                    innerY += nodeHeight + FrameRowGap;
                }

                plates[cluster] = new Rect(left, y, width, height);
                cellOf[cluster] = grid.Position(stage);
                if (framed)
                    containers.Add(new GroupContainer(cluster, plates[cluster]));

                y += height + FrameStackGap;
            }
        }

        List<EdgeRoute> routes = [];
        var extent = plates.Values.Concat(frames.Values).ToList();
        var bottom = extent.Max(rect => rect.Y + rect.Height);
        var leftEdge = extent.Min(rect => rect.X);
        var rightEdge = extent.Max(rect => rect.X + rect.Width);
        int lanesLeft = 0, lanesRight = 0, loops = 0;

        foreach (var edge in spec.Edges)
        {
            var from = frames[edge.From];
            var to = frames[edge.To];
            var fromCluster = clusterOf[edge.From];
            var toCluster = clusterOf[edge.To];

            if (edge.From == edge.To)
            {
                routes.Add(EdgeRoutes.SelfLoop(from, bottom + EdgeRoutes.ArcGap * ++loops));
            }
            else if (fromCluster == toCluster && Math.Abs(stackRow[edge.To] - stackRow[edge.From]) == 1)
            {
                routes.Add(EdgeRoutes.Stacked(from, to, stackRow[edge.To] > stackRow[edge.From]));
            }
            else if (fromCluster != toCluster && EdgeRoutes.Adjacent(cellOf[fromCluster], cellOf[toCluster]) is { } side)
            {
                routes.Add(EdgeRoutes.Neighbour(from, to, side));
            }
            else
            {
                var onLeft = (from.X + to.X) / 2 < (leftEdge + rightEdge) / 2;
                var origin = fromCluster == toCluster && onLeft ? plates[fromCluster].X : onLeft ? leftEdge : rightEdge;
                var lane = onLeft
                    ? origin - SideLaneGap * ++lanesLeft
                    : origin + SideLaneGap * ++lanesRight;

                routes.Add(EdgeRoutes.Lane(from, to, lane, onLeft));
            }
        }

        return new LayoutPlan(frames, routes, containers);
    }
}
