using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>Cài đặt Npgsql cho IContentStatusHistoryRepository (bảng content_status_history).</summary>
public class ContentStatusHistoryRepository : IContentStatusHistoryRepository
{
    private readonly IDbSession _session;

    public ContentStatusHistoryRepository(IDbSession session) => _session = session;

    public void Add(ContentStatusHistory history)
    {
        using var cmd = _session.CreateCommand(
            "INSERT INTO content_status_history (content_id, from_status, to_status, changed_by, changed_at, note) " +
            "VALUES (@contentId, @from::content_status, @to::content_status, @changedBy, @changedAt, @note)");
        cmd.Parameters.AddWithValue("contentId", history.ContentId);
        cmd.Parameters.AddWithValue("from", history.FromStatus is { } from
            ? PostgresEnumMapper.ToDatabaseValue(from)
            : DBNull.Value);
        cmd.Parameters.AddWithValue("to", PostgresEnumMapper.ToDatabaseValue(history.ToStatus));
        cmd.Parameters.AddWithValue("changedBy", (object?)history.ChangedByUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("changedAt", history.ChangedAt);
        cmd.Parameters.AddWithValue("note", (object?)history.Note ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}