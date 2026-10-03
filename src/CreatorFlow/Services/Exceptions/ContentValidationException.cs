namespace CreatorFlow.Services.Exceptions;

/// <summary>Ném ra khi dữ liệu Content nhập vào không hợp lệ (thiếu tiêu đề, quá dài, ...).</summary>
public class ContentValidationException : Exception
{
    public ContentValidationException(string message) : base(message)
    {
    }
}
