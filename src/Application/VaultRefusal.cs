namespace Snail.MCP.Obsidian.Application;

/// <summary>A request the vault turns down for a reason the agent can act on; the message tells it what to do instead.</summary>
/// <remarks>
/// It reaches the agent as a failure of type <c>refused</c> in its own words (Tools/VaultFailures, through
/// <c>ExplainFailures</c>): left to the MCP SDK it read "An error occurred invoking 'scheme_create'." and the
/// instruction was lost.
/// </remarks>
public sealed class VaultRefusal(string message) : InvalidOperationException(message);
