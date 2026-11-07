using ContextSketch.Core.Geometry;

namespace ContextSketch.Core;

/// <summary>Word wrapping shared by the on-screen view and the SVG export, so both break text the same way.</summary>
public static class TextWrap
{
    /// <summary>Relationship labels are plain sentences over at most this many lines, the last ending in "…" if cut.</summary>
    public const int LabelMaxLines = 3;

    /// <summary>Label lines are never narrower than this, however short the arrow...</summary>
    public const int LabelMinChars = 12;

    /// <summary>...nor wider than this, however long the arrow.</summary>
    public const int LabelMaxChars = 40;

    public const double LabelFontSize = 12;

    // Rough average character width, and the room kept clear along the arrow: at each end a 12px head plus
    // 20px of visible line, so the arrow still reads as a line either side of the label, plus the white patch.
    private const double LabelCharWidth = LabelFontSize * 0.55;
    private const double LabelInset = 2 * (12 + 20) + 8;

    /// <summary>How many characters fit on one label line along an arrow of this length.</summary>
    public static int LabelCharsFor(double arrowLength) =>
        Math.Clamp((int)((arrowLength - LabelInset) / LabelCharWidth), LabelMinChars, LabelMaxChars);

    /// <summary>A relationship label broken into the lines it is drawn on, fitted to its arrow. Empty when there is no label.</summary>
    public static IReadOnlyList<string> LabelLines(string? label, double arrowLength) =>
        string.IsNullOrWhiteSpace(label) ? [] : Wrap(label, LabelCharsFor(arrowLength), LabelMaxLines);

    /// <summary>The label lines for the visible part of a line from <paramref name="start"/> to <paramref name="end"/>.</summary>
    public static IReadOnlyList<string> LabelLines(string? label, Point start, Point end) =>
        LabelLines(label, Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y)));

    /// <summary>
    /// The shortest arrow, from <paramref name="shortest"/> up to <paramref name="longest"/>, whose label fits without being cut.
    /// Returns <paramref name="longest"/> when even that is not enough.
    /// </summary>
    public static double ArrowLengthFor(string? label, double shortest, double longest)
    {
        if (string.IsNullOrWhiteSpace(label)) return shortest;
        for (var length = shortest; length < longest; length += 10)
        {
            Wrap(label, LabelCharsFor(length), LabelMaxLines, out var cut);
            if (!cut) return length;
        }
        return longest;
    }

    /// <summary>Greedy word wrap; text that does not fit in <paramref name="maxLines"/> is cut with an ellipsis.</summary>
    public static List<string> Wrap(string text, int maxChars, int maxLines) => Wrap(text, maxChars, maxLines, out _);

    private static List<string> Wrap(string text, int maxChars, int maxLines, out bool cut)
    {
        var lines = new List<string>();
        var current = "";
        foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (candidate.Length <= maxChars || current.Length == 0)
                current = candidate;
            else
            {
                lines.Add(current);
                current = word;
            }
        }
        lines.Add(current);

        cut = true;
        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
            lines[^1] = Truncate(lines[^1], maxChars - 1) + "…";
        }
        else if (lines[^1].Length > maxChars)
            lines[^1] = Truncate(lines[^1], maxChars - 1) + "…";
        else
            cut = false;
        return lines;
    }

    private static string Truncate(string s, int length) => s.Length <= length ? s : s[..Math.Max(0, length)];
}
