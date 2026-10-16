using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Vault.Tests.Support;

/// <summary>Temporary folders of the tests, made inside the test process's own home.</summary>
/// <remarks>
/// The home is named after the test process and deleted when it exits, so a run that crashes leaves folders a cleaner
/// can prove to be leftovers; a folder named at random in the temporary folder proves nothing.
/// </remarks>
internal static class TestFolders
{
    public static string Create(string name) =>
        Directory.CreateDirectory(Path.Combine(ServerProgram.Home, $"{name}-{Guid.NewGuid():N}")).FullName;
}
