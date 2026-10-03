using CreatorFlow.Models.Enums;
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

    public List<ProjectMemberInfo> GetMembers(long projectId)
    {
        using var cmd = _session.CreateCommand(
            "SELECT u.id, u.displayname, pm.role " +
            "FROM projectmembers pm JOIN users u ON u.id = pm.userid " +
            "WHERE pm.projectid = @p ORDER BY u.displayname");
        cmd.Parameters.AddWithValue("p", projectId);

        var list = new List<ProjectMemberInfo>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ProjectMemberInfo
            {
                UserId = reader.GetInt64(0),
                Name = reader.GetString(1),
                Role = Enum.Parse<ProjectRole>(reader.GetString(2)),
            });
        }
        return list;
    }
}
