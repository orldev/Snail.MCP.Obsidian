using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snail.MCP.Obsidian.Tools;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Obsidian.Extensions;

/// <summary>The server as a running host over stdio, the one transport a vault on this machine is reached by.</summary>
public static class ServerHost
{
    /// <summary>Runs the server until the client or the system ends it.</summary>
    public static Task RunAsync(string[] args)
    {
        var builder = McpHost.CreateBuilder(args);

        builder.Services
            .AddVault(builder.Configuration)
            .AddMcpServer(ServerRegistration.Describe)
            .ExplainFailures(VaultFailures.Word)
            .WithStdioServerTransport()
            .WithServerTools();

        return builder.Build().RunAsync();
    }
}
