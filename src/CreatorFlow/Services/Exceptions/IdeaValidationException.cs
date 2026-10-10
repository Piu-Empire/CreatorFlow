namespace CreatorFlow.Services.Exceptions;

/// <summary>Ném ra khi dữ liệu Idea không hợp lệ hoặc thao tác không được phép với trạng thái hiện tại của Idea.</summary>
public class IdeaValidationException : Exception
{
    public IdeaValidationException(string message) : base(message)
    {
    }
}
