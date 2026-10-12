using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Snail.MCP.Vault.Tests.Contracts;

/// <summary>The server as a program, started the way a client starts it, against a vault of the test's own.</summary>
/// <remarks>
/// Only the process shows what a native build gets wrong: a parameter or a result type it was not told about fails
/// when the tool is listed or called, not when the program is built. So every tool is called once.
/// SNAIL_VAULT_SERVER names another build to start instead, such as a published native one.
/// </remarks>
public sealed class StdioContractTests : IDisposable
{
    private readonly string _vault = Directory.CreateTempSubdirectory("vault-stdio-tests").FullName;

    private static (string Command, string[] Arguments) Server =>
        Environment.GetEnvironmentVariable("SNAIL_VAULT_SERVER") is { Length: > 0 } native
            ? (native, [])
            : ("dotnet", [Path.Combine(AppContext.BaseDirectory, "Snail.MCP.Vault.dll")]);

    public void Dispose() => Directory.Delete(_vault, recursive: true);

    private Task<McpClient> StartedAsync() =>
        McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = Server.Command,
            Arguments = Server.Arguments,
            EnvironmentVariables = new Dictionary<string, string?> { ["SNAIL_MCP_VAULT_PATH"] = _vault }
        }));

    [Fact]
    public async Task ServerListsItsToolsAsAProgram()
    {
        await using var client = await StartedAsync();

        var tools = await client.ListToolsAsync();

        Assert.Equal(["scheme_create", "scheme_draw", "scheme_list", "scheme_read", "vault_status"], tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task EveryToolTakesItsArgumentsAndAnswers()
    {
        await using var client = await StartedAsync();

        var created = await CalledAsync(client, "scheme_create", new()
        {
            ["path"] = "Excalidraw/Created.md",
            ["nodes"] = new JsonArray(new JsonObject { ["key"] = "a", ["text"] = "A", ["group"] = "One" }, new JsonObject { ["key"] = "b", ["text"] = "B", ["shape"] = "ellipse" }),
            ["edges"] = new JsonArray(new JsonObject { ["from"] = "a", ["to"] = "b", ["label"] = "uses" })
        });
        var drawn = await CalledAsync(client, "scheme_draw", new()
        {
            ["path"] = "Excalidraw/Drawn.md",
            ["boxes"] = new JsonArray(new JsonObject { ["key"] = "x", ["text"] = "X", ["x"] = 0, ["y"] = 0 }, new JsonObject { ["key"] = "y", ["text"] = "Y", ["x"] = 400, ["y"] = 0 }),
            ["arrows"] = new JsonArray(new JsonObject { ["from"] = "x", ["to"] = "y" })
        });
        var read = await CalledAsync(client, "scheme_read", new() { ["path"] = "Excalidraw/Created.md" });
        var listed = await CalledAsync(client, "scheme_list", []);
        var status = await CalledAsync(client, "vault_status", []);

        Assert.Equal("Excalidraw/Created.md", created["path"]!.GetValue<string>());
        Assert.Equal(2, drawn["boxCount"]!.GetValue<int>());
        Assert.Equal(2, read["nodes"]!.AsArray().Count);
        Assert.Contains(listed is JsonArray list ? list : listed["result"]!.AsArray(), path => path!.GetValue<string>() == "Excalidraw/Drawn.md");
        Assert.Equal(2, status["schemeCount"]!.GetValue<int>());
    }

    private static async Task<JsonNode> CalledAsync(McpClient client, string tool, Dictionary<string, object?> arguments)
    {
        var answer = await client.CallToolAsync(tool, arguments);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(answer.Content)).Text;

        Assert.True(answer.IsError != true, $"{tool} failed: {text}");

        return answer.StructuredContent is { } structured ? JsonNode.Parse(structured.GetRawText())! : JsonNode.Parse(text)!;
    }
}
