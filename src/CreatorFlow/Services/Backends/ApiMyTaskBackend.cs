using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Tasks;
using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>My Tasks / progress / deadline chạy qua CreatorFlow.Api (quyền kiểm tra ở Backend).</summary>
public sealed class ApiMyTaskBackend(ApiClient api, UserSession session) : IMyTaskBackend
{
    private T Run<T>(Func<Task<T>> call) => ApiTaskCall.Run(call, session);

    public List<MyTaskItem> GetMyTasks(long projectId)
    {
        var response = Run(() => api.GetMyTasksAsync(projectId, ApiTaskCall.BearerOf(session)));
        return response.Tasks.Select(ApiTaskMapper.ToItem).ToList();
    }

    public MyTaskItem UpdateProgress(long contentId, int percent) =>
        ApiTaskMapper.ToItem(Run(() =>
            api.UpdateProgressAsync(contentId, new UpdateProgressRequest(percent), ApiTaskCall.BearerOf(session))));

    public MyTaskItem ChangeDeadline(long contentId, long assigneeUserId, DateTime? deadline) =>
        ApiTaskMapper.ToItem(Run(() =>
            api.ChangeDeadlineAsync(contentId, assigneeUserId,
                new ChangeDeadlineRequest(ApiTaskMapper.ToDateOnly(deadline)), ApiTaskCall.BearerOf(session))));

    public bool CanChangeDeadline(long projectId)
    {
        try
        {
            var role = Run(() => api.GetMyProjectRoleAsync(projectId, ApiTaskCall.BearerOf(session)));
            return role.Role is "Owner" or "Manager";
        }
        catch (Exception ex) when (ex is Exceptions.UnauthorizedWorkflowActionException or Exceptions.ContentValidationException)
        {
            return false; // không phải thành viên / không gọi được API: coi như không có quyền, nút bị khoá
        }
    }
}
