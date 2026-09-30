using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

public interface IActivityRepository
{
    /// <summary>
    /// Lấy Activity timeline của 1 Content, đã gộp sẵn từ ContentStatusHistory + Reviews
    /// và sắp xếp theo Timestamp tăng dần.
    /// </summary>
    List<ActivityItem> GetActivity(long contentId);
}