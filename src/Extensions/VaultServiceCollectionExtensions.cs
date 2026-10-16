using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Vault.Adapters.FileSystem;
using Snail.MCP.Vault.Application.Excalidraw.Drawing;
using Snail.MCP.Vault.Application.Ports;
using Snail.MCP.Vault.Application.Schemes;
using Snail.MCP.Vault.Configuration;

namespace Snail.MCP.Vault.Extensions;

/// <summary>Registration of everything behind the tools.</summary>
public static class VaultServiceCollectionExtensions
{
    /// <summary>Registers the settings read from <paramref name="settings"/>, the vault on disk and the scheme catalog.</summary>
    public static IServiceCollection AddVault(this IServiceCollection services, IConfiguration settings) =>
        services
            .AddSingleton(VaultConfig.Load(settings))
            .AddSingleton<IVaultStore, FileSystemVaultStore>()
            .AddSingleton<SceneComposer>()
            .AddSingleton<SceneDrafter>()
            .AddSingleton<SchemeCatalog>();
}
