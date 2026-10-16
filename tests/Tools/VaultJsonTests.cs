using System.Text.Json;
using Snail.MCP.Vault.Domain;
using Snail.MCP.Vault.Tools;

namespace Snail.MCP.Vault.Tests.Tools;

/// <summary>A field the agent leaves out keeps the default the type declares, through the generated serializer too.</summary>
public class VaultJsonTests
{
    [Fact]
    public void Deserialize_ArrowWithoutSidesOrLabel_KeepsItsDefaults()
    {
        var arrow = JsonSerializer.Deserialize<IReadOnlyList<DrawArrow>>("""[{"from":"x","to":"y"}]""", Snail.Sdk.Mcp.Hosting.ToolJson.Options(VaultJson.Default))![0];

        Assert.Equal("auto", arrow.FromSide);
        Assert.Equal("auto", arrow.ToSide);
        Assert.Equal("", arrow.Label);
        Assert.Empty(arrow.Bends);
    }

    [Fact]
    public void Deserialize_NodeAndBoxWithoutShape_AreRectangles()
    {
        var node = JsonSerializer.Deserialize<IReadOnlyList<NodeSpec>>("""[{"key":"a","text":"A"}]""", Snail.Sdk.Mcp.Hosting.ToolJson.Options(VaultJson.Default))![0];
        var box = JsonSerializer.Deserialize<IReadOnlyList<DrawBox>>("""[{"key":"a","text":"A","x":0,"y":0}]""", Snail.Sdk.Mcp.Hosting.ToolJson.Options(VaultJson.Default))![0];
        var edge = JsonSerializer.Deserialize<IReadOnlyList<EdgeSpec>>("""[{"from":"a","to":"b"}]""", Snail.Sdk.Mcp.Hosting.ToolJson.Options(VaultJson.Default))![0];

        Assert.Equal("rectangle", node.Shape);
        Assert.Equal("rectangle", box.Shape);
        Assert.Equal("", edge.Label);
    }
}
