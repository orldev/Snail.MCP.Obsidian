using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Obsidian.Tests.Chaos;

/// <summary>The server as a program, pushed where a test of its classes cannot reach: the MCP SDK between the agent and the code.</summary>
public sealed class ServerChaosTests : IDisposable
{
    private readonly ChaosVault _vault = new();

    public void Dispose() => _vault.Dispose();

    /// <summary>
    /// Breaks the conversation with the agent. The refusal "pass overwrite=true … instead of inventing a new path" is
    /// an InvalidOperationException, and the MCP SDK shows the agent the message of an McpException only: the agent
    /// reads "An error occurred invoking 'scheme_create'." and never the instruction the description promises.
    /// </summary>
    [Fact]
    public async Task SchemeCreate_ExistingPathWithoutOverwrite_TellsTheAgentToPassOverwrite()
    {
        await using var client = await StartedAsync();

        await client.CallToolAsync("scheme_create", Scheme("Excalidraw/Twice.md", 2));
        var second = await client.CallToolAsync("scheme_create", Scheme("Excalidraw/Twice.md", 2));

        Assert.True(second.IsError);
        Assert.Contains("overwrite", second.Text(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Breaks "without overwrite an existing path is refused". Exists and Write are two steps with the whole layout
    /// between them, and the SDK runs requests side by side: several calls for one new path all pass the check, and
    /// all but the last drawing are silently overwritten.
    /// </summary>
    [Fact]
    public async Task SchemeCreate_EightConcurrentCallsWithoutOverwrite_OnlyOneSucceeds()
    {
        await using var client = await StartedAsync();

        var calls = Enumerable.Range(0, 8).Select(_ => client.CallToolAsync("scheme_create", Scheme("Excalidraw/Raced.md", 400)).AsTask());
        var answers = await Task.WhenAll(calls);

        Assert.Equal(1, answers.Count(answer => answer.IsError != true));
    }

    /// <summary>
    /// Breaks the process itself. BackEdges walks the graph by recursion, one stack frame per node of a chain: a long
    /// enough chain overflows the stack, which no catch can stop, and the server dies for every client.
    /// </summary>
    [Fact]
    public async Task SchemeCreate_ChainOfHundredThousandNodes_LeavesTheServerAnswering()
    {
        await using var client = await StartedAsync();
        using var patience = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        await Record.ExceptionAsync(() => client.CallToolAsync("scheme_create", Scheme("Excalidraw/Chain.md", 100_000), cancellationToken: patience.Token).AsTask());
        var afterwards = await Record.ExceptionAsync(() => client.CallToolAsync("vault_status", new Dictionary<string, object?>(), cancellationToken: patience.Token).AsTask());

        Assert.Null(afterwards);
    }

    private Task<McpClient> StartedAsync() =>
        ServerProgram.StartAsync("Snail.MCP.Obsidian", "SNAIL_OBSIDIAN_SERVER", new Dictionary<string, string?> { ["SNAIL_MCP_OBSIDIAN_PATH"] = _vault.Root });

    private static Dictionary<string, object?> Scheme(string path, int chainLength)
    {
        JsonArray nodes = [];
        JsonArray edges = [];
        for (var index = 0; index < chainLength; index++)
        {
            nodes.Add((JsonNode)new JsonObject { ["key"] = $"n{index}", ["text"] = $"N{index}" });
            if (index > 0)
                edges.Add((JsonNode)new JsonObject { ["from"] = $"n{index - 1}", ["to"] = $"n{index}" });
        }

        return new() { ["path"] = path, ["nodes"] = nodes, ["edges"] = edges };
    }
}
