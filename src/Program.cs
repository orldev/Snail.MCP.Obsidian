using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snail.MCP.Obsidian.Extensions;
using Snail.MCP.Obsidian.Tools;
using Snail.Sdk.Mcp.Hosting;

var builder = McpHost.CreateBuilder(args);

builder.Services
    .AddVault(builder.Configuration)
    .AddMcpServer(server => server.ServerInfo = ServerIdentity.Of(typeof(Program).Assembly))
    .WithStdioServerTransport()
    .WithTools<SchemeTools>(ToolJson.Options(VaultJson.Default));

await builder.Build().RunAsync();
