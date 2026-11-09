namespace Snail.MCP.Obsidian.Application;

/// <summary>A path that leads out of the vault, as written or through a link on it.</summary>
/// <remarks>
/// It reaches the agent as a failure of type <c>denied</c> (Tools/VaultFailures). The refusal was named by
/// <see cref="UnauthorizedAccessException"/>, which the system throws too: a file it would not open, or a folder named
/// as a scheme, was answered "name a vault-relative path that stays inside the vault".
/// </remarks>
public sealed class VaultEscape(string message) : UnauthorizedAccessException(message);
