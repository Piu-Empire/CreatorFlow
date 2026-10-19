using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreatorFlow.AI.Models;

namespace CreatorFlow.AI;

public sealed class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly AIOptions _options;

    public AIService(
        HttpClient httpClient,
        AIOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<AiResult> AskAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return Failure("Prompt không được để trống.");
        }

        var configError = _options.Validate();
        if (configError is not null)
        {
            return Failure(configError);
        }

        using var timeoutCts =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(_options.TimeoutSeconds));

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                _options.Endpoint);

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _options.ApiKey);

            request.Content = JsonContent.Create(new
            {
                model = _options.Model,

                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
            });

            using var response =
                await _httpClient.SendAsync(
                    request,
                    linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var detail =
                    await TryReadBodyAsync(
                        response,
                        linkedCts.Token);

                return HandleHttpError(
                    response.StatusCode,
                    detail);
            }

            var json =
                await response.Content.ReadAsStringAsync(
                    linkedCts.Token);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Failure(
                    "AI API trả về dữ liệu rỗng.");
            }

            return ParseResponse(json);
        }
        catch (OperationCanceledException)
            when (timeoutCts.IsCancellationRequested)
        {
            return Failure(
                "AI request bị timeout.");
        }
        catch (OperationCanceledException)
        {
            return Failure(
                "AI request đã bị hủy.");
        }
        catch (HttpRequestException)
        {
            return Failure(
                "Không thể kết nối tới AI API.");
        }
        catch (JsonException)
        {
            return Failure(
                "AI API trả về JSON không hợp lệ.");
        }
        catch (Exception)
        {
            return Failure(
                "Có lỗi xảy ra khi gọi AI.");
        }
    }

    private static AiResult ParseResponse(string json)
    {
        using var document =
            JsonDocument.Parse(json);

        var root =
            document.RootElement;

        if (!root.TryGetProperty(
                "choices",
                out var choices))
        {
            return Failure(
                "Không tìm thấy choices trong response.");
        }

        if (choices.GetArrayLength() == 0)
        {
            return Failure(
                "AI không trả về kết quả.");
        }

        var firstChoice =
            choices[0];

        if (!firstChoice.TryGetProperty(
                "message",
                out var message))
        {
            return Failure(
                "Không tìm thấy message.");
        }

        if (!message.TryGetProperty(
                "content",
                out var content))
        {
            return Failure(
                "Không tìm thấy content.");
        }

        var text = ExtractContentText(content);

        if (string.IsNullOrWhiteSpace(text))
        {
            return Failure(
                "AI trả về nội dung rỗng.");
        }

        return new AiResult
        {
            Success = true,
            Content = text
        };
    }

    private static AiResult HandleHttpError(
        HttpStatusCode statusCode,
        string? detail = null)
    {
        var errorMessage =
            statusCode switch
            {
                HttpStatusCode.BadRequest
                    => "Request gửi tới AI không hợp lệ.",

                HttpStatusCode.Unauthorized
                    => "API key không hợp lệ.",

                HttpStatusCode.Forbidden
                    => "Không có quyền truy cập AI API.",

                HttpStatusCode.TooManyRequests
                    => "Đã vượt giới hạn request của AI API.",

                HttpStatusCode.InternalServerError
                    => "AI server đang gặp lỗi.",

                HttpStatusCode.BadGateway
                    => "AI server đang gặp lỗi.",

                HttpStatusCode.ServiceUnavailable
                    => "AI service hiện không khả dụng.",

                HttpStatusCode.GatewayTimeout
                    => "AI server phản hồi quá chậm.",

                _ =>
                    $"AI API trả lỗi HTTP {(int)statusCode}."
            };

        if (!string.IsNullOrWhiteSpace(detail))
        {
            var shortDetail =
                detail.Length > 300
                    ? detail[..300] + "..."
                    : detail;

            errorMessage += $" Chi tiết: {shortDetail}";
        }

        return new AiResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            StatusCode = (int)statusCode
        };
    }

    private static string? ExtractContentText(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString();
        }

        if (content.ValueKind == JsonValueKind.Array)
        {
            var parts = new List<string>();

            foreach (var item in content.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        parts.Add(s);
                    }

                    continue;
                }

                if (item.ValueKind == JsonValueKind.Object
                    && item.TryGetProperty("text", out var text))
                {
                    var s = text.GetString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        parts.Add(s);
                    }
                }
            }

            return string.Join(string.Empty, parts);
        }

        return null;
    }

    private static async Task<string?> TryReadBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            return string.IsNullOrWhiteSpace(body)
                ? null
                : body;
        }
        catch
        {
            return null;
        }
    }

    private static AiResult Failure(
        string errorMessage)
    {
        return new AiResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}