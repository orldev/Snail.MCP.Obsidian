using System.Text.Json.Nodes;
using Snail.MCP.Vault.Application.Excalidraw.Format;
using Snail.MCP.Vault.Application.Excalidraw.Reading;

namespace Snail.MCP.Vault.Tests.Application.Excalidraw;

public class RealVaultProjectionTests
{
    [Theory]
    [MemberData(nameof(RealVaultParityTests.SchemeFiles), MemberType = typeof(RealVaultParityTests))]
    public void Project_RealSchemeFile_GivesANonEmptyGraph(string file)
    {
        if (file.Length == 0)
            return;

        var document = ExcalidrawCodec.Parse(File.ReadAllText(file));
        var scene = JsonNode.Parse(ExcalidrawCodec.DecodeDrawing(document))!;

        var graph = GraphProjection.Project(scene);

        Assert.True(graph.Nodes.Count + graph.Labels.Count > 0);
    }

    [Theory]
    [MemberData(nameof(RealVaultParityTests.SchemeFiles), MemberType = typeof(RealVaultParityTests))]
    public void Project_RealSchemeFile_BindsEdgesToRealElements(string file)
    {
        if (file.Length == 0)
            return;

        var document = ExcalidrawCodec.Parse(File.ReadAllText(file));
        var scene = JsonNode.Parse(ExcalidrawCodec.DecodeDrawing(document))!;

        var graph = GraphProjection.Project(scene);
        var knownIds = graph.Nodes.Select(n => n.Id)
            .Concat(graph.Edges.Select(e => e.Id))
            .Concat(graph.Labels.Select(l => l.Id))
            .ToHashSet();

        foreach (var edge in graph.Edges)
        {
            if (edge.FromId is { } from)
                Assert.Contains(from, knownIds);

            if (edge.ToId is { } to)
                Assert.Contains(to, knownIds);
        }
    }
}
