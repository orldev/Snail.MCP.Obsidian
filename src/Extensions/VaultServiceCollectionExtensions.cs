using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Vault.Adapters.FileSystem;
using Snail.MCP.Vault.Application.Excalidraw;
using Snail.MCP.Vault.Application.Ports;
using Snail.MCP.Vault.Application.Schemes;
using Snail.MCP.Vault.Configuration;

namespace Snail.MCP.Vault.Extensions;

/// <summary>Registration of everything behind the tools.</summary>
public static class VaultServiceCollectionExtensions
{
    /// <summary>Registers the settings, the vault on disk and the scheme catalog.</summary>
    public static IServiceCollection AddVault(this IServiceCollection services) =>
        services
            .AddSingleton(VaultConfig.Load())
            .AddSingleton<IVaultStore, FileSystemVaultStore>()
            .AddSingleton<SceneComposer>()
            .AddSingleton<SceneDrafter>()
            .AddSingleton<SchemeCatalog>();
}
