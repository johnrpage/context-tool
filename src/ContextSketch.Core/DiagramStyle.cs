namespace ContextSketch.Core;

/// <summary>Colours and fonts shared by the on-screen view and the SVG export.</summary>
public static class DiagramStyle
{
    public const string FontFamily = "Segoe UI, Arial, sans-serif";

    public const string SystemFill = "#1168BD";
    public const string SystemStroke = "#0B4884";
    public const string SystemText = "#FFFFFF";

    public const string PrimaryFill = "#08427B";
    public const string PrimaryStroke = "#052E56";
    public const string PrimaryText = "#FFFFFF";

    public const string SecondaryFill = "#DCE8F5";
    public const string SecondaryStroke = "#08427B";
    public const string SecondaryText = "#08427B";

    public const string ExternalFill = "#8A8F98";
    public const string ExternalStroke = "#6B7078";
    public const string ExternalText = "#FFFFFF";

    public const string StorageFill = "#438DD5";
    public const string StorageStroke = "#2E6DA9";
    public const string StorageText = "#FFFFFF";

    public const string LineColor = "#52606D";
    public const string LabelText = "#1F2933";
    public const string Background = "#FFFFFF";
    public const string Selection = "#F59E0B";
    public const string Divider = "#D5DAE0";

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
