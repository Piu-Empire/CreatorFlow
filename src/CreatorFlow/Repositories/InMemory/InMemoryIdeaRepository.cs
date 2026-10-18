using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

/// <summary>
/// Kho ý tưởng trong bộ nhớ, dùng khi chưa có PostgreSQL (UseDatabase = false) và trong test.
/// Mỗi instance có dữ liệu riêng (không dùng chung static) nên các test không ảnh hưởng nhau.
/// Hành vi lọc/tìm kiếm khớp IdeaRepository (Npgsql): theo Project, trạng thái, tag, và từ khóa
/// trong tiêu đề/mô tả/ghi chú/tag không phân biệt hoa thường.
/// </summary>
public class InMemoryIdeaRepository : IIdeaRepository
{
    private readonly List<Idea> _ideas = new();
    private readonly Dictionary<long, HashSet<string>> _projectTags = new();
    private long _nextId = 1;

    /// <param name="withSeed">true: nạp vài Idea mẫu cho Project #1 để xem thử giao diện.</param>
    public InMemoryIdeaRepository(bool withSeed = false)
    {
        if (!withSeed) return;

        AddSeed("Series TikTok CreatorFlow", "Chuỗi video ngắn giới thiệu cách dùng CreatorFlow.", "Tham khảo kênh đối thủ.",
            IdeaStatus.Backlog, new[] { "TikTok", "Short-form" }, 3, DateTime.Now.AddDays(-4));
        AddSeed("Behind the scenes", "Video hậu trường quá trình làm nội dung.", string.Empty,
            IdeaStatus.Draft, new[] { "Marketing" }, 2, DateTime.Now.AddDays(-2));
        AddSeed("Podcast tập 12", "Chủ đề năng suất cho creator.", "Chờ chốt khách mời.",
            IdeaStatus.Backlog, new[] { "Marketing" }, 1, DateTime.Now.AddDays(-1));
        AddSeed("Livestream hỏi đáp", "Buổi livestream trả lời câu hỏi người xem.", string.Empty,
            IdeaStatus.Archived, Array.Empty<string>(), 1, DateTime.Now.AddDays(-9));
    }

    public List<Idea> GetByProject(long projectId, IdeaFilter filter)
    {
        string search = (filter.SearchText ?? string.Empty).Trim();
        string? tag = string.IsNullOrWhiteSpace(filter.Tag) ? null : filter.Tag.Trim();

        return _ideas
            .Where(i => i.ProjectId == projectId)
            .Where(i => !filter.Status.HasValue || i.Status == filter.Status.Value)
            .Where(i => tag is null || i.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            .Where(i => search.Length == 0
                        || Has(i.Title, search) || Has(i.Description, search) || Has(i.Note, search)
                        || i.Tags.Any(t => Has(t, search)))
            .OrderByDescending(i => i.UpdatedAt)
            .ThenByDescending(i => i.IdeaId)
            .Select(Clone)
            .ToList();
    }

    public Idea? GetById(long ideaId)
    {
        var idea = _ideas.FirstOrDefault(i => i.IdeaId == ideaId);
        return idea is null ? null : Clone(idea);
    }

    public List<string> GetProjectTags(long projectId) =>
        _projectTags.TryGetValue(projectId, out var tags)
            ? tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList()
            : new List<string>();

    public long Create(long projectId, IdeaDraft draft, long createdByUserId)
    {
        var now = DateTime.Now;
        var idea = new Idea
        {
            IdeaId = _nextId++,
            ProjectId = projectId,
            Title = draft.Title,
            Description = draft.Description,
            Note = draft.Note,
            Status = draft.Status,
            Tags = draft.Tags.ToList(),
            CreatedByUserId = createdByUserId,
            CreatedByName = NameOf(createdByUserId),
            CreatedAt = now,
            UpdatedAt = now,
        };
        _ideas.Add(idea);
        RegisterTags(projectId, idea.Tags);
        return idea.IdeaId;
    }

    public void Update(long ideaId, IdeaDraft draft)
    {
        var idea = _ideas.First(i => i.IdeaId == ideaId);
        idea.Title = draft.Title;
        idea.Description = draft.Description;
        idea.Note = draft.Note;
        idea.Status = draft.Status;
        idea.Tags = draft.Tags.ToList();
        idea.UpdatedAt = DateTime.Now;
        RegisterTags(idea.ProjectId, idea.Tags);
    }

    public long? ConvertToContent(long ideaId, string contentType, long convertedByUserId)
    {
        var idea = _ideas.FirstOrDefault(i => i.IdeaId == ideaId);
        if (idea is null || idea.Status is not (IdeaStatus.Draft or IdeaStatus.Backlog))
            return null;

        long contentId = InMemoryDataStore.Contents.Count > 0 ? InMemoryDataStore.Contents.Max(c => c.Id) + 1 : 1;
        InMemoryDataStore.Contents.Add(new InMemoryContentRecord
        {
            Id = contentId,
            ProjectId = idea.ProjectId,
            IdeaId = idea.IdeaId,
            Title = idea.Title,
            Description = idea.Description,
            ContentType = contentType,
            Status = ContentStatus.Idea,
            Priority = Priority.Medium,
            Tags = idea.Tags.ToList(),
            CreatedByUserId = convertedByUserId,
            UpdatedAt = DateTime.Now,
        });

        idea.Status = IdeaStatus.Converted;
        idea.ConvertedContentId = contentId;
        idea.UpdatedAt = DateTime.Now;
        return contentId;
    }

    public void Delete(long ideaId) => _ideas.RemoveAll(i => i.IdeaId == ideaId);

    private void AddSeed(string title, string description, string note, IdeaStatus status,
        string[] tags, long createdBy, DateTime updatedAt)
    {
        long id = Create(1, new IdeaDraft
        {
            Title = title,
            Description = description,
            Note = note,
            Status = status,
            Tags = tags.ToList(),
        }, createdBy);
        _ideas.First(i => i.IdeaId == id).UpdatedAt = updatedAt;
    }

    private void RegisterTags(long projectId, IEnumerable<string> tags)
    {
        if (!_projectTags.TryGetValue(projectId, out var set))
            _projectTags[projectId] = set = new HashSet<string>(StringComparer.Ordinal);
        foreach (string tag in tags)
            set.Add(tag);
    }

    private static string NameOf(long userId) =>
        InMemoryDataStore.UserNames.GetValueOrDefault(userId, $"User #{userId}");

    private static bool Has(string? text, string search) =>
        text is not null && text.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static Idea Clone(Idea i) => new()
    {
        IdeaId = i.IdeaId,
        ProjectId = i.ProjectId,
        Title = i.Title,
        Description = i.Description,
        Note = i.Note,
        Status = i.Status,
        Tags = i.Tags.ToList(),
        CreatedByUserId = i.CreatedByUserId,
        CreatedByName = i.CreatedByName,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,
        ConvertedContentId = i.ConvertedContentId,
    };
}