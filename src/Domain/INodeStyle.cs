namespace Snail.MCP.Vault.Domain;

/// <summary>Everything that decides how a block looks, shared by laid-out schemes and free drawings.</summary>
public interface INodeStyle
{
    /// <summary>rectangle, diamond or ellipse.</summary>
    string Shape { get; }

    /// <summary>Outline color as hex; <c>null</c> for the default.</summary>
    string? StrokeColor { get; }

    /// <summary>Fill color as hex; <c>null</c> for transparent.</summary>
    string? BackgroundColor { get; }

    /// <summary>How the fill is painted: solid, hachure, cross-hatch or zigzag; <c>null</c> for solid.</summary>
    string? FillStyle { get; }

    /// <summary>Outline style: solid, dashed or dotted; <c>null</c> for solid.</summary>
    string? StrokeStyle { get; }

    /// <summary>Outline width: 1 thin, 2 default, 4 extra bold.</summary>
    int? StrokeWidth { get; }

    /// <summary>Caption color as hex; <c>null</c> for the default.</summary>
    string? TextColor { get; }

    /// <summary>Caption font size in px; <c>null</c> for 20.</summary>
    int? FontSize { get; }

    /// <summary>Caption font by name; <c>null</c> for excalifont.</summary>
    string? Font { get; }
}
