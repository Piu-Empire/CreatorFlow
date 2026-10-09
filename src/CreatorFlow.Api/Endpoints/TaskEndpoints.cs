using CreatorFlow.Api.Services.Tasks;
using CreatorFlow.Contracts.Tasks;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// My Tasks, tiến độ, deadline và giao việc. Tất cả đều yêu cầu đăng nhập; quyền chi tiết do TaskService kiểm tra.
/// Người thao tác luôn lấy từ JWT, không bao giờ nhận từ body/route.
/// </summary>
public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        // Công việc của người đang đăng nhập trong một Project.
        api.MapGet("/projects/{projectId:long}/my-tasks", async (long projectId, TaskService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetMyTasksAsync(projectId, token)));

        // Vai trò của người đang đăng nhập trong Project.
        api.MapGet("/projects/{projectId:long}/my-role", async (long projectId, TaskService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetMyRoleAsync(projectId, token)));

        // Người có thể chọn làm người nhận việc (Owner/Manager: mọi Creator; Creator: chính mình).
        api.MapGet("/projects/{projectId:long}/assignable-members", async (long projectId, TaskService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetAssignableMembersAsync(projectId, token)));

        // Creator được giao của mọi Content trong Project (hiện avatar trên thẻ Board).
        api.MapGet("/projects/{projectId:long}/board/assignees", async (long projectId, TaskService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetBoardAssigneesAsync(projectId, token)));

        // Mọi Creator đang được giao một Content.
        api.MapGet("/contents/{contentId:long}/assignments", async (long contentId, TaskService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetAssignmentsAsync(contentId, token)));

        // Đặt danh sách Creator được giao (Owner/Manager).
        api.MapPut("/contents/{contentId:long}/assignments", async (long contentId, AssignContentRequest request, TaskService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.AssignContentAsync(contentId, request, token)));

        // Creator cập nhật tiến độ công việc của chính mình.
        api.MapPut("/contents/{contentId:long}/progress", async (long contentId, UpdateProgressRequest request, TaskService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.UpdateProgressAsync(contentId, request, token)));

        // Owner/Manager đổi deadline riêng của một Creator.
        api.MapPut("/contents/{contentId:long}/assignments/{userId:long}/deadline",
            async (long contentId, long userId, ChangeDeadlineRequest request, TaskService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.ChangeDeadlineAsync(contentId, userId, request, token)));
    }
}
