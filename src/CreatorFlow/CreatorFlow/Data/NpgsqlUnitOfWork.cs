using CreatorFlow.Repositories.Interfaces;
using Npgsql;

namespace CreatorFlow.Data;

/// <summary>
/// Cài đặt thật (mục 5.2, 5.7) cho IUnitOfWork bằng Npgsql.
/// Mở 1 NpgsqlConnection duy nhất khi khởi tạo (Program.cs) và dùng chung cho toàn bộ
/// Repository trong suốt phiên chạy ứng dụng (đơn giản hoá phù hợp app WinForms 1 người dùng/lần).
/// Begin() mở 1 NpgsqlTransaction; mọi Repository gọi qua cùng session này sẽ tự động
/// chạy trong transaction đó cho tới khi Commit()/Rollback().
/// </summary>
public class NpgsqlUnitOfWork : IUnitOfWork, IDbSession
{
    public NpgsqlConnection Connection { get; }

    public NpgsqlTransaction? Transaction { get; private set; }

    public NpgsqlUnitOfWork(string connectionString)
    {
        Connection = new NpgsqlConnection(connectionString);
        Connection.Open();
    }

    public void Begin()
    {
        if (Transaction != null)
            throw new InvalidOperationException("Đã có 1 transaction đang mở, không thể Begin() lồng nhau.");

        Transaction = Connection.BeginTransaction();
    }

    public void Commit()
    {
        Transaction?.Commit();
        Transaction?.Dispose();
        Transaction = null;
    }

    public void Rollback()
    {
        Transaction?.Rollback();
        Transaction?.Dispose();
        Transaction = null;
    }

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection.Dispose();
        GC.SuppressFinalize(this);
    }
}