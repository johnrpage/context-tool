namespace ContextSketch.Core;

/// <summary>Colours and fonts shared by the on-screen view and the SVG export.</summary>
public static class DiagramStyle
{
    public const string FontFamily = "Segoe UI, Arial, sans-serif";

    // The system: a white box with a heavy ink outline.
    public const string SystemFill = "#FFFFFF";
    public const string SystemStroke = "#0F172A";
    public const string SystemText = "#0F172A";
    public const double SystemStrokeWidth = 3;

    // Everything around it is slate.
    public const string PrimaryFill = "#64748B";
    public const string PrimaryStroke = "#475569";
    public const string PrimaryText = "#FFFFFF";

    public const string SecondaryFill = "#F8FAFC";
    public const string SecondaryStroke = "#64748B";
    public const string SecondaryText = "#334155";

    public const string ExternalFill = "#E2E8F0";
    public const string ExternalStroke = "#94A3B8";
    public const string ExternalText = "#1E293B";

    public const string StorageFill = "#CBD5E1";
    public const string StorageStroke = "#475569";
    public const string StorageText = "#1E293B";

    public const string LineColor = "#52606D";
    public const string LabelText = "#0F172A";
    public const string Background = "#FFFFFF";
    public const string Selection = "#F59E0B";
    public const string Divider = "#CBD5E1";

    /// <summary>An element's technology line is drawn in its text colour at this opacity.</summary>
    public const double TechnologyOpacity = 0.8;

    public static IEnumerable<KeyValuePair<string, string>> All => new Dictionary<string, string>
    {
        [nameof(SystemFill)] = SystemFill, [nameof(SystemStroke)] = SystemStroke, [nameof(SystemText)] = SystemText,
        [nameof(PrimaryFill)] = PrimaryFill, [nameof(PrimaryStroke)] = PrimaryStroke, [nameof(PrimaryText)] = PrimaryText,
        [nameof(SecondaryFill)] = SecondaryFill, [nameof(SecondaryStroke)] = SecondaryStroke, [nameof(SecondaryText)] = SecondaryText,
        [nameof(ExternalFill)] = ExternalFill, [nameof(ExternalStroke)] = ExternalStroke, [nameof(ExternalText)] = ExternalText,
        [nameof(StorageFill)] = StorageFill, [nameof(StorageStroke)] = StorageStroke, [nameof(StorageText)] = StorageText,
        [nameof(LineColor)] = LineColor, [nameof(LabelText)] = LabelText, [nameof(Background)] = Background,
        [nameof(Selection)] = Selection, [nameof(Divider)] = Divider,
    };
}
