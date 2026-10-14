using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Workflow;
using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>Workflow Review chạy qua CreatorFlow.Api (quyền, trạng thái, transaction kiểm tra ở Backend).</summary>
public sealed class ApiWorkflowBackend(ApiClient api, UserSession session) : IWorkflowBackend
{
    public void SubmitReview(long contentId, string? note) =>
        ApiTaskCall.Run(() => api.SubmitReviewAsync(contentId, new SubmitReviewRequest(note), ApiTaskCall.BearerOf(session)));

    public void DecideReview(long contentId, bool approve, string? feedback) =>
        ApiTaskCall.Run(() => api.DecideReviewAsync(contentId, new ReviewDecisionRequest(approve, feedback), ApiTaskCall.BearerOf(session)));

    public List<ActivityItem> GetActivity(long contentId)
    {
        var history = ApiTaskCall.Run(() => api.GetStatusHistoryAsync(contentId, ApiTaskCall.BearerOf(session)));
        return ToActivity(history.Entries);
    }

    /// <summary>
    /// Dựng Activity theo mẫu UX (mục 6): mỗi lần đổi trạng thái liên quan Review sinh 1 dòng
    /// "submitted/approved/rejected Review #n" (kèm ghi chú/feedback) rồi tới dòng đổi trạng thái.
    /// Số thứ tự Review đếm theo số lần Content vào REVIEW nên mỗi vòng gửi lại là một Review #n mới.
    /// </summary>
    internal static List<ActivityItem> ToActivity(IEnumerable<StatusHistoryEntryResponse> entries)
    {
        var items = new List<ActivityItem>();
        int reviewNo = 0;

        // API trả mới nhất trước → đảo lại thành cũ → mới.
        foreach (var e in entries.OrderBy(x => x.ChangedAt).ThenBy(x => x.HistoryId))
        {
            string actor = string.IsNullOrWhiteSpace(e.ChangedByName) ? "—" : e.ChangedByName!;
            string from = e.FromStatus?.ToUpperInvariant() ?? string.Empty;
            string to = e.ToStatus.ToUpperInvariant();
            string? note = string.IsNullOrWhiteSpace(e.Note) ? null : e.Note;

            if (to == "REVIEW")
            {
                reviewNo++;
                items.Add(new ActivityItem { Timestamp = e.ChangedAt, ActorName = actor, Description = $"submitted Review #{reviewNo}", Feedback = note });
                items.Add(new ActivityItem { Timestamp = e.ChangedAt, ActorName = actor, Description = $"{from} → {to}" });
            }
            else if (from == "REVIEW" && (to == "EDITING" || to == "READY"))
            {
                string verb = to == "READY" ? "approved" : "rejected";
                items.Add(new ActivityItem { Timestamp = e.ChangedAt, ActorName = actor, Description = $"{verb} Review #{reviewNo}", Feedback = note });
                items.Add(new ActivityItem { Timestamp = e.ChangedAt, ActorName = actor, Description = $"{from} → {to}" });
            }
            else
            {
                items.Add(new ActivityItem
                {
                    Timestamp = e.ChangedAt,
                    ActorName = actor,
                    Description = from.Length == 0 ? to : $"{from} → {to}",
                    Feedback = note,
                });
            }
        }
        return items;
    }
}
