using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Obsidian.Adapters.FileSystem;
using Snail.MCP.Obsidian.Application.Excalidraw.Drawing;
using Snail.MCP.Obsidian.Application.Ports;
using Snail.MCP.Obsidian.Application.Schemes;
using Snail.MCP.Obsidian.Configuration;

namespace Snail.MCP.Obsidian.Extensions;

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
