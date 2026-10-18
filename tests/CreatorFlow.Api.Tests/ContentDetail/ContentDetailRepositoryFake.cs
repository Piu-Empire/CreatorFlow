using CreatorFlow.Api.Models.ContentDetail;
using CreatorFlow.Api.Repositories.ContentDetail;

namespace CreatorFlow.Api.Tests.ContentDetail;

/// <summary>
/// IContentDetailRepository trong RAM. Script dùng compare-and-set giống câu SQL
/// "UPDATE ... WHERE script IS NOT DISTINCT FROM @expected" của ContentDetailRepository.
/// </summary>
internal sealed class ContentDetailRepositoryFake : IContentDetailRepository
{
    public sealed class Row
    {
        public required ContentDetailRecord Detail { get; set; }
        public string? Script { get; set; }
        public HashSet<string> Published { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private long _nextId = 100;

    public Dictionary<long, Row> Rows { get; } = new();
    public List<string> ActivePlatforms { get; } = new() { "TikTok", "YouTube", "Facebook", "Instagram" };
    public Dictionary<long, List<ActivityRecord>> Activity { get; } = new();

    public NewContent? LastCreated { get; private set; }
    public ContentUpdate? LastUpdate { get; private set; }
    public int UpdateCalls { get; private set; }
    public int ScriptWrites { get; private set; }

    /// <summary>true = người khác vừa sửa script giữa lúc đọc và ghi: lần ghi kế tiếp thất bại (CAS không khớp).</summary>
    public bool FailNextScriptWrite { get; set; }

    public Row Add(long contentId, long projectId, long createdBy, string status = "Script", DateOnly? deadline = null,
        DateOnly? plannedPublishAt = null, string[]? platforms = null, string? script = null, string contentType = "Short video")
    {
        var row = new Row
        {
            Detail = new ContentDetailRecord(contentId, projectId, $"Content {contentId}", "", contentType, status, "Medium",
                deadline, plannedPublishAt, createdBy, (platforms ?? Array.Empty<string>()).OrderBy(p => p, StringComparer.Ordinal).ToArray()),
            Script = script,
        };
        Rows[contentId] = row;
        return row;
    }

    public Task<ContentDetailRecord?> GetDetailAsync(long contentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Rows.TryGetValue(contentId, out Row? row) ? row.Detail : null);

    public Task<IReadOnlyList<string>> GetActivePlatformNamesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(ActivePlatforms.ToList());

    public Task<IReadOnlyList<string>> GetPublishedPlatformNamesAsync(long contentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Rows[contentId].Published.ToList());

    public Task<long> CreateAsync(NewContent content, CancellationToken cancellationToken = default)
    {
        LastCreated = content;
        long id = _nextId++;
        ContentUpdate d = content.Details;
        Rows[id] = new Row
        {
            Detail = new ContentDetailRecord(id, content.ProjectId, d.Title, d.Description, d.ContentType, Pascal(content.StatusDb),
                Pascal(d.PriorityDb), d.Deadline, d.PlannedPublishAt, content.CreatedBy, d.Platforms.OrderBy(p => p, StringComparer.Ordinal).ToArray()),
        };
        return Task.FromResult(id);
    }

    public Task UpdateAsync(long contentId, ContentUpdate update, CancellationToken cancellationToken = default)
    {
        UpdateCalls++;
        LastUpdate = update;
        Row row = Rows[contentId];
        row.Detail = row.Detail with
        {
            Title = update.Title, Description = update.Description, ContentType = update.ContentType, Priority = Pascal(update.PriorityDb),
            Deadline = update.Deadline, PlannedPublishAt = update.PlannedPublishAt,
            Platforms = update.Platforms.OrderBy(p => p, StringComparer.Ordinal).ToArray(),
        };
        return Task.CompletedTask;
    }

    public Task<ScriptRecord?> GetScriptAsync(long contentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Rows.TryGetValue(contentId, out Row? row)
            ? new ScriptRecord(contentId, row.Detail.ProjectId, row.Detail.Status, row.Detail.CreatedBy, row.Script)
            : null);

    public Task<bool> TryUpdateScriptAsync(long contentId, string? expectedRaw, string? newScript, CancellationToken cancellationToken = default)
    {
        Row row = Rows[contentId];
        if (FailNextScriptWrite)
        {
            FailNextScriptWrite = false;
            row.Script = "[người khác vừa sửa]";
            return Task.FromResult(false);
        }

        if (!string.Equals(row.Script, expectedRaw, StringComparison.Ordinal)) return Task.FromResult(false);
        row.Script = newScript;
        ScriptWrites++;
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<ActivityRecord>> GetActivityAsync(long contentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ActivityRecord>>(Activity.GetValueOrDefault(contentId) ?? new List<ActivityRecord>());

    private static string Pascal(string db) => db.Length == 0 ? db : char.ToUpperInvariant(db[0]) + db[1..].ToLowerInvariant();
}
