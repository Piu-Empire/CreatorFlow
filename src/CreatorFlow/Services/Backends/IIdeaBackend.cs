using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Nguồn dữ liệu của màn Idea Bank: IdeaService (chạy thẳng DB / InMemory) hoặc ApiIdeaBackend (qua CreatorFlow.Api,
/// quyền và validation kiểm tra ở Backend). IdeaBankControl chỉ biết interface này.
/// </summary>
public interface IIdeaBackend
{
    List<Idea> GetIdeas(long projectId, long userId, IdeaFilter? filter = null);
    List<string> GetProjectTags(long projectId, long userId);
    int CountBacklog(long projectId, long userId);

    bool CanCreate(long projectId, long userId);
    bool CanEdit(Idea idea, long userId);
    bool CanDelete(Idea idea, long userId);
    bool CanConvert(Idea idea, long userId);

    long Create(long projectId, IdeaDraft draft, long userId);
    void Update(long ideaId, IdeaDraft draft, long userId);
    long ConvertToContent(long ideaId, long userId);
    void Delete(long ideaId, long userId);
}
