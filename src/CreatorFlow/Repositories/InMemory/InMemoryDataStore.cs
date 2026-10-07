using CreatorFlow.Models.Enums;
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
    public string Description = string.Empty;
    public string Sprint = "Sprint 25";
    public string EstimatedDuration = string.Empty;
    public List<string> Platforms = new();
    public List<string> Tags = new();
    public long? AssigneeUserId;
    public long CreatedByUserId;
    public AssignmentStatus AssignmentStatus = AssignmentStatus.Assigned;
    public int ProgressPercent;
    public long? AssignedByUserId;
    public DateTime UpdatedAt = DateTime.Now;
}

/// <summary>
/// Một assignment (Content giao cho một Creator) trong bộ nhớ — tương ứng bảng content_assignments.
/// Mỗi Creator có trạng thái, tiến độ và deadline riêng.
/// </summary>
public class InMemoryAssignmentRecord
{
    public long Id;
    public long ContentId;
    public long AssigneeUserId;
    public long? AssignedByUserId;
    public AssignmentStatus Status = AssignmentStatus.Assigned;
    public int ProgressPercent;
    public DateTime? Deadline;
}

/// <summary>Dữ liệu demo dùng chung cho mọi InMemory*Repository, sống suốt vòng đời ứng dụng (static).</summary>
public static class InMemoryDataStore
{
    public static readonly Dictionary<long, string> UserNames = new()
    {
        { 1, "Demo Owner" },
        { 2, "Creator A" },
        { 3, "Manager B" },
        { 4, "Creator B" },
    };

    public static readonly Dictionary<(long ProjectId, long UserId), ProjectRole> Members = new()
    {
        { (1, 1), ProjectRole.Owner },
        { (1, 2), ProjectRole.Creator },
        { (1, 3), ProjectRole.Manager },
        { (1, 4), ProjectRole.Creator },
    };

    public static readonly List<InMemoryContentRecord> Contents = new()
    {
        new() { Id = 1, ProjectId = 1, AssignmentStatus = AssignmentStatus.Assigned, ProgressPercent = 0, AssignedByUserId = 3, Description = "Trend TikTok tháng 10: cách khai thác âm thanh đang lên xu hướng.", EstimatedDuration = "60 sec", Title = "TikTok trend tháng 10", Status = ContentStatus.Idea, Priority = Priority.High, Deadline = DateTime.Today.AddDays(5), Platforms = { "TikTok" }, AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 2, ProjectId = 1, AssignmentStatus = AssignmentStatus.InProgress, ProgressPercent = 30, AssignedByUserId = 3, Description = "Hướng dẫn nhanh 5 tính năng chính của sản phẩm cho người mới.", EstimatedDuration = "8 min", Title = "Video hướng dẫn sản phẩm", Status = ContentStatus.Script, Priority = Priority.Medium, Deadline = DateTime.Today.AddDays(3), AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 3, ProjectId = 1, AssignmentStatus = AssignmentStatus.InProgress, ProgressPercent = 60, AssignedByUserId = 3, Description = "Reel ngắn chủ đề A, quay dọc, cắt nhanh theo nhịp nhạc.", EstimatedDuration = "30 sec", Title = "Reel ngắn chủ đề A", Status = ContentStatus.Production, Priority = Priority.High, Deadline = DateTime.Today.AddDays(-1), Platforms = { "Instagram" }, AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 4, ProjectId = 1, AssignmentStatus = AssignmentStatus.InProgress, ProgressPercent = 80, AssignedByUserId = 3, Description = "Chiến dịch ra mắt: teaser + video chính cho TikTok và Instagram.", EstimatedDuration = "45 sec", Title = "TikTok Launch Campaign", Status = ContentStatus.Editing, Priority = Priority.Low, Deadline = DateTime.Today.AddDays(2), Platforms = { "TikTok", "Instagram" }, AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 5, ProjectId = 1, AssignmentStatus = AssignmentStatus.InProgress, ProgressPercent = 90, AssignedByUserId = 3, Description = "Video hướng dẫn chi tiết từ A đến Z, có phụ đề và chương mục.", EstimatedDuration = "24 min", Title = "YouTube Tutorial", Status = ContentStatus.Review, Priority = Priority.High, Deadline = DateTime.Today.AddDays(1), Platforms = { "YouTube" }, AssigneeUserId = 2, CreatedByUserId = 2 },
        new() { Id = 6, ProjectId = 1, AssignmentStatus = AssignmentStatus.Completed, ProgressPercent = 100, AssignedByUserId = 3, Description = "Bài đăng chiến dịch Facebook: ảnh + video ngắn kèm CTA.", EstimatedDuration = "2 min", Title = "Facebook Campaign", Status = ContentStatus.Ready, Priority = Priority.Medium, Deadline = DateTime.Today.AddDays(7), Platforms = { "Facebook" }, AssigneeUserId = 2, CreatedByUserId = 2 },
        // --- Task giao cho User #1 (Demo Owner = CurrentSession mặc định) để thử màn My Tasks ---
        new() { Id = 7, ProjectId = 1, Title = "Podcast tập 12 — kịch bản", Description = "Viết kịch bản và outline cho tập podcast số 12.", EstimatedDuration = "35 min", Status = ContentStatus.Script, Priority = Priority.Medium, Deadline = DateTime.Today.AddDays(4), Platforms = { "YouTube" }, AssigneeUserId = 1, CreatedByUserId = 3, AssignedByUserId = 3, AssignmentStatus = AssignmentStatus.InProgress, ProgressPercent = 40 },
        new() { Id = 8, ProjectId = 1, Title = "Banner Facebook tuần lễ ra mắt", Description = "Thiết kế bộ banner cho chiến dịch tuần lễ ra mắt.", EstimatedDuration = "2 min", Status = ContentStatus.Production, Priority = Priority.High, Deadline = DateTime.Today.AddDays(-2), Platforms = { "Facebook" }, AssigneeUserId = 1, CreatedByUserId = 3, AssignedByUserId = 3, AssignmentStatus = AssignmentStatus.InProgress, ProgressPercent = 15 },
        new() { Id = 9, ProjectId = 1, Title = "Reel khẩn: thông báo bảo trì", Description = "Reel ngắn thông báo lịch bảo trì hệ thống, cần đăng trong ngày.", EstimatedDuration = "20 sec", Status = ContentStatus.Editing, Priority = Priority.Urgent, Deadline = DateTime.Today, Platforms = { "Instagram" }, AssigneeUserId = 1, CreatedByUserId = 3, AssignedByUserId = 3, AssignmentStatus = AssignmentStatus.InProgress, ProgressPercent = 70 },
        new() { Id = 10, ProjectId = 1, Title = "Ý tưởng series Behind the scenes", Description = "Đề xuất format và lịch đăng cho series hậu trường.", EstimatedDuration = "10 min", Status = ContentStatus.Idea, Priority = Priority.Low, Platforms = { "YouTube" }, AssigneeUserId = 1, CreatedByUserId = 1, AssignedByUserId = 3, AssignmentStatus = AssignmentStatus.Assigned, ProgressPercent = 0 },
        new() { Id = 11, ProjectId = 1, Title = "Bài tổng kết Q2", Description = "Bài đăng tổng kết kết quả quý 2.", EstimatedDuration = "3 min", Status = ContentStatus.Published, Priority = Priority.Medium, Deadline = DateTime.Today.AddDays(-5), Platforms = { "Facebook" }, AssigneeUserId = 1, CreatedByUserId = 3, AssignedByUserId = 3, AssignmentStatus = AssignmentStatus.Completed, ProgressPercent = 100 },
        // Project #2: cùng User #1 nhưng KHÁC Project → My Tasks của Project #1 không được hiện dòng này.
        new() { Id = 12, ProjectId = 2, Title = "Task thuộc Project khác", Description = "Dùng để kiểm tra lọc theo Project.", Status = ContentStatus.Script, Priority = Priority.High, Deadline = DateTime.Today.AddDays(1), AssigneeUserId = 1, CreatedByUserId = 1, AssignedByUserId = 3, AssignmentStatus = AssignmentStatus.InProgress, ProgressPercent = 50 },
    };

    /// <summary>Assignment đang có (kể cả Cancelled để giữ lịch sử). Khởi tạo từ dữ liệu Contents mẫu phía trên.</summary>
    public static readonly List<InMemoryAssignmentRecord> Assignments = BuildSeedAssignments();

    private static long _nextAssignmentId = 1000;

    public static long NextAssignmentId() => ++_nextAssignmentId;

    private static List<InMemoryAssignmentRecord> BuildSeedAssignments()
    {
        // Mỗi Content mẫu có người phụ trách → 1 assignment (Id = Id Content, dễ đối chiếu).
        var list = Contents
            .Where(c => c.AssigneeUserId.HasValue)
            .Select(c => new InMemoryAssignmentRecord
            {
                Id = c.Id,
                ContentId = c.Id,
                AssigneeUserId = c.AssigneeUserId!.Value,
                AssignedByUserId = c.AssignedByUserId,
                Status = c.AssignmentStatus,
                ProgressPercent = c.ProgressPercent,
                Deadline = c.Deadline,
            })
            .ToList();

        // Demo nhiều Creator: CNT-002 có thêm Creator B với tiến độ và deadline riêng.
        list.Add(new InMemoryAssignmentRecord
        {
            Id = 901,
            ContentId = 2,
            AssigneeUserId = 4,
            AssignedByUserId = 3,
            Status = AssignmentStatus.InProgress,
            ProgressPercent = 10,
            Deadline = DateTime.Today.AddDays(6),
        });
        return list;
    }

    public static readonly List<ContentStatusHistory> StatusHistory = new();

    public static readonly List<Review> Reviews = new()
    {
        new Review { Id = 1, ContentId = 5, ReviewNo = 1, SubmittedByUserId = 2, Status = ReviewStatus.Pending, SubmittedAt = DateTime.Now.AddHours(-2) },
    };

    public static long NextContentStatusHistoryId = 1;
    public static long NextReviewId = 2;
}