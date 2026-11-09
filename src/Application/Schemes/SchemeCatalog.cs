using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Snail.MCP.Obsidian.Application.Excalidraw.Drawing;
using Snail.MCP.Obsidian.Application.Excalidraw.Format;
using Snail.MCP.Obsidian.Application.Excalidraw.Reading;
using Snail.MCP.Obsidian.Application.Ports;
using Snail.MCP.Obsidian.Domain;

namespace Snail.MCP.Obsidian.Application.Schemes;

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
    /// <remarks>
    /// Reads the head of each note, where the plugin's marker sits, not the note: ten notes of 2 MB cost 80 MB of
    /// allocations when every note was read whole. A note gone between the enumeration and the read is left out.
    /// </remarks>
    public IReadOnlyList<string> List() =>
        [.. vault.MarkdownFiles().Where(path => vault.ReadHead(path)?.Contains(PluginMarker, StringComparison.Ordinal) == true)];

    /// <summary>A scheme as its logical graph.</summary>
    /// <remarks>Broken JSON in the drawing block threw JsonException, which no refusal names, and a damaged file was answered as the server's failure.</remarks>
    public SchemeGraph Read(string relativePath)
    {
        var document = ExcalidrawCodec.Parse(vault.Read(relativePath));
        var scene = Scene(ExcalidrawCodec.DecodeDrawing(document))
            ?? throw new CorruptedDrawing("The drawing payload is empty.");

        return GraphProjection.Project(scene);
    }

    private static JsonNode? Scene(string drawing)
    {
        try
        {
            return JsonNode.Parse(drawing);
        }
        catch (JsonException broken)
        {
            throw new CorruptedDrawing($"The drawing block is not valid JSON: {broken.Message}", broken);
        }
    }

    /// <summary>Lays out and writes a scheme; returns its path, with <c>.md</c> added when missing.</summary>
    public string Create(string relativePath, SchemeSpec spec, bool overwrite = false)
    {
        var path = SchemePath(relativePath, overwrite);

        var composed = composer.Compose(spec);
        Store(path, composed, overwrite);

        return path;
    }

    /// <summary>Writes a drawing composed by the caller; returns the path and anything worth warning about.</summary>
    public (string Path, IReadOnlyList<string> Warnings) Draw(string relativePath, DrawSpec spec, bool overwrite = false)
    {
        var path = SchemePath(relativePath, overwrite);

        var drafted = drafter.Draft(spec);
        Store(path, drafted, overwrite);

        return (path, drafted.Warnings);
    }

    /// <summary>The file a scheme goes to, with <c>.md</c> added when missing, refused when it names a folder or is taken.</summary>
    /// <remarks>
    /// A path ending in a slash became "folder/.md", a hidden file the listing skips as a service file: the tool
    /// answered with a path scheme_list never showed. The check for a taken path spares a refused call its layout.
    /// </remarks>
    private string SchemePath(string relativePath, bool overwrite)
    {
        var trimmed = relativePath.Trim();
        if (trimmed.Length == 0 || trimmed.EndsWith('/') || trimmed.EndsWith('\\'))
            throw new VaultRefusal($"'{relativePath}' names a folder, not a scheme; name the file, e.g. 'Excalidraw/Payments.md'.");

        var path = trimmed.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? trimmed : $"{trimmed}.md";
        if (!overwrite && vault.Exists(path))
            throw Taken(path);

        return path;
    }

    /// <remarks>
    /// The check made by the file system as it writes is the one that holds when calls for the same path run side by
    /// side; the one before the layout only saves a refused call its work.
    /// </remarks>
    private void Store(string path, ComposedScene scene, bool overwrite)
    {
        if (!vault.TryWrite(path, ComposeMarkdown(scene), overwrite))
            throw Taken(path);
    }

    private static VaultRefusal Taken(string path) =>
        new($"'{path}' already exists; pass overwrite=true to redraw it in place instead of inventing a new path.");

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
}
