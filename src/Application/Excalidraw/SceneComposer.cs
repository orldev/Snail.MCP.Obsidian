using System.Text.Json.Nodes;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>Turns a scheme specification into a full Excalidraw scene: grid layout, styling, ids and bindings.</summary>
public sealed class SceneComposer(Random? random = null)
{
    private const int DefaultFontSize = ElementFactory.DefaultFontSize;
    private const double LineHeight = ElementFactory.LineHeight;
    private const int ColumnGap = 200;
    private const int RowGap = 64;
    private const int ArcGap = 88;
    private const int SideLaneGap = 44;
    private const double WaypointSize = 24;
    private const int OrderingSweeps = 4;
    private const int FramePadding = 28;
    private const int FrameTitleBand = 60;
    private const int GroupTitleSize = 20;
    private const int FrameRowGap = 36;
    private const int FrameColumnGap = 240;
    private const int FrameStackGap = 96;


    private readonly Random _random = random ?? Random.Shared;

    public ComposedScene Compose(SchemeSpec spec)
    {
        Validate(spec);

        var layout = Place(spec);
        var factory = new ElementFactory(_random);

        JsonArray elements = [];
        List<SchemeCaption> captions = [];
        Dictionary<string, JsonObject> shapesByKey = [];

        // Backdrops first: they must sit below the nodes they carry.
        foreach (var container in layout.Containers)
            elements.Add((JsonNode)GroupPlate(factory, container));

        foreach (var container in layout.Containers)
        {
            var title = GroupTitle(factory, container);
            captions.Add(new SchemeCaption { Text = container.Title, Id = ElementFactory.Id(title) });
            elements.Add((JsonNode)title);
        }

        foreach (var node in spec.Nodes)
        {
            var frame = layout.Frames[node.Key];
            var shape = factory.Shape(node, frame);
            shapesByKey[node.Key] = shape;
            elements.Add((JsonNode)shape);

            var caption = factory.Caption(node, node.Text, shape, frame);
            ElementFactory.Bind(shape, caption, "text");
            captions.Add(new SchemeCaption { Text = node.Text, Id = ElementFactory.Id(caption) });
            elements.Add((JsonNode)caption);
        }

        for (var i = 0; i < spec.Edges.Count; i++)
        {
            var edge = spec.Edges[i];
            var arrow = factory.Arrow(edge, shapesByKey[edge.From], shapesByKey[edge.To], layout.Routes[i]);
            ElementFactory.Bind(shapesByKey[edge.From], arrow, "arrow");
            ElementFactory.Bind(shapesByKey[edge.To], arrow, "arrow");
            elements.Add((JsonNode)arrow);

            if (edge.Label.Length == 0)
                continue;

            var label = factory.ArrowLabel(edge.Label, arrow);
            ElementFactory.Bind(arrow, label, "text");
            captions.Add(new SchemeCaption { Text = edge.Label, Id = ElementFactory.Id(label) });
            elements.Add((JsonNode)label);
        }

        return new ComposedScene { Scene = ElementFactory.Scene(elements), Captions = captions };
    }

    private static void Validate(SchemeSpec spec)
    {
        if (spec.Nodes.Count == 0)
            throw new ArgumentException("A scheme needs at least one node.");

        HashSet<string> keys = [];
        foreach (var node in spec.Nodes)
        {
            if (!keys.Add(node.Key))
                throw new ArgumentException($"Node key '{node.Key}' is used twice.");

            StyleRules.ValidateNode(node);
        }

        foreach (var edge in spec.Edges)
        {
            if (!keys.Contains(edge.From))
                throw new ArgumentException($"Edge refers to unknown node key '{edge.From}'.");

            if (!keys.Contains(edge.To))
                throw new ArgumentException($"Edge refers to unknown node key '{edge.To}'.");

            StyleRules.ValidateEdge(edge);
        }
    }

    private static LayoutPlan Place(SchemeSpec spec) =>
        spec.Nodes.Any(node => Group(node) is not null) ? PlaceGrouped(spec) : PlaceLayered(spec);

    private static string? Group(NodeSpec node) =>
        string.IsNullOrWhiteSpace(node.Group) ? null : node.Group;

    /// <summary>Groups become labeled frame columns; members stack vertically inside their frame.</summary>
    private static LayoutPlan PlaceGrouped(SchemeSpec spec)
    {
        var clusterOf = spec.Nodes.ToDictionary(node => node.Key, node => Group(node) ?? $"\n@{node.Key}");
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

        var ranks = Ranks(clusters, clusterEdges);
        var order = Enumerable.Range(0, ranks.Values.Max() + 1).Select(_ => new List<string>()).ToList();
        foreach (var cluster in clusters)
            order[ranks[cluster]].Add(cluster);

        Dictionary<string, List<string>> above = [], below = [];
        foreach (var (from, to) in clusterEdges)
        {
            if (ranks[to] != ranks[from] + 1)
                continue;

            Link(above, to, from);
            Link(below, from, to);
        }

        MinimizeCrossings(order, above, below);

        var size = clusters.ToDictionary(cluster => cluster, cluster =>
        {
            var width = members[cluster].Max(NodeWidth);
            var height = members[cluster].Sum(NodeHeight) + FrameRowGap * (members[cluster].Count - 1);

            return cluster.StartsWith('\n')
                ? (Width: width, Height: height)
                : (Width: width + 2 * FramePadding, Height: height + FrameTitleBand + FramePadding);
        });

        // Stages are packed into a grid rather than one long row: a wide, flat diagram forces every
        // connection into a long haul, while a roughly square one keeps them local.
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
                    var nodeWidth = NodeWidth(node);
                    var nodeHeight = NodeHeight(node);
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
                routes.Add(SelfRoute(from, bottom + ArcGap * ++loops));
            }
            else if (fromCluster == toCluster && Math.Abs(stackRow[edge.To] - stackRow[edge.From]) == 1)
            {
                routes.Add(StackRoute(from, to, stackRow[edge.To] > stackRow[edge.From]));
            }
            else if (fromCluster != toCluster && Adjacent(cellOf[fromCluster], cellOf[toCluster]) is { } side)
            {
                routes.Add(NeighbourRoute(from, to, side));
            }
            else
            {
                // Everything else — stack members that are not neighbours, and cells the grid did not put side
                // by side — travels in its own vertical lane clear of the blocks, entering both ends from the side.
                var onLeft = (from.X + to.X) / 2 < (leftEdge + rightEdge) / 2;
                var origin = fromCluster == toCluster && onLeft ? plates[fromCluster].X : onLeft ? leftEdge : rightEdge;
                var lane = onLeft
                    ? origin - SideLaneGap * ++lanesLeft
                    : origin + SideLaneGap * ++lanesRight;

                routes.Add(LaneRoute(from, to, lane, onLeft));
            }
        }

        return new LayoutPlan(frames, routes, containers);
    }

    private static LayoutPlan PlaceLayered(SchemeSpec spec)
    {
        var ranks = Ranks([.. spec.Nodes.Select(node => node.Key)], [.. spec.Edges.Select(edge => (edge.From, edge.To))]);
        var order = Order(spec, ranks, out var chains);

        Dictionary<string, Rect> frames = [];
        Dictionary<string, (double X, double Y)> waypoints = [];
        var nodesByKey = spec.Nodes.ToDictionary(node => node.Key);

        double x = 0;
        foreach (var column in order)
        {
            var sized = column.Select(id => nodesByKey.TryGetValue(id, out var node)
                ? (Id: id, Width: NodeWidth(node), Height: NodeHeight(node))
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
                Link(above, chain[step], chain[step - 1]);
                Link(below, chain[step - 1], chain[step]);
            }
        }

        MinimizeCrossings(order, above, below);
        return order;
    }

    private static void MinimizeCrossings(List<List<string>> order, Dictionary<string, List<string>> above, Dictionary<string, List<string>> below)
    {
        for (var sweep = 0; sweep < OrderingSweeps; sweep++)
        {
            var down = sweep % 2 == 0;
            var neighbours = down ? above : below;
            var passes = down ? Enumerable.Range(1, order.Count - 1) : Enumerable.Range(0, order.Count - 1).Reverse();

            foreach (var rank in passes)
            {
                var reference = order[down ? rank - 1 : rank + 1];
                var positions = reference.Select((id, position) => (id, position)).ToDictionary(slot => slot.id, slot => (double)slot.position);
                order[rank] = [.. order[rank]
                    .Select((id, position) => (Id: id, Key: Barycenter(neighbours.GetValueOrDefault(id), positions, position)))
                    .OrderBy(slot => slot.Key)
                    .Select(slot => slot.Id)];
            }
        }
    }

    private static void Link(Dictionary<string, List<string>> map, string key, string value)
    {
        if (!map.TryGetValue(key, out var values))
            map[key] = values = [];

        values.Add(value);
    }

    private static double Barycenter(List<string>? neighbours, Dictionary<string, double> positions, int fallback)
    {
        var known = neighbours?.Where(positions.ContainsKey).Select(id => positions[id]).ToList();
        return known is { Count: > 0 } ? known.Average() : fallback;
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

            var dip = bottom + ArcGap * ++backEdges;
            routes.Add(edge.From == edge.To ? SelfRoute(from, dip) : ArcRoute(from, to, dip, above: false));
        }

        return routes;
    }

    /// <summary>A square bracket clearing the diagram: out of the top (or bottom), along <paramref name="level"/>, back in.</summary>
    private static EdgeRoute ArcRoute(Rect from, Rect to, double level, bool above)
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

    private static EdgeRoute SelfRoute(Rect node, double dip)
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
    private static Side? Adjacent((int Row, int Column) from, (int Row, int Column) to) =>
        (to.Row - from.Row, to.Column - from.Column) switch
        {
            (0, 1) => Side.Right,
            (0, -1) => Side.Left,
            (1, 0) => Side.Below,
            (-1, 0) => Side.Above,
            _ => null,
        };

    private static EdgeRoute NeighbourRoute(Rect from, Rect to, Side side) =>
        side switch
        {
            Side.Right => new EdgeRoute((from.X + from.Width, from.Y + from.Height / 2), (to.X, to.Y + to.Height / 2), [], (1, 0.5001), (0, 0.5001)),
            Side.Left => new EdgeRoute((from.X, from.Y + from.Height / 2), (to.X + to.Width, to.Y + to.Height / 2), [], (0, 0.5001), (1, 0.5001)),
            Side.Below => StackRoute(from, to, downwards: true),
            _ => StackRoute(from, to, downwards: false),
        };

    private static EdgeRoute StackRoute(Rect from, Rect to, bool downwards) =>
        downwards
            ? new EdgeRoute((from.X + from.Width / 2, from.Y + from.Height), (to.X + to.Width / 2, to.Y), [], (0.5001, 1), (0.5001, 0))
            : new EdgeRoute((from.X + from.Width / 2, from.Y), (to.X + to.Width / 2, to.Y + to.Height), [], (0.5001, 0), (0.5001, 1));

    /// <summary>A bracket running in a free vertical lane, leaving and entering both blocks from the same side.</summary>
    private static EdgeRoute LaneRoute(Rect from, Rect to, double lane, bool onLeft)
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

    private static Dictionary<string, int> Ranks(IReadOnlyList<string> keys, IReadOnlyList<(string From, string To)> edges)
    {
        var layers = Layers(keys, edges);
        var packed = layers.Values.Distinct().Order()
            .Select((layer, rank) => (layer, rank))
            .ToDictionary(pair => pair.layer, pair => pair.rank);

        return layers.ToDictionary(pair => pair.Key, pair => packed[pair.Value]);
    }

    private static Dictionary<string, int> Layers(IReadOnlyList<string> keys, IReadOnlyList<(string From, string To)> edges)
    {
        var backEdges = BackEdges(keys, edges);
        var layers = keys.ToDictionary(key => key, _ => 0);

        for (var pass = 0; pass < keys.Count; pass++)
        {
            var changed = false;
            for (var i = 0; i < edges.Count; i++)
            {
                var (from, to) = edges[i];
                if (backEdges.Contains(i) || layers[to] >= layers[from] + 1)
                    continue;

                layers[to] = layers[from] + 1;
                changed = true;
            }

            if (!changed)
                break;
        }

        return layers;
    }

    /// <summary>Edge indices that close a cycle; layering skips them so a cycle cannot inflate the ranks.</summary>
    private static HashSet<int> BackEdges(IReadOnlyList<string> keys, IReadOnlyList<(string From, string To)> edges)
    {
        var adjacency = keys.ToDictionary(key => key, _ => new List<(int Index, string To)>());
        for (var i = 0; i < edges.Count; i++)
            adjacency[edges[i].From].Add((i, edges[i].To));

        var state = keys.ToDictionary(key => key, _ => 0);
        HashSet<int> back = [];

        void Visit(string key)
        {
            state[key] = 1;
            foreach (var (index, to) in adjacency[key])
            {
                if (state[to] == 1)
                    back.Add(index);
                else if (state[to] == 0)
                    Visit(to);
            }

            state[key] = 2;
        }

        foreach (var key in keys)
        {
            if (state[key] == 0)
                Visit(key);
        }

        return back;
    }

    private static double NodeWidth(NodeSpec node) => StyleRules.Measure(node, node.Text).Width;

    private static double NodeHeight(NodeSpec node) => StyleRules.Measure(node, node.Text).Height;

    /// <summary>The backdrop a group sits on: a plain rounded rectangle drawn below its members.</summary>
    private static JsonObject GroupPlate(ElementFactory factory, GroupContainer container) =>
        factory.Plate(container.Rect, "#f8f9fa", "#ced4da", "solid", GroupMarker(SceneMarkers.GroupPlate, container.Title));

    private static JsonObject GroupTitle(ElementFactory factory, GroupContainer container) =>
        factory.Label(
            container.Title,
            container.Rect.X + FramePadding,
            container.Rect.Y + FramePadding / 2.0,
            GroupTitleSize,
            "#868e96",
            "excalifont",
            GroupMarker(SceneMarkers.GroupTitle, container.Title));

    private static JsonObject GroupMarker(string role, string group) =>
        new() { [SceneMarkers.Namespace] = new JsonObject { ["role"] = role, ["group"] = group } };

    private enum Side { Left, Right, Above, Below }

    private sealed record LayoutPlan(
        Dictionary<string, Rect> Frames,
        IReadOnlyList<EdgeRoute> Routes,
        IReadOnlyList<GroupContainer> Containers);

    private sealed record GroupContainer(string Title, Rect Rect);
}
