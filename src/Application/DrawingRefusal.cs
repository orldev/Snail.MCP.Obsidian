namespace Snail.MCP.Obsidian.Application;

/// <summary>A value the drawing does not take — an unknown shape, a color that is none, too many nodes; the message names it and what is allowed.</summary>
/// <remarks>
/// It reaches the agent as a failure of type <c>arguments</c> (Tools/VaultFailures). The refusal was named by
/// <see cref="ArgumentException"/>, and every exception derived from it came along: a bug that threw
/// ArgumentOutOfRangeException was answered as the caller's mistake and never logged. Only this one is the caller's.
/// </remarks>
public sealed class DrawingRefusal(string message) : ArgumentException(message);
