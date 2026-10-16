using Snail.MCP.Obsidian.Application.Excalidraw.Drawing;
using Snail.MCP.Obsidian.Application.Excalidraw.Format;
using Snail.MCP.Obsidian.Application.Ports;
using Snail.MCP.Obsidian.Application.Schemes;

namespace Snail.MCP.Obsidian.Tests.Application.Schemes;

public class SchemeCatalogTests
{
    private const string SceneJson = """{"type":"excalidraw","version":2,"elements":[{"id":"a1","type":"rectangle","x":0,"y":0,"width":10,"height":10,"isDeleted":false}]}""";

    [Fact]
    public void List_MixedNotes_GivesOnlyExcalidrawFiles()
    {
        var catalog = Catalog(new()
        {
            ["Excalidraw/Scheme.md"] = SchemeFile(),
            ["Notes/Plain.md"] = "# Just a note\n",
        });

        Assert.Equal(["Excalidraw/Scheme.md"], catalog.List());
    }

    [Fact]
    public void Read_SchemeFile_GivesItsGraph()
    {
        var catalog = Catalog(new() { ["Scheme.md"] = SchemeFile() });

        var graph = catalog.Read("Scheme.md");

        Assert.Single(graph.Nodes);
    }

    internal static SchemeCatalog Catalog(Dictionary<string, string> files) =>
        new(new FakeVaultStore(files), new SceneComposer(new Random(42)), new SceneDrafter(new Random(42)));

    private static string SchemeFile() =>
        $"---\n\nexcalidraw-plugin: parsed\n\n---\n\n# Excalidraw Data\n\n## Text Elements\n\n%%\n## Drawing\n```compressed-json\n{LzString.CompressToBase64(SceneJson)}\n```\n%%";

    internal sealed class FakeVaultStore(Dictionary<string, string> files) : IVaultStore
    {
        public IEnumerable<string> MarkdownFiles() => files.Keys;

        public string Read(string relativePath) => files[relativePath];

        public string? ReadHead(string relativePath) =>
            files.TryGetValue(relativePath, out var content) ? content[..Math.Min(512, content.Length)] : null;

        public bool Exists(string relativePath) => files.ContainsKey(relativePath);

        public bool TryWrite(string relativePath, string content, bool overwrite)
        {
            if (!overwrite && files.ContainsKey(relativePath))
                return false;

            files[relativePath] = content;

            return true;
        }
    }
}
