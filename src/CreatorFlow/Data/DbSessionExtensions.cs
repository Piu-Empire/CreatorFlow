using Npgsql;

namespace CreatorFlow.Data;

/// <summary>Tiện ích dùng chung cho mọi Repository Implementation khi tạo lệnh SQL.</summary>
public static class DbSessionExtensions
{
    /// <summary>
    /// Tạo NpgsqlCommand từ Connection hiện hành của session, tự gắn Transaction đang mở (nếu có).
    /// Nhờ vậy Repository không cần biết WorkflowService có đang chạy trong 1 transaction hay không.
    /// </summary>
    public static NpgsqlCommand CreateCommand(this IDbSession session, string sql)
    {
        var cmd = session.Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = session.Transaction;
        return cmd;
    }
}