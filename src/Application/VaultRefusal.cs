namespace Snail.MCP.Vault.Application;

/// <summary>A request the vault turns down for a reason the agent can act on; the message tells it what to do instead.</summary>
/// <remarks>
/// The tools carry it to the agent as an MCP error, the one exception whose message the SDK shows: any other reached
/// the agent as "An error occurred invoking 'scheme_create'." and the instruction was lost.
/// </remarks>
public sealed class VaultRefusal(string message) : InvalidOperationException(message);
