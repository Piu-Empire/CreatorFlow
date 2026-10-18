namespace CreatorFlow.Contracts.Workflow;

/// <summary>
/// Chuyển trạng thái theo bước đi thẳng:
/// IDEA→SCRIPT, SCRIPT→PRODUCTION, PRODUCTION→EDITING, READY→PUBLISHED.
/// </summary>
public sealed record ChangeContentStatusRequest(string Status, string? Note = null);

/// <summary>Gửi Content đang ở EDITING vào duyệt (EDITING→REVIEW). Tạo Review mới, không ghi đè lần duyệt cũ.</summary>
public sealed record SubmitReviewRequest(string? Note = null);

/// <summary>
/// Chủ Project duyệt (REVIEW→READY) hoặc từ chối (REVIEW→EDITING) Review đang PENDING.
/// Từ chối bắt buộc phải có Feedback.
/// </summary>
public sealed record ReviewDecisionRequest(bool Approve, string? Feedback = null);
