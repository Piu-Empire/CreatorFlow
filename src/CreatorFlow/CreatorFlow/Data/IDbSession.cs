using Npgsql;

namespace CreatorFlow.Data;

/// <summary>
/// Đại diện cho 1 "phiên" kết nối PostgreSQL dùng chung giữa Service và các Repository (mục 5.2, 5.7).
/// NpgsqlUnitOfWork implement interface này để Repository luôn thấy đúng Connection/Transaction
/// hiện hành — kể cả khi WorkflowService đang mở transaction (Begin/Commit/Rollback).
/// Repository KHÔNG tự mở NpgsqlConnection riêng, tránh trường hợp Update Contents.Status
/// và ghi ContentStatusHistory chạy trên 2 connection khác nhau (mất tính transaction).
/// </summary>
public interface IDbSession
{
    NpgsqlConnection Connection { get; }

    /// <summary>Null khi không có transaction nào đang mở (thao tác đọc/ghi đơn lẻ).</summary>
    NpgsqlTransaction? Transaction { get; }
}