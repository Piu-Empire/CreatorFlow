using CreatorFlow.Models;

namespace CreatorFlow.Repositories.InMemory;

/// <summary>
/// Bản ghi Content trong bộ nhớ, gộp thêm các cột view-model cần cho Board
/// (Priority, Deadline, Platforms, Tags, Assignee) mà Model Content chính thức chưa có,
/// vì DB thật (schema.sql) join từ nhiều bảng ra những cột này.
/// CHỈ dùng tạm khi chưa có PostgreSQL — xoá cả thư mục InMemory/ khi có DB thật.
/// </summary>
public class InMemoryContentRecord
{
    public long Id;
    public long ProjectId;
    public long? IdeaId;
    public string Title = string.Empty;
    public ContentStatus Status;
    public Priority Priority;
    public DateTime? Deadline;
    public List<string> Platforms = new();
    public List<string> Tags = new();
    public long? AssigneeUserId;
    public long CreatedByUserId;
    public DateTime UpdatedAt = DateTime.Now;
}

/// <summary>Dữ liệu demo dùng chung cho mọi InMemory*Repository, sống suốt vòng đời ứng dụng (static).</summary>
public static class InMemoryDataStore
{
    public static readonly Dictionary<long, string> UserNames = new()
    {
        { 1, "Demo Owner" },
        { 2, "Creator A" },
        { 3, "Manager B" },
    };

    public static readonly Dictionary<(long ProjectId, long UserId), ProjectRole> Members = new()
    {
        { (1, 1), ProjectRole.Owner },
        { (1, 2), ProjectRole.Creator },
        { (1, 3), ProjectRole.Manager },
    };

    public static readonly List<InMemoryContentRecord> Contents = new()
    {
        new() { Id = 1, ProjectId = 1, Title = "TikTok trend tháng 10", Status = ContentStatus.Idea, Priority = Priority.High, Deadline = DateTime.Today.AddDays(5), Platforms = { "TikTok" }, AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 2, ProjectId = 1, Title = "Video hướng dẫn sản phẩm", Status = ContentStatus.Script, Priority = Priority.Medium, Deadline = DateTime.Today.AddDays(3), AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 3, ProjectId = 1, Title = "Reel ngắn chủ đề A", Status = ContentStatus.Production, Priority = Priority.High, Deadline = DateTime.Today.AddDays(-1), Platforms = { "Instagram" }, AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 4, ProjectId = 1, Title = "TikTok Launch Campaign", Status = ContentStatus.Editing, Priority = Priority.Low, Deadline = DateTime.Today.AddDays(2), Platforms = { "TikTok", "Instagram" }, AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 5, ProjectId = 1, Title = "YouTube Tutorial", Status = ContentStatus.Review, Priority = Priority.High, Deadline = DateTime.Today.AddDays(1), Platforms = { "YouTube" }, AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 6, ProjectId = 1, Title = "Facebook Campaign", Status = ContentStatus.Ready, Priority = Priority.Medium, Deadline = DateTime.Today.AddDays(7), Platforms = { "Facebook" }, AssigneeUserId = 2, CreatedByUserId = 2 },
    };

    public static readonly List<ContentStatusHistory> StatusHistory = new();

    public static readonly List<Review> Reviews = new()
    {
        new Review { Id = 1, ContentId = 5, ReviewNo = 1, SubmittedByUserId = 2, Status = ReviewStatus.Pending, SubmittedAt = DateTime.Now.AddHours(-2) },
    };

    public static long NextContentStatusHistoryId = 1;
    public static long NextReviewId = 2;
}
