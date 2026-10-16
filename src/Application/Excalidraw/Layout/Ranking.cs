namespace Snail.MCP.Obsidian.Application.Excalidraw.Layout;

/// <summary>Assigns every key of a directed graph a rank, the column it is drawn in, so that edges point forward.</summary>
internal static class Ranking
{
    /// <summary>Ranks by longest path from a source, packed so that no rank is empty; edges closing a cycle are left out.</summary>
    public static Dictionary<string, int> Ranks(IReadOnlyList<string> keys, IReadOnlyList<(string From, string To)> edges)
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
    /// <remarks>
    /// The walk keeps a stack of its own: the recursive one took a thread stack frame per node of a chain, and a chain
    /// of 20 000 nodes overflowed it and killed the server, which no catch can prevent.
    /// </remarks>
    private static HashSet<int> BackEdges(IReadOnlyList<string> keys, IReadOnlyList<(string From, string To)> edges)
    {
        var adjacency = keys.ToDictionary(key => key, _ => new List<(int Index, string To)>());
        for (var i = 0; i < edges.Count; i++)
            adjacency[edges[i].From].Add((i, edges[i].To));

        var state = keys.ToDictionary(key => key, _ => Visit.Unseen);
        HashSet<int> back = [];
        var path = new Stack<(string Key, int Next)>();

        foreach (var root in keys)
        {
            if (state[root] != Visit.Unseen)
                continue;

            state[root] = Visit.OnPath;
            path.Push((root, 0));

            while (path.TryPop(out var top))
            {
                var outgoing = adjacency[top.Key];
                if (top.Next == outgoing.Count)
                {
                    state[top.Key] = Visit.Done;
                    continue;
                }

                path.Push((top.Key, top.Next + 1));

                var (index, to) = outgoing[top.Next];
                switch (state[to])
                {
                    case Visit.OnPath:
                        back.Add(index);
                        break;
                    case Visit.Unseen:
                        state[to] = Visit.OnPath;
                        path.Push((to, 0));
                        break;
                }
            }
        }

        return back;
    }

    private enum Visit { Unseen, OnPath, Done }
}
