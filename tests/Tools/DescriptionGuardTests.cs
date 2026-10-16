using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Snail.MCP.Obsidian.Application.Excalidraw.Drawing;
using Snail.MCP.Obsidian.Domain;
using Snail.MCP.Obsidian.Tools;

namespace Snail.MCP.Obsidian.Tests.Tools;

/// <summary>
/// Descriptions are a contract a model obeys, written by hand beside the code: these tests hold every allowed value and
/// every limit they state to the values and limits the code enforces.
/// </summary>
public class DescriptionGuardTests
{
    private static readonly Type[] Specs = [typeof(NodeSpec), typeof(EdgeSpec), typeof(DrawBox), typeof(DrawZone), typeof(DrawText), typeof(DrawArrow)];

    public static TheoryData<Type, string, string[]> OfferedValues() => new()
    {
        { typeof(NodeSpec), nameof(NodeSpec.Shape), [.. StyleVocabulary.Shapes] },
        { typeof(DrawBox), nameof(DrawBox.Shape), [.. StyleVocabulary.Shapes] },
        { typeof(NodeSpec), nameof(NodeSpec.FillStyle), [.. StyleVocabulary.FillStyles] },
        { typeof(DrawBox), nameof(DrawBox.FillStyle), [.. StyleVocabulary.FillStyles] },
        { typeof(NodeSpec), nameof(NodeSpec.StrokeStyle), [.. StyleVocabulary.StrokeStyles] },
        { typeof(DrawBox), nameof(DrawBox.StrokeStyle), [.. StyleVocabulary.StrokeStyles] },
        { typeof(DrawZone), nameof(DrawZone.StrokeStyle), [.. StyleVocabulary.StrokeStyles] },
        { typeof(EdgeSpec), nameof(EdgeSpec.StrokeStyle), [.. StyleVocabulary.StrokeStyles] },
        { typeof(DrawArrow), nameof(DrawArrow.StrokeStyle), [.. StyleVocabulary.StrokeStyles] },
        { typeof(NodeSpec), nameof(NodeSpec.StrokeWidth), [.. StyleVocabulary.StrokeWidths.Select(width => $"{width}")] },
        { typeof(DrawBox), nameof(DrawBox.StrokeWidth), [.. StyleVocabulary.StrokeWidths.Select(width => $"{width}")] },
        { typeof(EdgeSpec), nameof(EdgeSpec.StrokeWidth), [.. StyleVocabulary.StrokeWidths.Select(width => $"{width}")] },
        { typeof(DrawArrow), nameof(DrawArrow.StrokeWidth), [.. StyleVocabulary.StrokeWidths.Select(width => $"{width}")] },
        { typeof(EdgeSpec), nameof(EdgeSpec.StartArrowhead), [.. StyleVocabulary.Arrowheads] },
        { typeof(EdgeSpec), nameof(EdgeSpec.EndArrowhead), [.. StyleVocabulary.Arrowheads] },
        { typeof(DrawArrow), nameof(DrawArrow.StartArrowhead), [.. StyleVocabulary.Arrowheads] },
        { typeof(DrawArrow), nameof(DrawArrow.EndArrowhead), [.. StyleVocabulary.Arrowheads] },
        { typeof(DrawArrow), nameof(DrawArrow.FromSide), [.. StyleVocabulary.Sides] },
        { typeof(DrawArrow), nameof(DrawArrow.ToSide), [.. StyleVocabulary.Sides] },
        { typeof(NodeSpec), nameof(NodeSpec.Font), [.. StyleVocabulary.Fonts] },
        { typeof(DrawBox), nameof(DrawBox.Font), [.. StyleVocabulary.Fonts] },
        { typeof(DrawText), nameof(DrawText.Font), [.. StyleVocabulary.Fonts] },
    };

    public static TheoryData<Type, string> FontSizes() => new()
    {
        { typeof(NodeSpec), nameof(NodeSpec.FontSize) },
        { typeof(DrawBox), nameof(DrawBox.FontSize) },
        { typeof(DrawText), nameof(DrawText.FontSize) },
        { typeof(DrawZone), nameof(DrawZone.TitleSize) },
    };

    public static TheoryData<string, string, string> CountLimits() => new()
    {
        { nameof(SchemeTools.Create), "nodes", $"At most {SchemeLimits.MostNodes} nodes." },
        { nameof(SchemeTools.Create), "edges", $"At most {SchemeLimits.MostEdges} edges." },
        { nameof(SchemeTools.Draw), "boxes", $"At most {SchemeLimits.MostNodes} boxes." },
        { nameof(SchemeTools.Draw), "arrows", $"At most {SchemeLimits.MostEdges} arrows." },
    };

    [Theory]
    [MemberData(nameof(OfferedValues))]
    public void Description_OfAField_NamesEveryValueItAccepts(Type spec, string property, string[] allowed)
    {
        var description = Described(spec.GetProperty(property)!);

        var missing = allowed.Where(value => !Regex.IsMatch(description, $@"(?<![\w-]){Regex.Escape(value)}(?![\w-])")).ToList();

        Assert.True(missing.Count == 0, $"{spec.Name}.{property} does not name {string.Join(", ", missing)}: \"{description}\"");
    }

    [Theory]
    [MemberData(nameof(FontSizes))]
    public void Description_OfAFontSize_StatesTheLimitsInCode(Type spec, string property)
    {
        var description = Described(spec.GetProperty(property)!);

        Assert.Contains($"{SchemeLimits.SmallestFontSize} to {SchemeLimits.LargestFontSize}", description, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(CountLimits))]
    public void Description_OfACollection_StatesTheLimitInCode(string tool, string parameter, string limit)
    {
        var described = typeof(SchemeTools).GetMethod(tool)!.GetParameters().Single(candidate => candidate.Name == parameter);

        Assert.EndsWith(limit, described.GetCustomAttribute<DescriptionAttribute>()!.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Description_OfEdges_StatesTheSkippedColumnLimitInCode()
    {
        var described = typeof(SchemeTools).GetMethod(nameof(SchemeTools.Create))!.GetParameters().Single(candidate => candidate.Name == "edges");

        Assert.Contains($"at most {SchemeLimits.MostSkippedColumns}", described.GetCustomAttribute<DescriptionAttribute>()!.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Specs_EveryProperty_IsDescribed()
    {
        var bare = Specs.SelectMany(spec => spec.GetProperties()).Where(property => property.GetCustomAttribute<DescriptionAttribute>() is null);

        Assert.Empty(bare.Select(property => $"{property.DeclaringType!.Name}.{property.Name}"));
    }

    /// <summary>A font named in the vocabulary but missing from the plugin's font table would fail only when drawn.</summary>
    [Fact]
    public void Compose_EveryFontOfTheVocabulary_IsDrawn()
    {
        var spec = new SchemeSpec { Nodes = [.. StyleVocabulary.Fonts.Select(font => new NodeSpec { Key = font, Text = font, Font = font })] };

        var composed = new SceneComposer(new Random(1)).Compose(spec);

        Assert.NotEmpty(composed.Scene["elements"]!.AsArray());
    }

    private static string Described(PropertyInfo property) =>
        property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "";
}
