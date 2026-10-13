using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Tasks;
using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>My Tasks / progress / deadline chạy qua CreatorFlow.Api (quyền kiểm tra ở Backend).</summary>
public sealed class ApiMyTaskBackend(ApiClient api, UserSession session) : IMyTaskBackend
{
    public List<MyTaskItem> GetMyTasks(long projectId)
    {
        var response = ApiTaskCall.Run(() => api.GetMyTasksAsync(projectId, ApiTaskCall.BearerOf(session)));
        return response.Tasks.Select(ApiTaskMapper.ToItem).ToList();
    }

    public MyTaskItem UpdateProgress(long contentId, int percent) =>
        ApiTaskMapper.ToItem(ApiTaskCall.Run(() =>
            api.UpdateProgressAsync(contentId, new UpdateProgressRequest(percent), ApiTaskCall.BearerOf(session))));

    public MyTaskItem ChangeDeadline(long contentId, long assigneeUserId, DateTime? deadline) =>
        ApiTaskMapper.ToItem(ApiTaskCall.Run(() =>
            api.ChangeDeadlineAsync(contentId, assigneeUserId,
                new ChangeDeadlineRequest(ApiTaskMapper.ToDateOnly(deadline)), ApiTaskCall.BearerOf(session))));

    public bool CanChangeDeadline(long projectId)
    {
        try
        {
            var role = ApiTaskCall.Run(() => api.GetMyProjectRoleAsync(projectId, ApiTaskCall.BearerOf(session)));
            return role.Role is "Owner" or "Manager";
        }
        catch (Exception ex) when (ex is Exceptions.UnauthorizedWorkflowActionException or Exceptions.ContentValidationException)
        {
            return false; // không phải thành viên / không gọi được API: coi như không có quyền, nút bị khoá
        }
    }
}
