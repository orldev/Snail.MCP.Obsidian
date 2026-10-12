using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Snail.MCP.Vault.Application.Excalidraw;
using Snail.MCP.Vault.Application.Ports;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Schemes;

/// <summary>Finds Excalidraw schemes in the vault, serves their logical graphs, and draws new ones.</summary>
public sealed class SchemeCatalog(IVaultStore vault, SceneComposer composer, SceneDrafter drafter)
{
    private const string PluginMarker = "excalidraw-plugin:";

    private static readonly JsonSerializerOptions SceneSerialization = new()
    {
        WriteIndented = true,
        IndentCharacter = '\t',
        IndentSize = 1,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Vault-relative paths of the Markdown files the Excalidraw plugin owns.</summary>
    public IReadOnlyList<string> List() =>
        [.. vault.MarkdownFiles().Where(path => Head(vault.Read(path)).Contains(PluginMarker))];

    /// <summary>A scheme as its logical graph.</summary>
    public SchemeGraph Read(string relativePath)
    {
        var document = ExcalidrawCodec.Parse(vault.Read(relativePath));
        var scene = JsonNode.Parse(ExcalidrawCodec.DecodeDrawing(document))
            ?? throw new FormatException("The drawing payload is empty.");

        return GraphProjection.Project(scene);
    }

    /// <summary>Lays out and writes a scheme; returns its path, with <c>.md</c> added when missing.</summary>
    public string Create(string relativePath, SchemeSpec spec, bool overwrite = false)
    {
        var path = relativePath.EndsWith(".md") ? relativePath : $"{relativePath}.md";
        if (!overwrite && vault.Exists(path))
            throw new InvalidOperationException($"'{path}' already exists; pass overwrite=true to redraw it in place instead of inventing a new path.");

        var composed = composer.Compose(spec);

        vault.Write(path, ComposeMarkdown(composed));

        return path;
    }

    /// <summary>Writes a drawing composed by the caller; returns the path and anything worth warning about.</summary>
    public (string Path, IReadOnlyList<string> Warnings) Draw(string relativePath, DrawSpec spec, bool overwrite = false)
    {
        var path = relativePath.EndsWith(".md") ? relativePath : $"{relativePath}.md";
        if (!overwrite && vault.Exists(path))
            throw new InvalidOperationException($"'{path}' already exists; pass overwrite=true to redraw it in place instead of inventing a new path.");

        var drafted = drafter.Draft(spec);

        vault.Write(path, ComposeMarkdown(drafted));

        return (path, drafted.Warnings);
    }

    private static string ComposeMarkdown(ComposedScene composed)
    {
        var empty = new ExcalidrawDocument
        {
            Prefix = $"{Envelope(composed.Captions)}```compressed-json\n",
            Drawing = "",
            Suffix = "```\n%%",
            Format = DrawingFormat.CompressedJson,
        };

        var document = ExcalidrawCodec.WithDrawing(empty, composed.Scene.ToJsonString(SceneSerialization));

        return ExcalidrawCodec.Serialize(document);
    }

    private static string Envelope(IReadOnlyList<SchemeCaption> captions)
    {
        var text = new StringBuilder(
            "---\n\nexcalidraw-plugin: parsed\ntags: [excalidraw]\n\n---\n" +
            "==⚠  Switch to EXCALIDRAW VIEW in the MORE OPTIONS menu of this document. ⚠== " +
            "You can decompress Drawing data with the command palette: 'Decompress current Excalidraw file'. " +
            "For more info check in plugin settings under 'Saving'\n\n\n" +
            "# Excalidraw Data\n\n## Text Elements\n");

        foreach (var caption in captions)
            text.Append($"{caption.Text} ^{caption.Id}\n\n");

        text.Append("%%\n## Drawing\n");

        return text.ToString();
    }

    private static string Head(string content) =>
        content.Length <= 512 ? content : content[..512];
}
