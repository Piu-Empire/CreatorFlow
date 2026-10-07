using CreatorFlow.Models.Enums;
using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>Cài đặt Npgsql cho IProjectMemberRepository (bảng project_members).</summary>
public class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly IDbSession _session;

    public ProjectMemberRepository(IDbSession session) => _session = session;

    public ProjectRole? GetRole(long projectId, long userId)
    {
        using var cmd = _session.CreateCommand(
            "SELECT role::text FROM project_members WHERE project_id = @p AND user_id = @u");
        cmd.Parameters.AddWithValue("p", projectId);
        cmd.Parameters.AddWithValue("u", userId);

        var result = cmd.ExecuteScalar();
        return result is null ? null : PostgresEnumMapper.Parse<ProjectRole>((string)result);
    }

    public List<ProjectMemberInfo> GetMembers(long projectId)
    {
        using var cmd = _session.CreateCommand(
            "SELECT u.user_id, u.display_name, pm.role::text " +
            "FROM project_members pm JOIN users u ON u.user_id = pm.user_id " +
            "WHERE pm.project_id = @p ORDER BY u.display_name");
        cmd.Parameters.AddWithValue("p", projectId);

        var list = new List<ProjectMemberInfo>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ProjectMemberInfo
            {
                UserId = reader.GetInt64(0),
                Name = reader.GetString(1),
                Role = PostgresEnumMapper.Parse<ProjectRole>(reader.GetString(2)),
            });
        }
        return list;
    }
}