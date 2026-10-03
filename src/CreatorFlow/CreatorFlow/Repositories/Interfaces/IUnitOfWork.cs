using System;

namespace CreatorFlow.Repositories.Interfaces;

/// <summary>
/// Đại diện cho 1 transaction dùng chung khi 1 chức năng phải cập nhật
/// nhiều bảng cùng lúc (VD Submit Review: tạo Reviews + update Contents.Status
/// + ghi ContentStatusHistory) — theo mục 5.2 và 5.7.
/// Bản implement thật (Npgsql) sẽ mở NpgsqlTransaction ở đây.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    void Begin();
    void Commit();
    void Rollback();
}