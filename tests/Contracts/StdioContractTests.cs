using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Obsidian.Tests.Contracts;

/// <summary>The server as a program, started the way a client starts it, against a vault of the test's own.</summary>
/// <remarks>
/// Only the process shows what a native build gets wrong: a parameter or a result type it was not told about fails
/// when the tool is listed or called, not when the program is built. So every tool is called once.
/// SNAIL_OBSIDIAN_SERVER names another build to start instead, such as a published native one.
/// </remarks>
public sealed class StdioContractTests : IDisposable
{
    private readonly string _vault = TestFolder.Create();

    public void Dispose() => Directory.Delete(_vault, recursive: true);

    private Task<McpClient> StartedAsync(Dictionary<string, string?>? variables = null) =>
        ServerProgram.StartAsync("Snail.MCP.Obsidian", "SNAIL_OBSIDIAN_SERVER", variables ?? new() { ["SNAIL_MCP_OBSIDIAN_PATH"] = _vault });

    [Fact]
    public async Task ListTools_AsAProgram_NamesEveryTool()
    {
        await using var client = await StartedAsync();

        var tools = await client.ListToolsAsync();

        Assert.Equal(["scheme_create", "scheme_draw", "scheme_list", "scheme_read", "vault_status"], tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
    }

    /// <summary>A client knows the server by the command a person types to start it.</summary>
    [Fact]
    public async Task Initialize_AsAProgram_NamesTheServerByItsCommand()
    {
        await using var client = await StartedAsync();

        Assert.NotNull(client.ServerInfo);
        Assert.Equal("s3l-mcp-obsidian", client.ServerInfo.Name);
    }

    /// <summary>
    /// The guard rules of every Snail server, on the list a model receives: names in snake case and listed once, the tool
    /// and every parameter described, read-only or destructive stated, every allowed value named, English.
    /// </summary>
    [Fact]
    public async Task Tools_AsListed_KeepTheGuardRules()
    {
        await using var client = await StartedAsync();

        var listed = await client.ListToolsAsync();

        Assert.Empty(ToolGuards.Problems(listed.Select(tool => tool.ProtocolTool)));
    }

    /// <summary>
    /// A server whose vault is not named starts and answers every tool with how to name one: stopping instead, it
    /// aborted before the client connected, and Claude Code showed no reason.
    /// </summary>
    [Fact]
    public async Task CallTool_WithoutAVault_RefusesWithTheInstruction()
    {
        await using var client = await StartedAsync(new() { ["SNAIL_MCP_OBSIDIAN_PATH"] = null });

        var status = await client.CallToolAsync("vault_status", new Dictionary<string, object?>());
        var listed = await client.CallToolAsync("scheme_list", new Dictionary<string, object?>());

        Assert.True(status.IsError);
        Assert.Contains("SNAIL_MCP_OBSIDIAN_PATH", status.Text(), StringComparison.Ordinal);
        Assert.True(listed.IsError);
        Assert.Contains("SNAIL_MCP_OBSIDIAN_PATH", listed.Text(), StringComparison.Ordinal);
    }

    /// <summary>A vault named only by the settings file, the file named by <c>SNAIL_MCP_OBSIDIAN_CONFIG</c>, is the server's.</summary>
    [Fact]
    public async Task Vault_FromTheSettingsFile_IsServed()
    {
        var settings = TestFolder.Create();

        try
        {
            var file = Path.Combine(settings, "config.json");

            await File.WriteAllTextAsync(file, new JsonObject { ["Path"] = _vault }.ToJsonString());

            await using var client = await StartedAsync(new() { ["SNAIL_MCP_OBSIDIAN_CONFIG"] = file, ["SNAIL_MCP_OBSIDIAN_PATH"] = null });

            var status = await CalledAsync(client, "vault_status", []);

            Assert.Equal(_vault, status["vaultPath"]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(settings, recursive: true);
        }
    }

    [Fact]
    public async Task CallTool_EveryTool_TakesItsArgumentsAndAnswers()
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
