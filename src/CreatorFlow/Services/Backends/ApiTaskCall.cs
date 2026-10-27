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
    public static T Run<T>(Func<Task<T>> call, UserSession? session = null)
    {
        if (session is not null) _ = BearerOf(session);
        long generation = session?.Generation ?? 0;
        try
        {
            T result = Task.Run(call).GetAwaiter().GetResult();
            EnsureCurrentSession(session, generation);
            return result;
        }
        catch (ApiBusinessException ex)
        {
            if (session is not null && generation == session.Generation)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized) session.Clear();
                if (ex.StatusCode == HttpStatusCode.Forbidden) session.NotifyAuthorizationRejected(generation);
            }
            throw Translate(ex);
        }
        catch (ApiException ex)
        {
            throw new ContentValidationException(ex.Message); // mất mạng / quá thời gian chờ: báo cho người dùng
        }
    }

    /// <summary>Bản bất đồng bộ của <see cref="Run{T}"/>: chạy tiếp trên thread giao diện nên không treo form khi chờ API.</summary>
    public static async Task<T> RunAsync<T>(Func<Task<T>> call, UserSession? session = null)
    {
        if (session is not null) _ = BearerOf(session);
        long generation = session?.Generation ?? 0;
        try
        {
            T result = await call();
            EnsureCurrentSession(session, generation);
            return result;
        }
        catch (ApiBusinessException ex)
        {
            if (session is not null && generation == session.Generation)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized) session.Clear();
                if (ex.StatusCode == HttpStatusCode.Forbidden) session.NotifyAuthorizationRejected(generation);
            }
            throw Translate(ex);
        }
        catch (ApiException ex)
        {
            throw new ContentValidationException(ex.Message); // mất mạng / quá thời gian chờ: báo cho người dùng
        }
    }

    public static string BearerOf(UserSession session)
    {
        if (session.IsAuthenticated && !string.IsNullOrEmpty(session.AccessToken)) return session.AccessToken;
        if (session.AccessToken is not null || session.CurrentUser is not null) session.Clear();
        throw new UnauthorizedWorkflowActionException("Vui lòng đăng nhập lại.");
    }

    private static void EnsureCurrentSession(UserSession? session, long generation)
    {
        if (session is not null && generation != session.Generation)
            throw new UnauthorizedWorkflowActionException("Phiên đã thay đổi. Vui lòng thử lại trong phiên hiện tại.");
    }

    private static Exception Translate(ApiBusinessException ex) => ex.Code == "script_conflict" ? new ScriptConflictException() : ex.StatusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new UnauthorizedWorkflowActionException(ex.Message),
        HttpStatusCode.NotFound => new InvalidOperationException(ex.Message),
        _ => new ContentValidationException(ex.Message),
    };
}
