using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>Cài đặt Npgsql cho IProjectMemberRepository (bảng ProjectMembers — mục 5.3, bảng 7).</summary>
public class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly IDbSession _session;

    public ProjectMemberRepository(IDbSession session) => _session = session;

    public ProjectRole? GetRole(long projectId, long userId)
    {
        using var cmd = _session.CreateCommand(
            "SELECT role FROM projectmembers WHERE projectid = @p AND userid = @u");
        cmd.Parameters.AddWithValue("p", projectId);
        cmd.Parameters.AddWithValue("u", userId);

        var result = cmd.ExecuteScalar();
        return result is null ? null : Enum.Parse<ProjectRole>((string)result);
    }
}