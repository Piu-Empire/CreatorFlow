using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Tasks;
using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>Giao việc cho một hoặc nhiều Creator chạy qua CreatorFlow.Api (quyền, đúng Project, deadline kiểm tra ở Backend).</summary>
public sealed class ApiAssignmentBackend(ApiClient api, UserSession session) : IAssignmentBackend
{
    public List<ProjectMemberInfo> GetAssignableMembers(long projectId)
    {
        var response = ApiTaskCall.Run(() => api.GetAssignableMembersAsync(projectId, ApiTaskCall.BearerOf(session)));
        return response.Members.Select(ApiTaskMapper.ToMember).ToList();
    }

    public List<MyTaskItem> GetAssignments(long contentId)
    {
        var response = ApiTaskCall.Run(() => api.GetAssignmentsAsync(contentId, ApiTaskCall.BearerOf(session)));
        return response.Assignments.Select(ApiTaskMapper.ToItem).ToList();
    }

    public void AssignContent(long contentId, IReadOnlyList<AssignmentRequest> assignments)
    {
        var request = new AssignContentRequest(assignments
            .Select(a => new AssigneeDeadlineRequest(a.UserId, ApiTaskMapper.ToDateOnly(a.Deadline)))
            .ToList());
        ApiTaskCall.Run(() => api.AssignContentAsync(contentId, request, ApiTaskCall.BearerOf(session)));
    }
}
