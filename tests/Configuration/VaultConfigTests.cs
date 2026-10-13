using System.Collections;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Snail.MCP.Vault.Configuration;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Vault.Tests.Configuration;

/// <summary>The vault root comes from the settings file of Snail.Sdk's chain, and the variable wins over the file.</summary>
public sealed class VaultConfigTests : IDisposable
{
    private readonly string _scratch = Directory.CreateTempSubdirectory("vault-config-tests").FullName;

    public void Dispose() => Directory.Delete(_scratch, recursive: true);

    [Fact]
    public void Path_FromTheSettingsFile_IsTheRoot()
    {
        var vault = Vault("FromFile");

        var config = VaultConfig.Load(Settings(SettingsFile(vault), new Hashtable()));

        Assert.Equal(vault, config.RootPath);
    }

    [Fact]
    public void Variable_OverTheSettingsFile_Wins()
    {
        var variables = new Hashtable { [VaultConfig.PathVariable] = Vault("FromVariable") };

        var config = VaultConfig.Load(Settings(SettingsFile(Vault("FromFile")), variables));

        Assert.Equal(Path.Combine(_scratch, "FromVariable"), config.RootPath);
    }

    [Fact]
    public void Path_NowhereSet_StopsTheServer() =>
        Assert.Throws<InvalidOperationException>(() => VaultConfig.Load(Settings(null, new Hashtable())));

    /// <summary>The host adds the settings chain itself only for a program that names its server; this one is <c>vault</c>.</summary>
    [Fact]
    public void Server_ByItsAssembly_IsNamedForItsSettingsChain() =>
        Assert.Equal("vault", ServerSettings.NameOf(typeof(VaultConfig).Assembly));

    private static IConfiguration Settings(string? file, IDictionary variables) =>
        new ConfigurationBuilder().AddServerSettings("vault", file, variables).Build();

    private string Vault(string name) => Directory.CreateDirectory(Path.Combine(_scratch, name)).FullName;

    private string SettingsFile(string vault)
    {
        var file = Path.Combine(_scratch, "config.json");

        File.WriteAllText(file, new JsonObject { [VaultConfig.PathKey] = vault }.ToJsonString());

        return file;
    }
}
