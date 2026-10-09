using System.Net;
using CreatorFlow.ApiClients;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Cầu nối đồng bộ → API cho giao diện hiện còn gọi đồng bộ (sẽ chuyển sang async khi các màn còn lại sang API).
/// Chạy trên thread-pool để không deadlock với SynchronizationContext của WinForms, và đổi lỗi API sang đúng các
/// ngoại lệ nghiệp vụ mà giao diện đã xử lý (ContentValidationException / UnauthorizedWorkflowActionException).
/// </summary>
internal static class ApiTaskCall
{
    public static T Run<T>(Func<Task<T>> call)
    {
        try
        {
            return Task.Run(call).GetAwaiter().GetResult();
        }
        catch (ApiBusinessException ex)
        {
            throw Translate(ex);
        }
        catch (ApiException ex)
        {
            throw new ContentValidationException(ex.Message); // mất mạng / quá thời gian chờ: báo cho người dùng
        }
    }

    public static string BearerOf(UserSession session) =>
        session.IsAuthenticated && !string.IsNullOrEmpty(session.AccessToken)
            ? session.AccessToken
            : throw new UnauthorizedWorkflowActionException("Vui lòng đăng nhập lại.");

    private static Exception Translate(ApiBusinessException ex) => ex.StatusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new UnauthorizedWorkflowActionException(ex.Message),
        HttpStatusCode.NotFound => new InvalidOperationException(ex.Message),
        _ => new ContentValidationException(ex.Message),
    };
}
