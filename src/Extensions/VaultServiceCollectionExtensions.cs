using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Vault.Adapters.FileSystem;
using Snail.MCP.Vault.Application.Excalidraw;
using Snail.MCP.Vault.Application.Ports;
using Snail.MCP.Vault.Application.Schemes;
using Snail.MCP.Vault.Configuration;

namespace Snail.MCP.Vault.Extensions;

public static class VaultServiceCollectionExtensions
{
    public static IServiceCollection AddVault(this IServiceCollection services) =>
        services
            .AddSingleton(VaultConfig.Load())
            .AddSingleton<IVaultStore, FileSystemVaultStore>()
            .AddSingleton<SceneComposer>()
            .AddSingleton<SceneDrafter>()
            .AddSingleton<SchemeCatalog>();
}
