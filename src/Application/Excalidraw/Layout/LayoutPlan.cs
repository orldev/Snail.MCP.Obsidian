namespace Snail.MCP.Vault.Application.Excalidraw.Layout;

/// <summary>Where every node goes, how every edge runs, in the order of the specification, and the group backdrops.</summary>
internal sealed record LayoutPlan(
    Dictionary<string, Rect> Frames,
    IReadOnlyList<EdgeRoute> Routes,
    IReadOnlyList<GroupContainer> Containers);
