using System.Text;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>Splits an Excalidraw markdown file into byte-exact segments and converts the drawing payload between its stored form and plain JSON.</summary>
public static class ExcalidrawCodec
{
    private const int CompressedLineWidth = 256;

    /// <summary>Splits a file into the drawing payload and the text around it.</summary>
    public static ExcalidrawDocument Parse(string markdown)
    {
        var fence = FindOpeningFence(markdown);
        var closingStart = FindClosingFence(markdown, fence.BodyStart);

        return new ExcalidrawDocument
        {
            Prefix = markdown[..fence.BodyStart],
            Drawing = markdown[fence.BodyStart..closingStart],
            Suffix = markdown[closingStart..],
            Format = fence.Format,
        };
    }

    /// <summary>Joins the segments back into the file, byte for byte.</summary>
    public static string Serialize(ExcalidrawDocument document) =>
        $"{document.Prefix}{document.Drawing}{document.Suffix}";

    /// <summary>The drawing payload as plain scene JSON, decompressed when stored compressed.</summary>
    public static string DecodeDrawing(ExcalidrawDocument document) =>
        document.Format == DrawingFormat.CompressedJson
            ? LzString.DecompressFromBase64(StripLineBreaks(document.Drawing))
            : document.Drawing;

    /// <summary>The document with a new scene, stored in the format the document already uses.</summary>
    public static ExcalidrawDocument WithDrawing(ExcalidrawDocument document, string drawingJson) =>
        document with { Drawing = EncodeDrawing(drawingJson, document.Format) };

    private static string EncodeDrawing(string drawingJson, DrawingFormat format)
    {
        if (format == DrawingFormat.Json)
            return drawingJson.EndsWith('\n') ? drawingJson : $"{drawingJson}\n";

        var compressed = LzString.CompressToBase64(drawingJson);
        var lines = new StringBuilder(compressed.Length + compressed.Length / CompressedLineWidth * 2 + 1);
        for (var offset = 0; offset < compressed.Length; offset += CompressedLineWidth)
        {
            if (offset > 0)
                lines.Append("\n\n");

            lines.Append(compressed, offset, Math.Min(CompressedLineWidth, compressed.Length - offset));
        }

        lines.Append('\n');

        return lines.ToString();
    }

    private static (int BodyStart, DrawingFormat Format) FindOpeningFence(string markdown)
    {
        List<(int LineStart, int BodyStart, DrawingFormat Format)> candidates = [];
        var lastDrawingHeading = -1;

        foreach (var (lineStart, lineEnd) in Lines(markdown))
        {
            var line = markdown.AsSpan(lineStart, lineEnd - lineStart).TrimEnd("\r\n");
            if (line is "# Drawing" or "## Drawing")
                lastDrawingHeading = lineStart;
            else if (line is "```json")
                candidates.Add((lineStart, lineEnd, DrawingFormat.Json));
            else if (line is "```compressed-json")
                candidates.Add((lineStart, lineEnd, DrawingFormat.CompressedJson));
        }

        foreach (var candidate in candidates)
        {
            if (candidate.LineStart > lastDrawingHeading)
                return (candidate.BodyStart, candidate.Format);
        }

        if (candidates.Count > 0)
            return (candidates[0].BodyStart, candidates[0].Format);

        throw new FormatException("The file has no json or compressed-json drawing block; it is not an Excalidraw markdown file.");
    }

    private static int FindClosingFence(string markdown, int bodyStart)
    {
        foreach (var (lineStart, lineEnd) in Lines(markdown, bodyStart))
        {
            var line = markdown.AsSpan(lineStart, lineEnd - lineStart).TrimEnd("\r\n");
            if (line is "```")
                return lineStart;
        }

        throw new FormatException("The drawing block is never closed; the file is truncated or corrupted.");
    }

    private static IEnumerable<(int Start, int End)> Lines(string text, int from = 0)
    {
        var start = from;
        while (start < text.Length)
        {
            var newline = text.IndexOf('\n', start);
            var end = newline < 0 ? text.Length : newline + 1;
            yield return (start, end);
            start = end;
        }
    }

    private static string StripLineBreaks(string value) =>
        value.Replace("\n", "").Replace("\r", "");
}
