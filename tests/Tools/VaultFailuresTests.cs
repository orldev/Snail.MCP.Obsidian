using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Obsidian.Application;
using Snail.MCP.Obsidian.Tools;
using Snail.Sdk.Mcp.Hosting;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Obsidian.Tests.Tools;

/// <summary>Which failures the server answers as the caller's mistake, and which stay its own.</summary>
/// <remarks>
/// The refusals were named by the BCL's exceptions, and every exception derived from them came with them: a bug that
/// threw ArgumentOutOfRangeException reached the model as "change what the error names" and was never logged, and the
/// system refusing a file was answered "name a path that stays inside the vault".
/// </remarks>
public sealed class VaultFailuresTests
{
    [Theory]
    [InlineData("vault", "refused")]
    [InlineData("drawing", ToolFailures.Arguments)]
    [InlineData("corrupted", "corrupted")]
    [InlineData("escape", "denied")]
    public async Task CallTool_ThrowingARefusalOfTheVault_AnswersItsType(string thrown, string type)
    {
        var answer = await CalledAsync(thrown);

        Assert.True(answer.IsError);
        Assert.Equal(type, answer.Type());
        Assert.NotNull(answer.Error());
    }

    [Theory]
    [InlineData("argument")]
    [InlineData("range")]
    [InlineData("format")]
    [InlineData("access")]
    public async Task CallTool_ThrowingAnExceptionNobodyForesaw_IsTheServersFailure(string thrown)
    {
        var answer = await CalledAsync(thrown);

        Assert.True(answer.IsError);
        Assert.Equal(ToolFailures.Internal, answer.Type());
    }

    private static async Task<CallToolResult> CalledAsync(string thrown)
    {
        await using var server = await InProcessServer.StartAsync(builder => builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .ExplainFailures(VaultFailures.Word)
            .WithTools<Throwing>());
        await using var client = await server.SessionAsync();

        return await client.CallToolAsync("throw", new Dictionary<string, object?> { ["exception"] = thrown });
    }

    /// <summary>A tool that throws the exception the test names.</summary>
    [McpServerToolType]
    public sealed class Throwing
    {
        [McpServerTool(Name = "throw"), Description("Throws the exception the test names.")]
        public static CallToolResult Throw([Description("The exception to throw.")] string exception) =>
            throw (exception switch
            {
                "vault" => new VaultRefusal("refused by the vault"),
                "drawing" => new DrawingRefusal("a value the drawing does not take"),
                "corrupted" => new CorruptedDrawing("a file that is no drawing"),
                "escape" => new VaultEscape("a path out of the vault"),
                "argument" => new ArgumentException("a bug's argument"),
                "range" => new ArgumentOutOfRangeException(nameof(exception), "a bug's index"),
                "format" => new FormatException("a bug's parse"),
                "access" => new UnauthorizedAccessException("the system's refusal"),
                _ => new InvalidOperationException(exception),
            });
    }
}
