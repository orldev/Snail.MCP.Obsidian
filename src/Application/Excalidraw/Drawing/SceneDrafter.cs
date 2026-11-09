using System.Text.Json.Nodes;
using Snail.MCP.Obsidian.Application.Excalidraw.Layout;
using Snail.MCP.Obsidian.Domain;

namespace Snail.MCP.Obsidian.Application.Excalidraw.Drawing;

/// <summary>
/// Renders a drawing exactly as composed by the caller: no layout of its own. It only does what the caller
/// cannot — measuring captions, generating ids, binding arrows to blocks — and reports blocks that overlap.
/// </summary>
public sealed class SceneDrafter(Random? random = null)
{
    private const int ZoneTitlePadding = 24;

    private readonly Random _random = random ?? Random.Shared;

    /// <summary>Renders a composed drawing into a scene, with a warning for every overlap.</summary>
    /// <remarks>Zones go in first: they are the ground everything else stands on.</remarks>
    public ComposedScene Draft(DrawSpec spec)
    {
        Validate(spec);

        var factory = new ElementFactory(_random);
        JsonArray elements = [];
        List<SchemeCaption> captions = [];
        Dictionary<string, (JsonObject Element, Rect Rect)> boxes = [];

        foreach (var zone in spec.Zones)
        {
            elements.Add((JsonNode)factory.Plate(
                new Rect(zone.X, zone.Y, zone.Width, zone.Height),
                zone.BackgroundColor ?? "#f8f9fa",
                zone.StrokeColor ?? "#ced4da",
                zone.StrokeStyle ?? "solid"));

            if (zone.Title.Length == 0)
                continue;

            var size = zone.TitleSize ?? ElementFactory.DefaultFontSize;
            var title = factory.Label(zone.Title, zone.X + ZoneTitlePadding, zone.Y + ZoneTitlePadding / 2.0, size, zone.TitleColor ?? "#868e96", "excalifont");
            captions.Add(new SchemeCaption { Text = zone.Title, Id = ElementFactory.Id(title) });
            elements.Add((JsonNode)title);
        }

        foreach (var box in spec.Boxes)
        {
            var measured = StyleRules.Measure(box, box.Text);
            var rect = new Rect(box.X, box.Y, box.Width ?? measured.Width, box.Height ?? measured.Height);

            var shape = factory.Shape(box, rect);
            boxes[box.Key] = (shape, rect);
            elements.Add((JsonNode)shape);

            var caption = factory.Caption(box, box.Text, shape, rect);
            ElementFactory.Bind(shape, caption, "text");
            captions.Add(new SchemeCaption { Text = box.Text, Id = ElementFactory.Id(caption) });
            elements.Add((JsonNode)caption);
        }

        foreach (var text in spec.Texts)
        {
            var label = factory.Label(
                text.Text,
                text.X,
                text.Y,
                text.FontSize ?? ElementFactory.DefaultFontSize,
                text.Color ?? "#1e1e1e",
                text.Font ?? "excalifont");
            captions.Add(new SchemeCaption { Text = text.Text, Id = ElementFactory.Id(label) });
            elements.Add((JsonNode)label);
        }

        foreach (var arrow in spec.Arrows)
        {
            var (fromElement, fromRect) = boxes[arrow.From];
            var (toElement, toRect) = boxes[arrow.To];

            var drawn = factory.Arrow(arrow, fromElement, toElement, Route(arrow, fromRect, toRect));
            ElementFactory.Bind(fromElement, drawn, "arrow");
            ElementFactory.Bind(toElement, drawn, "arrow");
            elements.Add((JsonNode)drawn);

            if (arrow.Label.Length == 0)
                continue;

            var label = factory.ArrowLabel(arrow.Label, drawn);
            ElementFactory.Bind(drawn, label, "text");
            captions.Add(new SchemeCaption { Text = arrow.Label, Id = ElementFactory.Id(label) });
            elements.Add((JsonNode)label);
        }

        return new ComposedScene
        {
            Scene = ElementFactory.Scene(elements),
            Captions = captions,
            Warnings = Overlaps(spec, boxes),
        };
    }

    private static void Validate(DrawSpec spec)
    {
        if (spec.Boxes.Count == 0)
            throw new DrawingRefusal("A drawing needs at least one box.");

        if (spec.Boxes.Count > SchemeLimits.MostNodes || spec.Arrows.Count > SchemeLimits.MostEdges)
            throw new DrawingRefusal($"A drawing takes at most {SchemeLimits.MostNodes} boxes and {SchemeLimits.MostEdges} arrows; split it into several drawings.");

        HashSet<string> keys = [];
        foreach (var box in spec.Boxes)
        {
            if (!keys.Add(box.Key))
                throw new DrawingRefusal($"Box key '{box.Key}' is used twice.");

            StyleRules.ValidateNode(box);
        }

        foreach (var zone in spec.Zones)
        {
            StyleRules.ExpectColor(zone.BackgroundColor, "backgroundColor");
            StyleRules.ExpectColor(zone.StrokeColor, "strokeColor");
            StyleRules.ExpectColor(zone.TitleColor, "titleColor");
            StyleRules.ExpectStrokeStyle(zone.StrokeStyle);
            StyleRules.ExpectFontSize(zone.TitleSize, "titleSize");
        }

        foreach (var text in spec.Texts)
        {
            StyleRules.ExpectColor(text.Color, "color");
            StyleRules.ExpectFont(text.Font);
            StyleRules.ExpectFontSize(text.FontSize, "fontSize");
        }

        foreach (var arrow in spec.Arrows)
        {
            if (!keys.Contains(arrow.From))
                throw new DrawingRefusal($"Arrow refers to unknown box key '{arrow.From}'.");

            if (!keys.Contains(arrow.To))
                throw new DrawingRefusal($"Arrow refers to unknown box key '{arrow.To}'.");

            StyleRules.ExpectSide(arrow.FromSide, "fromSide");
            StyleRules.ExpectSide(arrow.ToSide, "toSide");
            StyleRules.ValidateEdge(arrow);

            foreach (var bend in arrow.Bends)
            {
                if (bend.Count != 2)
                    throw new DrawingRefusal($"A bend of the arrow '{arrow.From}' → '{arrow.To}' must be a pair [x, y].");
            }
        }
    }

    private static EdgeRoute Route(DrawArrow arrow, Rect from, Rect to)
    {
        var bends = arrow.Bends.Select(bend => (X: bend[0], Y: bend[1])).ToList();
        var towardsTo = bends.Count > 0 ? bends[0] : (to.CentreX, to.CentreY);
        var towardsFrom = bends.Count > 0 ? bends[^1] : (from.CentreX, from.CentreY);

        var fromSide = arrow.FromSide == "auto" ? Facing(from, towardsTo) : arrow.FromSide;
        var toSide = arrow.ToSide == "auto" ? Facing(to, towardsFrom) : arrow.ToSide;

        return new EdgeRoute(Port(from, fromSide), Port(to, toSide), bends, Anchor(fromSide), Anchor(toSide));
    }

    /// <summary>Which face of a block looks towards a point — the one the arrow should use.</summary>
    private static string Facing(Rect rect, (double X, double Y) target)
    {
        var dx = target.X - rect.CentreX;
        var dy = target.Y - rect.CentreY;

        if (Math.Abs(dx) * rect.Height >= Math.Abs(dy) * rect.Width)
            return dx >= 0 ? "right" : "left";

        return dy >= 0 ? "bottom" : "top";
    }

    private static (double X, double Y) Port(Rect rect, string side) =>
        side switch
        {
            "left" => (rect.X, rect.CentreY),
            "right" => (rect.Right, rect.CentreY),
            "top" => (rect.CentreX, rect.Y),
            _ => (rect.CentreX, rect.Bottom),
        };

    private static (double X, double Y) Anchor(string side) =>
        side switch
        {
            "left" => (0, 0.5001),
            "right" => (1, 0.5001),
            "top" => (0.5001, 0),
            _ => (0.5001, 1),
        };

    private static IReadOnlyList<string> Overlaps(DrawSpec spec, Dictionary<string, (JsonObject Element, Rect Rect)> boxes)
    {
        List<string> warnings = [];
        for (var i = 0; i < spec.Boxes.Count; i++)
        {
            for (var j = i + 1; j < spec.Boxes.Count; j++)
            {
                var (left, right) = (boxes[spec.Boxes[i].Key].Rect, boxes[spec.Boxes[j].Key].Rect);
                if (left.X < right.Right && right.X < left.Right && left.Y < right.Bottom && right.Y < left.Bottom)
                    warnings.Add($"Boxes '{spec.Boxes[i].Key}' and '{spec.Boxes[j].Key}' overlap.");
            }
        }

        return warnings;
    }
}
