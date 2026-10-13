using CreatorFlow.Contracts.Projects;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services;

public sealed record ProjectSnapshot(long ProjectId, string Name, ProjectRole Role);

public sealed class ProjectContext
{
    public ProjectSnapshot? Current { get; private set; }
    public long Generation { get; private set; }
    public event EventHandler? Changed;

    public void Apply(ProjectAccessResponse response)
    {
        if (response.ProjectId <= 0 || string.IsNullOrWhiteSpace(response.Name) || response.Status != "ACTIVE" ||
            response.CurrentUserRole is not ("OWNER" or "MANAGER" or "CREATOR"))
            throw new CreatorFlow.ApiClients.ApiException(CreatorFlow.ApiClients.ApiErrorKind.InvalidResponse, "API trả Project không hợp lệ.");
        var role = Enum.Parse<ProjectRole>(response.CurrentUserRole, true);
        Current = new(response.ProjectId, response.Name, role);
        Publish();
    }

    public void Rename(long id, string name)
    {
        if (Current is not { } current || current.ProjectId != id) return;
        Current = current with { Name = name };
        Publish();
    }

    public void Clear() { Current = null; Publish(); }
    private void Publish()
    {
        Generation++;
        CurrentSession.CurrentProjectId = Current?.ProjectId ?? 0;
        CurrentSession.CurrentProjectName = Current?.Name ?? string.Empty;
        CurrentSession.CurrentUserRole = Current?.Role;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
