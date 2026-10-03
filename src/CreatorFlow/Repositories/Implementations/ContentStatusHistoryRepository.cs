using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>Cài đặt Npgsql cho IContentStatusHistoryRepository (mục 5.4 bảng 18).</summary>
public class ContentStatusHistoryRepository : IContentStatusHistoryRepository
{
    private readonly IDbSession _session;

    public ContentStatusHistoryRepository(IDbSession session) => _session = session;

    public void Add(ContentStatusHistory history)
    {
        using var cmd = _session.CreateCommand(
            "INSERT INTO contentstatushistory (contentid, fromstatus, tostatus, changedbyuserid, changedat, note) " +
            "VALUES (@contentId, @from, @to, @changedBy, @changedAt, @note)");
        cmd.Parameters.AddWithValue("contentId", history.ContentId);
        cmd.Parameters.AddWithValue("from", history.FromStatus.ToString());
        cmd.Parameters.AddWithValue("to", history.ToStatus.ToString());
        cmd.Parameters.AddWithValue("changedBy", history.ChangedByUserId);
        cmd.Parameters.AddWithValue("changedAt", history.ChangedAt);
        cmd.Parameters.AddWithValue("note", (object?)history.Note ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}