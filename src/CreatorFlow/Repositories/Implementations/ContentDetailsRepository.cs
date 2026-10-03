using CreatorFlow.Models.Enums;
using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IContentDetailsRepository.
/// TODO: chưa viết vì schema.sql chưa có trong repo (chưa biết cột description/sprint/estimatedduration,
/// cấu trúc bảng contentplatforms / contentassignments). Khi bật UseDatabase = true cần:
///   1) Thêm cột vào contents: description text, sprint text, estimatedduration text.
///   2) Create: INSERT contents + contentplatforms + contentassignments trong cùng transaction của _session.
///   3) Update: UPDATE contents + đồng bộ lại contentplatforms / contentassignments.
/// </summary>
public class ContentDetailsRepository : IContentDetailsRepository
{
    private readonly IDbSession _session;

    public ContentDetailsRepository(IDbSession session) => _session = session;

    public long Create(long projectId, ContentStatus status, ContentDraft draft, long createdByUserId) =>
        throw new NotSupportedException("ContentDetailsRepository (PostgreSQL) chưa được cài đặt - xem TODO trong file.");

    public void Update(long contentId, ContentDraft draft) =>
        throw new NotSupportedException("ContentDetailsRepository (PostgreSQL) chưa được cài đặt - xem TODO trong file.");
}
