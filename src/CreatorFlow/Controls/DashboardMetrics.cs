using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;

namespace CreatorFlow.Controls;

/// <summary>6 chỉ số Dashboard của một Project (đếm thuần, không phụ thuộc UI/DB).</summary>
public sealed record DashboardCounts(int Total, int InProgress, int WaitingReview, int Ready, int Published, int Overdue);

/// <summary>
/// Hàm thuần đếm chỉ số Dashboard từ danh sách card của Project, tách riêng để unit test không cần UI/DB.
/// Nguồn card giống Board/Calendar (đã loại Archived) và Overdue dùng cùng quy tắc với ContentBoardCard.IsOverdue
/// (Published coi như đã xong) để số liệu không lệch với màn Board.
/// </summary>
public static class DashboardMetrics
{
    public static DashboardCounts From(IEnumerable<ContentBoardCard> cards, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(cards);

        int total = 0, inProgress = 0, waitingReview = 0, ready = 0, published = 0, overdue = 0;
        foreach (ContentBoardCard card in cards)
        {
            total++;
            switch (card.Status)
            {
                case ContentStatus.Production:
                case ContentStatus.Editing:
                    inProgress++;
                    break;
                case ContentStatus.Review:
                    waitingReview++;
                    break;
                case ContentStatus.Ready:
                    ready++;
                    break;
                case ContentStatus.Published:
                    published++;
                    break;
            }

            if (TaskRules.IsOverdue(card.Deadline, card.Status == ContentStatus.Published, today))
                overdue++;
        }

        return new DashboardCounts(total, inProgress, waitingReview, ready, published, overdue);
    }
}
