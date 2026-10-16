using System.Text.Json;
using Snail.MCP.Obsidian.Application.Excalidraw.Format;

namespace Snail.MCP.Obsidian.Tests.Application.Excalidraw;

public class RealVaultParityTests
{
    private static readonly string VaultPath =
        Environment.GetEnvironmentVariable("SNAIL_MCP_OBSIDIAN_TEST_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Obsidian Vault");

    public static TheoryData<string> SchemeFiles()
    {
        TheoryData<string> files = [];
        foreach (var folder in new[] { "Excalidraw", "Excalidraw draft" })
        {
            var path = Path.Combine(VaultPath, folder);
            if (!Directory.Exists(path))
                continue;

            foreach (var file in Directory.EnumerateFiles(path, "*.md"))
                files.Add(file);
        }

        if (files.Count == 0)
            files.Add("");

        return files;
    }

    [Theory]
    [MemberData(nameof(SchemeFiles))]
    public void Serialize_RealSchemeFile_ReproducesItByteExact(string file)
    {
        if (file.Length == 0)
            return;

        var text = File.ReadAllText(file);

        Assert.Equal(text, ExcalidrawCodec.Serialize(ExcalidrawCodec.Parse(text)));
    }

    [Theory]
    [MemberData(nameof(SchemeFiles))]
    public void DecodeDrawing_RealSchemeFile_IsAnExcalidrawScene(string file)
    {
        if (file.Length == 0)
            return;

        var document = ExcalidrawCodec.Parse(File.ReadAllText(file));

        using var scene = JsonDocument.Parse(ExcalidrawCodec.DecodeDrawing(document));

        Assert.Equal("excalidraw", scene.RootElement.GetProperty("type").GetString());
        Assert.True(scene.RootElement.GetProperty("elements").GetArrayLength() > 0);
    }

    [Theory]
    [MemberData(nameof(SchemeFiles))]
    public void WithDrawing_UnchangedRealScene_MatchesThePluginBytes(string file)
    {
        if (file.Length == 0)
            return;

        var document = ExcalidrawCodec.Parse(File.ReadAllText(file));
        var scene = ExcalidrawCodec.DecodeDrawing(document);

        var reencoded = ExcalidrawCodec.WithDrawing(document, scene);

        Assert.Equal(document.Drawing, reencoded.Drawing);
    }
}
