using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CreatorFlow.ApiClients;

public sealed partial class ApiClient
{
    public Task<T> SendJsonAsync<T>(HttpMethod method, string path, object? body, string? bearer = null, CancellationToken token = default) =>
        SendTypedAsync<T>(new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body, options: JsonOptions) }, bearer, token);

    public Task<T> SendMultipartAsync<T>(string path, MultipartFormDataContent content, string bearer, CancellationToken token = default) =>
        SendTypedAsync<T>(new HttpRequestMessage(HttpMethod.Put, path) { Content = content }, bearer, token);

    private async Task<T> SendTypedAsync<T>(HttpRequestMessage request, string? bearer, CancellationToken token)
    {
        using (request)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(httpClient.Timeout);
            if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            try
            {
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                byte[] data = await ReadBoundedAsync(response.Content, 65536, deadline.Token);
                if (!response.IsSuccessStatusCode) throw CreateBusinessError(response.StatusCode, data);
                T? result = JsonSerializer.Deserialize<T>(data, JsonOptions);
                return result ?? throw new ApiException(ApiErrorKind.InvalidResponse, "API trả dữ liệu không hợp lệ.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (OperationCanceledException) { throw new ApiException(ApiErrorKind.Timeout, "API phản hồi quá thời gian chờ."); }
            catch (HttpRequestException) { throw new ApiException(ApiErrorKind.Network, "Không kết nối được API."); }
            catch (JsonException) { throw new ApiException(ApiErrorKind.InvalidResponse, "API trả dữ liệu không hợp lệ."); }
        }
    }

    public async Task<byte[]?> GetAvatarAsync(string bearer, CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/users/me/avatar");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(httpClient.Timeout);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode) throw CreateBusinessError(response.StatusCode, await ReadBoundedAsync(response.Content, 65536, deadline.Token));
            if (response.Content.Headers.ContentType?.MediaType != "image/png")
                throw new ApiException(ApiErrorKind.InvalidResponse, "API trả ảnh không hợp lệ.");
            return await ReadBoundedAsync(response.Content, 2097152, deadline.Token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { throw new ApiException(ApiErrorKind.Timeout, "API phản hồi quá thời gian chờ."); }
        catch (HttpRequestException) { throw new ApiException(ApiErrorKind.Network, "Không kết nối được API."); }
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpContent content, int maximum, CancellationToken token)
    {
        if (content.Headers.ContentLength > maximum) throw new ApiException(ApiErrorKind.InvalidResponse, "API trả dữ liệu vượt giới hạn.");
        await using var source = await content.ReadAsStreamAsync(token);
        using var destination = new MemoryStream();
        byte[] buffer = new byte[8192];
        int read;
        while ((read = await source.ReadAsync(buffer, token)) > 0)
        {
            if (destination.Length + read > maximum) throw new ApiException(ApiErrorKind.InvalidResponse, "API trả dữ liệu vượt giới hạn.");
            destination.Write(buffer, 0, read);
        }
        return destination.ToArray();
    }

    private static ApiBusinessException CreateBusinessError(HttpStatusCode status, byte[] body)
    {
        string code = "http_error", message = GetErrorMessage(status);
        string? field = null;
        Guid? requestId = null;
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new(status, code, message);
            if (root.TryGetProperty("code", out var codeValue) && codeValue.ValueKind == JsonValueKind.String)
            {
                string? value = codeValue.GetString();
                string[] allowed = ["validation", "email_in_use", "email_unavailable", "reset_unavailable", "invalid_credentials", "account_ineligible", "email_verification_required", "verification_request_failed", "invalid_verification", "reset_request_failed", "invalid_reset", "unauthorized", "avatar_not_found", "avatar_unavailable", "service_unavailable"];
                if (value is not null && allowed.Contains(value))
                {
                    code = value;
                    if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String && title.GetString() is { Length: > 0 and <= 300 } safeTitle && !safeTitle.Contains('\n')) message = safeTitle;
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        string[] fields = ["Email", "DisplayName", "Password", "ConfirmPassword", "CurrentPassword", "NewPassword", "AvatarUrl", "Code"];
                        foreach (string candidate in fields) if (errors.TryGetProperty(candidate, out _)) { field = candidate; break; }
                    }
                    if (root.TryGetProperty("requestId", out var id) && id.ValueKind == JsonValueKind.String && Guid.TryParse(id.GetString(), out var parsed)) requestId = parsed;
                }
            }
        }
        catch (JsonException) { }
        return new(status, code, message, field, requestId);
    }
}
