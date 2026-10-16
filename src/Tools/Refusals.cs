using ModelContextProtocol;
using Snail.MCP.Vault.Application;

namespace Snail.MCP.Vault.Tools;

/// <summary>Carries a refusal the agent can act on across the MCP SDK, which shows the agent the message of its own exception only.</summary>
/// <remarks>
/// A refusal of the vault, an invalid argument with its allowed values, a corrupted file and a path outside the vault
/// all say what to do next; any other exception reached the agent as "An error occurred invoking 'scheme_create'."
/// and nothing more. A CallTool filter cannot do this: the SDK turns the exception into that answer inside the call.
/// </remarks>
internal static class Refusals
{
    public static T Shown<T>(Func<T> call)
    {
        try
        {
            return call();
        }
        catch (Exception refusal) when (refusal is VaultRefusal or ArgumentException or FormatException or UnauthorizedAccessException)
        {
            throw new McpException(refusal.Message, refusal);
        }
    }
}
