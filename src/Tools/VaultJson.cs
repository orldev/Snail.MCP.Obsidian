using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol;
using Snail.MCP.Vault.Domain;

namespace Snail.MCP.Vault.Tools;

/// <summary>What the tools take and give as JSON, described by generated code rather than found by reflection.</summary>
/// <remarks>
/// A native build cannot look a type up at run time, and neither can this server built any other way: with
/// PublishAot on, reflection-based serialization is off everywhere, so a type missing here fails the same in a test
/// as in the published program — when the tool is listed, not when the build ends.
/// <para>
/// A property with a default — an arrow's side, a node's shape, an empty list — is <c>set</c>, not <c>init</c>: the
/// generated code builds an object with every <c>init</c> property at once, so one the agent left out arrived as
/// null instead of its default, where reflection had kept the default.
/// </para>
/// <para>
/// A tool answers with a concrete type: the SDK serializes a result by the type it is at run time, and the list a
/// collection expression makes is a compiler-made type with two list interfaces, which the generated code refuses
/// to choose between.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(IReadOnlyList<NodeSpec>))]
[JsonSerializable(typeof(IReadOnlyList<EdgeSpec>))]
[JsonSerializable(typeof(IReadOnlyList<DrawBox>))]
[JsonSerializable(typeof(IReadOnlyList<DrawZone>))]
[JsonSerializable(typeof(IReadOnlyList<DrawText>))]
[JsonSerializable(typeof(IReadOnlyList<DrawArrow>))]
[JsonSerializable(typeof(SchemeGraph))]
[JsonSerializable(typeof(VaultStatus))]
[JsonSerializable(typeof(CreatedScheme))]
[JsonSerializable(typeof(DrawnScheme))]
public sealed partial class VaultJson : JsonSerializerContext
{
    private static JsonSerializerOptions? s_options;

    /// <summary>The MCP SDK's own options, with these types put first.</summary>
    /// <remarks>Built on first use: an initializer here could run before the generated <c>Default</c> it reads exists.</remarks>
    public static JsonSerializerOptions ToolOptions => LazyInitializer.EnsureInitialized(ref s_options, Combined);

    private static JsonSerializerOptions Combined()
    {
        var options = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions);

        options.TypeInfoResolverChain.Insert(0, Default);
        options.MakeReadOnly();

        return options;
    }
}

/// <summary>Where the vault is and how many schemes it holds.</summary>
public sealed record VaultStatus(string VaultPath, int SchemeCount);

/// <summary>A scheme laid out by the server.</summary>
public sealed record CreatedScheme(string Path, int NodeCount, int EdgeCount);

/// <summary>A scheme composed by the agent, with what the server noticed about it.</summary>
public sealed record DrawnScheme(string Path, int BoxCount, int ArrowCount, IReadOnlyList<string> Warnings);
