using System.Collections;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Snail.MCP.Obsidian.Application;
using Snail.MCP.Obsidian.Configuration;
using Snail.Sdk.Mcp.Hosting;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Obsidian.Tests.Configuration;

/// <summary>The vault root comes from the settings file of Snail.Sdk's chain, and the variable wins over the file.</summary>
public sealed class VaultConfigTests : IDisposable
{
    private readonly string _scratch = TestFolder.Create();

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

    /// <summary>A missing vault is reported, not thrown: thrown, it aborted the server before the client connected, and no reason showed.</summary>
    [Fact]
    public void Path_NowhereSet_IsReportedWithTheInstruction()
    {
        var config = VaultConfig.Load(Settings(null, new Hashtable()));

        var refusal = Assert.Throws<VaultRefusal>(() => config.Root);

        Assert.Null(config.RootPath);
        Assert.Contains(VaultConfig.PathVariable, refusal.Message, StringComparison.Ordinal);
        Assert.Contains(VaultConfig.SettingsFile, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Path_OfAFolderNotThere_IsReportedWithIt()
    {
        var missing = Path.Combine(_scratch, "Missing");

        var config = VaultConfig.Load(Settings(null, new Hashtable { [VaultConfig.PathVariable] = missing }));

        Assert.Null(config.RootPath);
        Assert.Contains(missing, config.Problem, StringComparison.Ordinal);
    }

    /// <summary>The host adds the settings chain itself only for a program that names its server; this one is <c>obsidian</c>.</summary>
    [Fact]
    public void Server_ByItsAssembly_IsNamedForItsSettingsChain() =>
        Assert.Equal("obsidian", ServerSettings.NameOf(typeof(VaultConfig).Assembly));

    private static IConfiguration Settings(string? file, IDictionary variables) =>
        new ConfigurationBuilder().AddServerSettings("obsidian", file, variables).Build();

    private string Vault(string name) => Directory.CreateDirectory(Path.Combine(_scratch, name)).FullName;

    private string SettingsFile(string vault)
    {
        var file = Path.Combine(_scratch, "config.json");

        File.WriteAllText(file, new JsonObject { [VaultConfig.PathKey] = vault }.ToJsonString());

        return file;
    }
}
