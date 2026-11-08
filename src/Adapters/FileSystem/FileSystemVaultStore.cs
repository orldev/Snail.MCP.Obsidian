using Snail.MCP.Obsidian.Application;
using Snail.MCP.Obsidian.Application.Ports;
using Snail.MCP.Obsidian.Configuration;
using Snail.Sdk.Mcp.Hosting.Sessions;

namespace Snail.MCP.Obsidian.Adapters.FileSystem;

/// <summary>Serves vault files from disk, refusing paths that escape the vault root.</summary>
/// <remarks>
/// The boundary is where a path leads, not how it reads: every symbolic link on it is followed (RealPath of Snail.Sdk)
/// before the path is compared with the root, itself taken with its links followed. Checked as text,
/// "Linked/Stolen.md" wrote through a link to a folder outside the vault, and a note that was a link to a file outside
/// read that file. A link whose target is not there yet is followed too: taken for the end of the path, a write
/// through it created the file outside.
/// </remarks>
public sealed class FileSystemVaultStore(VaultConfig config) : IVaultStore
{
    private const int HeadLength = 512;

    private readonly string? _root = config.RootPath is { } root ? WithSeparator(root) : null;

    private readonly string? _realRoot = config.RootPath is { } root ? WithSeparator(RealPath.Of(Path.GetFullPath(root)) ?? root) : null;

    /// <remarks>Without a vault every call is refused with the instruction to name one, and the store is still built: a tool is built outside the call that carries a refusal to the agent.</remarks>
    private string Root => _root ?? config.Root;

    private string RealRoot => _realRoot ?? config.Root;

    /// <inheritdoc/>
    /// <remarks>
    /// Links are not walked: a link to a folder outside offered its notes as the vault's, and a link to the vault's own
    /// root inside it listed every note again under Loop/, Loop/Loop/ and on until the system refused the path.
    /// </remarks>
    public IEnumerable<string> MarkdownFiles()
    {
        var enumeration = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
        };

        var root = Root;

        foreach (var file in Directory.EnumerateFiles(root, "*.md", enumeration))
        {
            var relative = Path.GetRelativePath(root, file);
            if (!IsInsideServiceFolder(relative))
                yield return relative.Replace(Path.DirectorySeparatorChar, '/');
        }
    }

    /// <inheritdoc/>
    public string Read(string relativePath)
    {
        try
        {
            return File.ReadAllText(Resolve(relativePath));
        }
        catch (Exception missing) when (missing is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new VaultRefusal($"'{relativePath}' is not in the vault; scheme_list names every scheme.");
        }
    }

    /// <inheritdoc/>
    /// <remarks>A note deleted between the enumeration and this read — by Obsidian, a sync client, the user — is gone, not a failure.</remarks>
    public string? ReadHead(string relativePath)
    {
        Span<char> head = stackalloc char[HeadLength];

        try
        {
            using var reader = new StreamReader(Resolve(relativePath));
            var length = reader.ReadBlock(head);

            return new string(head[..length]);
        }
        catch (Exception missing) when (missing is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public bool Exists(string relativePath) => File.Exists(Resolve(relativePath));

    /// <inheritdoc/>
    /// <remarks>
    /// The text goes into a hidden file beside the target, which is then moved over it: a crash or a full disk
    /// mid-write leaves the old drawing whole. A new path is first claimed by creating it, which the file system lets
    /// exactly one caller do; checked first and written later, eight calls racing for one path all won. The move alone
    /// is no claim: File.Move without overwrite checks and then renames, and sixteen threads racing over 300 paths
    /// left two to five winners on 59 of them.
    /// </remarks>
    public bool TryWrite(string relativePath, string content, bool overwrite)
    {
        var fullPath = Resolve(relativePath);
        var folder = Path.GetDirectoryName(fullPath)!;
        var staged = Path.Combine(folder, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        Directory.CreateDirectory(folder);

        var isClaimed = !overwrite && TryClaim(fullPath);
        if (!overwrite && !isClaimed)
            return false;

        try
        {
            File.WriteAllText(staged, content);
            File.Move(staged, fullPath, overwrite: true);
            isClaimed = false;

            return true;
        }
        finally
        {
            File.Delete(staged);
            if (isClaimed)
                File.Delete(fullPath);
        }
    }

    private static bool TryClaim(string fullPath)
    {
        try
        {
            using var claim = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);

            return true;
        }
        catch (IOException) when (File.Exists(fullPath))
        {
            return false;
        }
    }

    private static bool IsInsideServiceFolder(string relativePath) =>
        relativePath.Split(Path.DirectorySeparatorChar).Any(segment => segment.StartsWith('.'));

    /// <remarks>
    /// The root is compared with its separator: "/vault" alone let "../vault backup/x.md" through, a sibling folder
    /// whose name merely begins with the root's.
    /// </remarks>
    private string Resolve(string relativePath)
    {
        var root = Root;
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(root, StringComparison.Ordinal) || !LeadsInside(fullPath))
            throw new UnauthorizedAccessException($"The path '{relativePath}' escapes the vault root.");

        return fullPath;
    }

    /// <remarks>A path whose links cannot be followed — a loop, more than RealPath.MostLinks of them, one that may not be read — leads nowhere inside.</remarks>
    private bool LeadsInside(string fullPath) =>
        RealPath.Of(fullPath) is { } real && WithSeparator(real).StartsWith(RealRoot, StringComparison.Ordinal);

    private static string WithSeparator(string path) =>
        $"{Path.TrimEndingDirectorySeparator(path)}{Path.DirectorySeparatorChar}";
}
