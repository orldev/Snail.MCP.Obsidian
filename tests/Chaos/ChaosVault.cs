using Snail.MCP.Vault.Tests.Support;

namespace Snail.MCP.Vault.Tests.Chaos;

/// <summary>A vault folder of one chaos test, inside the test process's own home so that a cleaner can prove it a leftover.</summary>
internal sealed class ChaosVault : IDisposable
{
    public ChaosVault(string name = "Vault")
    {
        Parent = TestFolders.Create("chaos");
        Root = Directory.CreateDirectory(Path.Combine(Parent, name)).FullName;
    }

    public string Parent { get; }

    public string Root { get; }

    public void Dispose() => Directory.Delete(Parent, recursive: true);
}
