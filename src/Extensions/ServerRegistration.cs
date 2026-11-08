using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Snail.MCP.Obsidian.Tools;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Obsidian.Extensions;

/// <summary>What the server says about itself and which tools it offers.</summary>
public static class ServerRegistration
{
    /// <summary>The server's name, version and instructions.</summary>
    public static void Describe(McpServerOptions server)
    {
        ArgumentNullException.ThrowIfNull(server);

        server.ServerInfo = ServerIdentity.Of(typeof(ServerRegistration).Assembly);
        server.ServerInstructions = ToolDescriptions.ServerInstructions;
    }

    extension(IMcpServerBuilder builder)
    {
        /// <summary>Every tool of the server, with the generated JSON of what they take and give; a native build keeps the methods of the classes named here.</summary>
        public IMcpServerBuilder WithServerTools() => builder.WithTools<SchemeTools>(ToolJson.Options(VaultJson.Default));
    }
}
