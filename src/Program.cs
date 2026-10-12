using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snail.MCP.Vault.Extensions;
using Snail.MCP.Vault.Tools;
using Snail.Sdk.Mcp.Hosting;

var builder = McpHost.CreateBuilder(args);

builder.Services
    .AddVault()
    .AddMcpServer(server => server.ServerInfo = ServerIdentity.Of(typeof(Program).Assembly))
    .WithStdioServerTransport()
    .WithTools<SchemeTools>(ToolJson.Options(VaultJson.Default));

await builder.Build().RunAsync();
