using System.Text.Json.Nodes;
using Snail.MCP.Vault.Application.Excalidraw.Format;
using Snail.MCP.Vault.Application.Excalidraw.Reading;

namespace Snail.MCP.Vault.Tests.Chaos;

public class SceneReadingChaosTests
{
    /// <summary>
    /// Breaks the decoder on a damaged file. The reverse alphabet is an array of 128 entries indexed by the character,
    /// so a single non-ASCII character in the compressed block — an editor's typographic quote, a sync conflict marker —
    /// fails with IndexOutOfRangeException instead of the FormatException every other corruption gives.
    /// </summary>
    [Fact]
    public void DecompressFromBase64_NonAsciiCharacter_FailsAsCorruptedFormat()
    {
        var compressed = LzString.CompressToBase64("""{"type":"excalidraw"}""");
        var damaged = $"{compressed[..1]}…{compressed[1..]}";

        var failure = Record.Exception(() => LzString.DecompressFromBase64(damaged));

        Assert.IsType<FormatException>(failure);
    }

    /// <summary>
    /// Breaks the projection on one foreign element. Text() and Number() read fields tolerantly, but isDeleted and
    /// type are read with GetValue, which throws on a value of another JSON kind: one element written by another tool
    /// hides every other node of the scheme.
    /// </summary>
    [Theory]
    [InlineData("""{"id":"x","type":"rectangle","isDeleted":"false"}""")]
    [InlineData("""{"id":"x","type":7}""")]
    [InlineData("42")]
    public void Project_OneMalformedElement_ProjectsTheOthers(string malformed)
    {
        var scene = JsonNode.Parse($$"""{"elements":[{"id":"a","type":"rectangle","x":0,"y":0,"width":10,"height":10},{{malformed}}]}""")!;

        var projected = Record.Exception(() => GraphProjection.Project(scene));

        Assert.Null(projected);
    }
}
