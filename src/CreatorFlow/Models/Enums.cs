namespace CreatorFlow.Models;

/// <summary>
/// Trạng thái của Content theo Workflow chuẩn (mục 5.3):
/// IDEA → SCRIPT → PRODUCTION → EDITING → REVIEW → READY → PUBLISHED
/// Bị Reject ở REVIEW thì quay về EDITING.
/// </summary>
public enum ContentStatus
{
    Idea,
    Script,
    Production,
    Editing,
    Review,
    Ready,
    Published
}

/// <summary>
/// Trạng thái của 1 lần Review (mục 5.4, bảng 19 - Reviews).
/// </summary>
public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>
/// Vai trò theo từng Project, lưu tại ProjectMembers (mục 5.3), KHÔNG lưu trong Users.
/// </summary>
public enum ProjectRole
{
    Owner,
    Manager,
    Creator
}