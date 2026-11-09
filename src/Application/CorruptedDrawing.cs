namespace Snail.MCP.Obsidian.Application;

/// <summary>A file that is no drawing this server can read: no drawing block, a block never closed, a damaged compressed stream, broken JSON.</summary>
/// <remarks>
/// It reaches the agent as a failure of type <c>corrupted</c> (Tools/VaultFailures), with what to do about the file.
/// The refusal was named by <see cref="FormatException"/>, which every parse of the BCL throws for a bug too, and broken
/// JSON threw JsonException, which is none: a damaged file was answered "the server failed inside, try once more".
/// </remarks>
public sealed class CorruptedDrawing(string message, Exception? innerException = null) : FormatException(message, innerException);
