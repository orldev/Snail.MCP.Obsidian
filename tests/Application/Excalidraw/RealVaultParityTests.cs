using System.Text.Json;
using Snail.MCP.Obsidian.Application.Excalidraw.Format;

namespace Snail.MCP.Obsidian.Tests.Application.Excalidraw;

public class RealVaultParityTests
{
    /// <summary>The schemes of the vault <see cref="RealVaultTheoryAttribute.Variable"/> names; none when it names none, and the theories are skipped.</summary>
    /// <remarks>A vault named and holding no scheme leaves a theory without data, which xUnit reports as a failure: the vault named is not the one meant.</remarks>
    public static TheoryData<string> SchemeFiles()
    {
        TheoryData<string> files = [];
        if (RealVaultTheoryAttribute.Path is not { } vault)
            return files;

        foreach (var folder in new[] { "Excalidraw", "Excalidraw draft" })
        {
            var path = Path.Combine(vault, folder);
            if (!Directory.Exists(path))
                continue;

            foreach (var file in Directory.EnumerateFiles(path, "*.md"))
                files.Add(file);
        }

        return files;
    }

    [RealVaultTheory]
    [MemberData(nameof(SchemeFiles))]
    public void Serialize_RealSchemeFile_ReproducesItByteExact(string file)
    {
        var text = File.ReadAllText(file);

        Assert.Equal(text, ExcalidrawCodec.Serialize(ExcalidrawCodec.Parse(text)));
    }

    [RealVaultTheory]
    [MemberData(nameof(SchemeFiles))]
    public void DecodeDrawing_RealSchemeFile_IsAnExcalidrawScene(string file)
    {
        var document = ExcalidrawCodec.Parse(File.ReadAllText(file));

        using var scene = JsonDocument.Parse(ExcalidrawCodec.DecodeDrawing(document));

        Assert.Equal("excalidraw", scene.RootElement.GetProperty("type").GetString());
        Assert.True(scene.RootElement.GetProperty("elements").GetArrayLength() > 0);
    }

    [RealVaultTheory]
    [MemberData(nameof(SchemeFiles))]
    public void WithDrawing_UnchangedRealScene_MatchesThePluginBytes(string file)
    {
        var document = ExcalidrawCodec.Parse(File.ReadAllText(file));
        var scene = ExcalidrawCodec.DecodeDrawing(document);

        var reencoded = ExcalidrawCodec.WithDrawing(document, scene);

        Assert.Equal(document.Drawing, reencoded.Drawing);
    }
}
