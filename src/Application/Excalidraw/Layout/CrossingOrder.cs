namespace Snail.MCP.Vault.Application.Excalidraw.Layout;

/// <summary>Orders the slots of each rank so that fewer edges cross: each slot moves to the mean position of its neighbours.</summary>
internal static class CrossingOrder
{
    private const int Sweeps = 4;

    /// <summary>Reorders every rank in place, sweeping down by the rank above and back up by the rank below.</summary>
    public static void Minimize(List<List<string>> order, Dictionary<string, List<string>> above, Dictionary<string, List<string>> below)
    {
        for (var sweep = 0; sweep < Sweeps; sweep++)
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

    /// <summary>Records <paramref name="value"/> as a neighbour of <paramref name="key"/>.</summary>
    public static void Link(Dictionary<string, List<string>> map, string key, string value)
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
}
