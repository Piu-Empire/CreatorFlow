using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryContentScriptRepository : IContentScriptRepository
{
    public ContentScript? GetByContentId(long contentId)
    {
        var r = InMemoryDataStore.Contents.FirstOrDefault(c => c.Id == contentId);
        if (r == null)
            return null;

        return new ContentScript
        {
            ContentId = r.Id,
            ProjectId = r.ProjectId,
            Status = r.Status,
            CreatedByUserId = r.CreatedByUserId,
            Script = string.IsNullOrEmpty(r.Script) ? null : r.Script,
        };
    }

    public bool TryUpdateScript(long contentId, string? expectedScript, string? newScript)
    {
        var r = InMemoryDataStore.Contents.FirstOrDefault(c => c.Id == contentId);
        if (r == null)
            return false;

        // Cùng ngữ nghĩa với bản Npgsql: null và chuỗi rỗng đều là "chưa có script".
        if (!string.Equals(r.Script ?? string.Empty, expectedScript ?? string.Empty, StringComparison.Ordinal))
            return false;

        r.Script = newScript ?? string.Empty;
        r.UpdatedAt = DateTime.Now;
        return true;
    }
}
