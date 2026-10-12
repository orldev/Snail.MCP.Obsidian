namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>
/// Marks the decorations this server draws around a scheme — group backdrops and their titles — so that
/// reading a scene back can tell them apart from the nodes the user cares about. Excalidraw keeps unknown
/// keys of <c>customData</c> untouched, so the marks survive edits in the plugin.
/// </summary>
public static class SceneMarkers
{
    /// <summary>The key of <c>customData</c> the marks live under.</summary>
    public const string Namespace = "snail";
    /// <summary>Marks the backdrop of a group.</summary>
    public const string GroupPlate = "group";
    /// <summary>Marks the title of a group.</summary>
    public const string GroupTitle = "groupTitle";
}
