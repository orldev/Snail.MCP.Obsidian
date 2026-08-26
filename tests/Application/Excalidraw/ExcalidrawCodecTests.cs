using Snail.MCP.Vault.Application.Excalidraw;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Tests.Application.Excalidraw;

public class ExcalidrawCodecTests
{
    private const string SceneJson = """{"type":"excalidraw","version":2,"elements":[{"id":"a1","type":"rectangle","x":0,"y":0}]}""";

    [Fact]
    public void ParseDetectsCompressedFormat()
    {
        var document = ExcalidrawCodec.Parse(CompressedFile(SceneJson));

        Assert.Equal(DrawingFormat.CompressedJson, document.Format);
        Assert.EndsWith("```compressed-json\n", document.Prefix);
        Assert.StartsWith("```", document.Suffix);
    }

    [Fact]
    public void ParseDetectsPlainJsonFormat()
    {
        var document = ExcalidrawCodec.Parse(PlainJsonFile(SceneJson));

        Assert.Equal(DrawingFormat.Json, document.Format);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SerializeReproducesInputByteExact(bool compressed)
    {
        var file = compressed ? CompressedFile(SceneJson) : PlainJsonFile(SceneJson);

        Assert.Equal(file, ExcalidrawCodec.Serialize(ExcalidrawCodec.Parse(file)));
    }

    [Fact]
    public void DecodeDrawingReturnsOriginalJson()
    {
        var document = ExcalidrawCodec.Parse(CompressedFile(SceneJson));

        Assert.Equal(SceneJson, ExcalidrawCodec.DecodeDrawing(document));
    }

    [Fact]
    public void WithDrawingRoundTripsThroughEncoding()
    {
        var document = ExcalidrawCodec.Parse(CompressedFile(SceneJson));
        var updated = """{"type":"excalidraw","version":2,"elements":[]}""";

        var rewritten = ExcalidrawCodec.WithDrawing(document, updated);

        Assert.Equal(updated, ExcalidrawCodec.DecodeDrawing(rewritten));
        Assert.Equal(document.Prefix, rewritten.Prefix);
        Assert.Equal(document.Suffix, rewritten.Suffix);
    }

    [Fact]
    public void WithDrawingWrapsCompressedPayloadInSpacedLines()
    {
        var bigScene = $$"""{"type":"excalidraw","elements":[{{string.Join(",", Enumerable.Range(0, 300).Select(i => $$"""{"id":"node{{i}}","x":{{i * 10}}}"""))}}]}""";
        var document = ExcalidrawCodec.WithDrawing(ExcalidrawCodec.Parse(CompressedFile(SceneJson)), bigScene);

        var lines = document.Drawing.TrimEnd('\n').Split('\n');

        Assert.True(lines.Length > 2);
        Assert.All(lines.Where((_, i) => i % 2 == 0), line => Assert.InRange(line.Length, 1, 256));
        Assert.All(lines.Where((_, i) => i % 2 == 1), line => Assert.Equal("", line));
    }

    [Fact]
    public void ParsePrefersFenceAfterDrawingHeading()
    {
        var decoy = "## Text Elements\nsnippet with a fence ^ref1\n\n```json\n{\"not\":\"the drawing\"}\n```\n\n%%\n## Drawing\n";
        var file = $"{decoy}```compressed-json\n{LzString.CompressToBase64(SceneJson)}\n```\n%%";

        var document = ExcalidrawCodec.Parse(file);

        Assert.Equal(DrawingFormat.CompressedJson, document.Format);
        Assert.Equal(SceneJson, ExcalidrawCodec.DecodeDrawing(document));
    }

    [Fact]
    public void ParseAcceptsLegacyLevelOneDrawingHeading()
    {
        var file = $"# Text Elements\nNode A ^abc123\n\n# Drawing\n```json\n{SceneJson}\n```\n";

        var document = ExcalidrawCodec.Parse(file);

        Assert.Equal(SceneJson, ExcalidrawCodec.DecodeDrawing(document).TrimEnd('\n'));
    }

    [Fact]
    public void ParseRejectsPlainNote()
    {
        Assert.Throws<FormatException>(() => ExcalidrawCodec.Parse("# Just a note\n\nNo drawing here.\n"));
    }

    [Fact]
    public void ParseRejectsUnclosedDrawingBlock()
    {
        Assert.Throws<FormatException>(() => ExcalidrawCodec.Parse("## Drawing\n```compressed-json\nAAAA"));
    }

    private static string CompressedFile(string sceneJson) =>
        $"---\n\nexcalidraw-plugin: parsed\ntags: [excalidraw]\n\n---\n\n# Excalidraw Data\n\n## Text Elements\nNode A ^abc123\n\n%%\n## Drawing\n```compressed-json\n{LzString.CompressToBase64(sceneJson)}\n```\n%%";

    private static string PlainJsonFile(string sceneJson) =>
        $"---\n\nexcalidraw-plugin: parsed\n\n---\n\n# Excalidraw Data\n\n## Text Elements\n\n%%\n## Drawing\n```json\n{sceneJson}\n```\n%%";
}
