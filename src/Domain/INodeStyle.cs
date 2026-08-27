namespace Snail.MCP.Vault.Domain;

/// <summary>Everything that decides how a block looks, shared by laid-out schemes and free drawings.</summary>
public interface INodeStyle
{
    string Shape { get; }

    string? StrokeColor { get; }

    string? BackgroundColor { get; }

    string? FillStyle { get; }

    string? StrokeStyle { get; }

    int? StrokeWidth { get; }

    string? TextColor { get; }

    int? FontSize { get; }

    string? Font { get; }
}
