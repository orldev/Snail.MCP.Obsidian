using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Snail.MCP.Vault.Extensions;
using Snail.MCP.Vault.Tools;

// The empty host: the server reads its one setting itself, so the full builder's appsettings, their watchers, the
// command line and the default loggers would load for nothing. stdout carries JSON-RPC; log lines go to stderr.
var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { Args = args });

builder.Logging.SetMinimumLevel(LogLevel.Information).AddProvider(new StderrLogging());

builder.Services
    .AddVault()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<SchemeTools>(VaultJson.ToolOptions);

await builder.Build().RunAsync();
