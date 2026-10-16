using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Obsidian.Tests.Chaos;

/// <summary>A vault of one chaos test and the folder around it, made by TestFolder so that a cleaner can prove it a leftover.</summary>
internal sealed class ChaosVault : IDisposable
{
    public ChaosVault(string name = "Vault")
    {
        Parent = TestFolder.Create();
        Root = Directory.CreateDirectory(Path.Combine(Parent, name)).FullName;
    }

    public string Parent { get; }

    public string Root { get; }

    public void Dispose() => Directory.Delete(Parent, recursive: true);
}
