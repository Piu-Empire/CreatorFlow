namespace CreatorFlow.Contracts.ContentDetail;

/// <summary>
/// Gộp Activity thô từ 2 nguồn (lịch sử đổi trạng thái + Review) thành một timeline.
/// Dùng chung cho CreatorFlow.Api và client để hai bên không lệch cách hiển thị.
///
/// Mỗi lần gửi duyệt/duyệt/từ chối, DB ghi 2 dòng: một dòng đổi trạng thái và một dòng Review.
/// Compose() ghép chúng thành một sự kiện Review duy nhất, lấy ghi chú (submit) từ dòng trạng thái,
/// nhờ vậy timeline không còn dòng trùng.
/// </summary>
public static class ActivityTimeline
{
    /// <summary>
    /// Cả 2 dòng của cùng một thao tác được ghi trong 1 transaction nên thời gian gần như trùng nhau.
    /// Cho phép lệch tối đa 5 giây để chịu được khác biệt múi giờ/độ chính xác.
    /// </summary>
    private static readonly TimeSpan MatchWindow = TimeSpan.FromSeconds(5);

    public static IReadOnlyList<ActivityItemResponse> Compose(IEnumerable<ActivityItemResponse> entries)
    {
        var all = entries.ToList();
        var reviewEvents = all.Where(IsReview).ToList();
        var statusEvents = all.Where(e => !IsReview(e)).ToList();
        var merged = new List<ActivityItemResponse>(all.Count);
        var statusUsed = new bool[statusEvents.Count];

        foreach (var review in reviewEvents)
        {
            var item = review;
            int match = FindMatchingStatus(review, statusEvents, statusUsed);
            if (match >= 0)
            {
                statusUsed[match] = true;

                // Ghi chú của lần gửi duyệt nằm ở content_status_history.note, không nằm trong reviews.
                if (string.IsNullOrWhiteSpace(item.Feedback) && !string.IsNullOrWhiteSpace(statusEvents[match].Feedback))
                    item = item with { Feedback = statusEvents[match].Feedback };
            }

            merged.Add(item);
        }

        for (int i = 0; i < statusEvents.Count; i++)
            if (!statusUsed[i]) merged.Add(statusEvents[i]);

        return merged.OrderBy(i => i.Timestamp).ToList();
    }

    private static int FindMatchingStatus(
        ActivityItemResponse review, List<ActivityItemResponse> statusEvents, bool[] statusUsed)
    {
        for (int i = 0; i < statusEvents.Count; i++)
        {
            if (statusUsed[i]) continue;

            var status = statusEvents[i];
            if (!string.Equals(status.ActorName, review.ActorName, StringComparison.OrdinalIgnoreCase)) continue;
            if (!SameTransition(status, review)) continue;
            if ((status.Timestamp - review.Timestamp).Duration() > MatchWindow) continue;

            return i;
        }

        return -1;
    }

    /// <summary>Dòng trạng thái có cùng hướng chuyển với sự kiện Review (vào REVIEW khi gửi, ra khỏi REVIEW khi quyết định).</summary>
    private static bool SameTransition(ActivityItemResponse status, ActivityItemResponse review) =>
        review.Kind switch
        {
            ActivityKinds.ReviewSubmitted => IsStatus(status.ToStatus, "Review"),
            ActivityKinds.ReviewDecided =>
                IsStatus(status.FromStatus, "Review") && IsStatus(status.ToStatus, review.ToStatus),
            _ => false,
        };

    private static bool IsReview(ActivityItemResponse item) =>
        item.Kind is ActivityKinds.ReviewSubmitted or ActivityKinds.ReviewDecided;

    private static bool IsStatus(string? value, string? expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
