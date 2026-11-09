using Snail.MCP.Obsidian.Application;
using Snail.Sdk.Mcp.Hosting;
using Snail.Sdk.Mcp.Hosting.Answers;

namespace Snail.MCP.Obsidian.Tools;

/// <summary>The words of a failure the vault throws for the caller's mistake, answered by <c>ExplainFailures</c> as the refusal it is.</summary>
/// <remarks>
/// A refusal of the vault, a value the drawing does not take, a file that is no drawing and a path the server may not
/// use each say what to do next in their message. Left to the default, each was answered "the server failed inside, try
/// once more", and the model repeated the call as it was. Anything else stays <c>internal</c>, with the SDK's hint.
/// Each is named by the server's own exception: named by ArgumentException, FormatException and
/// UnauthorizedAccessException, the refusals took in every exception of the BCL derived from them, and a bug or the
/// system's refusal reached the model as its own mistake, unlogged.
/// </remarks>
public static class VaultFailures
{
    /// <summary>Names the vault's refusals by their exception, and leaves the rest to the SDK.</summary>
    public static void Word(FailureOptions failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        failures
            .Refuse<VaultRefusal>("refused")
            .Refuse<DrawingRefusal>(ToolFailures.Arguments, static _ => "change what the error names; the tool's description lists the values and limits each field takes")
            .Refuse<CorruptedDrawing>("corrupted", static _ => "the file is not a drawing this server can read: repair it in Obsidian, or redraw it with overwrite=true")
            .Refuse<VaultEscape>("denied", static _ => "name a vault-relative path that stays inside the vault");
    }
}
