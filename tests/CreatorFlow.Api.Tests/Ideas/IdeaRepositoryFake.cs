using CreatorFlow.Api.Models.Ideas;
using CreatorFlow.Api.Repositories.Ideas;

namespace CreatorFlow.Api.Tests.Ideas;

/// <summary>
/// IIdeaRepository trong RAM, giữ đúng ngữ nghĩa của IdeaRepository (Npgsql):
/// lọc status/tag/search không phân biệt hoa-thường, mới sửa trước; chuyển Content chỉ thành công khi Idea còn Draft/Backlog.
/// </summary>
internal sealed class IdeaRepositoryFake : IIdeaRepository
{
    public static readonly DateTime Start = new(2026, 10, 11, 9, 0, 0, DateTimeKind.Utc);

    private long _nextIdeaId = 1;
    private long _nextContentId = 500;
    private int _clock;

    public List<Idea> Ideas { get; } = new();
    public HashSet<string> ExtraProjectTags { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(long IdeaId, string ContentType, long By, string Note)> Conversions { get; } = new();

    /// <summary>true = người khác vừa chuyển Idea này: ConvertToContentAsync trả null dù trạng thái đọc được vẫn là Draft/Backlog.</summary>
    public bool LoseConvertRace { get; set; }

    public Idea Seed(long projectId, long createdBy, string title = "Idea", IdeaStatus status = IdeaStatus.Draft,
        string description = "", string note = "", params string[] tags)
    {
        var idea = new Idea(_nextIdeaId++, projectId, title, description, note, status, tags, createdBy,
            $"User {createdBy}", Tick(), Tick(), null);
        Ideas.Add(idea);
        return idea;
    }

    public Idea Get(long ideaId) => Ideas.Single(i => i.IdeaId == ideaId);

    public Task<IReadOnlyList<Idea>> GetByProjectAsync(long projectId, IdeaFilter filter, CancellationToken cancellationToken = default)
    {
        string search = (filter.SearchText ?? string.Empty).Trim();
        IEnumerable<Idea> rows = Ideas.Where(i => i.ProjectId == projectId);
        if (filter.Status is { } status) rows = rows.Where(i => i.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Tag)) rows = rows.Where(i => i.Tags.Any(t => t.Equals(filter.Tag.Trim(), StringComparison.OrdinalIgnoreCase)));
        if (search.Length > 0)
            rows = rows.Where(i => Has(i.Title, search) || Has(i.Description, search) || Has(i.Note, search) || i.Tags.Any(t => Has(t, search)));
        return Task.FromResult<IReadOnlyList<Idea>>(rows.OrderByDescending(i => i.UpdatedAt).ThenByDescending(i => i.IdeaId).ToList());
    }

    public Task<Idea?> GetByIdAsync(long ideaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Ideas.FirstOrDefault(i => i.IdeaId == ideaId));

    public Task<IReadOnlyList<string>> GetProjectTagsAsync(long projectId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Ideas.Where(i => i.ProjectId == projectId).SelectMany(i => i.Tags)
            .Concat(ExtraProjectTags).Distinct().OrderBy(t => t, StringComparer.Ordinal).ToList());

    public Task<long> CreateAsync(long projectId, IdeaDraft draft, long createdByUserId, CancellationToken cancellationToken = default)
    {
        Idea idea = Seed(projectId, createdByUserId, draft.Title, draft.Status, draft.Description, draft.Note, draft.Tags.ToArray());
        return Task.FromResult(idea.IdeaId);
    }

    public Task UpdateAsync(long ideaId, IdeaDraft draft, CancellationToken cancellationToken = default)
    {
        int index = Ideas.FindIndex(i => i.IdeaId == ideaId);
        Ideas[index] = Ideas[index] with
        {
            Title = draft.Title, Description = draft.Description, Note = draft.Note, Status = draft.Status,
            Tags = draft.Tags.OrderBy(t => t, StringComparer.Ordinal).ToList(), UpdatedAt = Tick(),
        };
        return Task.CompletedTask;
    }

    public Task<long?> ConvertToContentAsync(long ideaId, string contentType, long convertedByUserId, string historyNote, CancellationToken cancellationToken = default)
    {
        int index = Ideas.FindIndex(i => i.IdeaId == ideaId);
        if (index < 0 || LoseConvertRace || Ideas[index].Status is not (IdeaStatus.Draft or IdeaStatus.Backlog))
            return Task.FromResult<long?>(null);

        long contentId = _nextContentId++;
        Ideas[index] = Ideas[index] with { Status = IdeaStatus.Converted, ConvertedContentId = contentId, UpdatedAt = Tick() };
        Conversions.Add((ideaId, contentType, convertedByUserId, historyNote));
        return Task.FromResult<long?>(contentId);
    }

    public Task DeleteAsync(long ideaId, CancellationToken cancellationToken = default)
    {
        Ideas.RemoveAll(i => i.IdeaId == ideaId);
        return Task.CompletedTask;
    }

    private static bool Has(string text, string search) => text.Contains(search, StringComparison.OrdinalIgnoreCase);

    private DateTime Tick() => Start.AddMinutes(_clock++);
}
