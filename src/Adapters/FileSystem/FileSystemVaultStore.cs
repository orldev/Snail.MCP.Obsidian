using Snail.MCP.Vault.Application;
using Snail.MCP.Vault.Application.Ports;
using Snail.MCP.Vault.Configuration;

namespace Snail.MCP.Vault.Adapters.FileSystem;

/// <summary>Serves vault files from disk, refusing paths that escape the vault root.</summary>
/// <remarks>
/// The boundary is where a path leads, not how it reads: a symbolic link is followed before the path is compared
/// with the root, itself taken with its links followed. Checked as text, "Linked/Stolen.md" wrote through a link to
/// a folder outside the vault, and a note that was a link to a file outside read that file.
/// </remarks>
public sealed class FileSystemVaultStore(VaultConfig config) : IVaultStore
{
    private const int HeadLength = 512;

    private const int MostLinksFollowed = 40;

    private readonly string? _root = config.RootPath is { } root ? WithSeparator(root) : null;

    private readonly string? _realRoot = config.RootPath is { } root ? WithSeparator(RealPath(Path.GetFullPath(root))) : null;

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

    private bool LeadsInside(string fullPath)
    {
        try
        {
            return WithSeparator(RealPath(fullPath)).StartsWith(RealRoot, StringComparison.Ordinal);
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>The path with every symbolic link along it followed, as far as the path exists.</summary>
    /// <remarks>
    /// Whether a segment is a link is read from the segment itself, which answers null for a path not there yet; only
    /// a link is then followed to its final target. A link that leads round in a circle is refused by the system or by
    /// the count of links followed.
    /// </remarks>
    private static string RealPath(string fullPath, int linksFollowed = 0)
    {
        if (linksFollowed > MostLinksFollowed)
            throw new IOException($"More than {MostLinksFollowed} links lead from '{fullPath}'.");

        var root = Path.GetPathRoot(fullPath)!;
        var segments = fullPath[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var real = root;

        foreach (var segment in segments)
        {
            var next = new FileInfo(Path.Join(real, segment));
            if (next.LinkTarget is null)
            {
                real = next.FullName;
                continue;
            }

            var target = next.ResolveLinkTarget(returnFinalTarget: true)!;
            real = RealPath(target.FullName, linksFollowed + 1);
        }

        return real;
    }

    private static string WithSeparator(string path) =>
        $"{Path.TrimEndingDirectorySeparator(path)}{Path.DirectorySeparatorChar}";
}
