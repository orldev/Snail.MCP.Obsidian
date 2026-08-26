using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Snail.MCP.Vault.Extensions;
using Snail.MCP.Vault.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddVault()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<SchemeTools>();

await builder.Build().RunAsync();
