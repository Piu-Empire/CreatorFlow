namespace CreatorFlow.Contracts.ContentDetail;

/// <summary>
/// Shared sprint labels used by the WinForms client and API. Labels are selected manually and do not map to dates;
/// the default applies to newly created content, while existing content keeps its stored sprint.
/// </summary>
public static class SprintCatalog
{
    public const string DefaultSprint = "Sprint 26";
    private static readonly string[] Values = ["Sprint 24", "Sprint 25", "Sprint 26"];

    public static IReadOnlyList<string> Available { get; } = Array.AsReadOnly(Values);

    public static bool TryNormalize(string? sprint, out string normalized)
    {
        string value = sprint?.Trim() ?? string.Empty;
        normalized = Available.FirstOrDefault(item => item.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        return normalized.Length > 0;
    }
}
