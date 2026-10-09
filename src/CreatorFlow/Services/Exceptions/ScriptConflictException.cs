namespace CreatorFlow.Services.Exceptions;

/// <summary>Ném ra khi script đã bị người khác sửa kể từ lúc người dùng mở nó (phát hiện bằng ScriptDocument.Version).</summary>
public class ScriptConflictException : Exception
{
    public ScriptConflictException()
        : base("Kịch bản đã được người khác cập nhật sau khi bạn mở. Hãy tải lại bản mới nhất rồi chỉnh sửa tiếp.")
    {
    }
}
