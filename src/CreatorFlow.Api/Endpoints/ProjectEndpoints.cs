using CreatorFlow.Api.Services.Projects;
using CreatorFlow.Contracts.Projects;

namespace CreatorFlow.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/projects").RequireAuthorization();
        group.MapGet("/", async (ProjectService service, CancellationToken token) => Http(await service.ListAsync(token)));
        group.MapGet("/{id:long}", async (long id, ProjectService service, CancellationToken token) => Http(await service.GetAsync(id, token)));
        group.MapGet("/{id:long}/access", async (long id, ProjectService service, CancellationToken token) => Http(await service.AccessAsync(id, token)));
        group.MapGet("/{id:long}/members", async (long id, ProjectMembersService service, CancellationToken token) => Http(await service.ListAsync(id, token)));
        group.MapPut("/{id:long}/members/{userId:long}/role", async (long id, long userId, UpdateProjectMemberRoleRequest input,
            ProjectMembersService service, CancellationToken token) => Http(await service.UpdateRoleAsync(id, userId, input, token)));
        group.MapDelete("/{id:long}/members/{userId:long}", async (long id, long userId, ProjectMembersService service, CancellationToken token) =>
            Http(await service.RemoveAsync(id, userId, token)));
        group.MapPost("/", async (CreateProjectRequest input, ProjectService service, CancellationToken token) =>
        {
            var result = await service.CreateAsync(input, token);
            return result.Code is null ? Results.Created($"/api/projects/{result.Value!.ProjectId}", result.Value) : Http(result);
        });
        group.MapPut("/{id:long}", async (long id, UpdateProjectRequest input, ProjectService service, CancellationToken token) => Http(await service.UpdateAsync(id, input, token)));
        group.MapPost("/{id:long}/archive", async (long id, ProjectService service, CancellationToken token) => Http(await service.ArchiveAsync(id, token)));
        group.MapPost("/{id:long}/restore", async (long id, ProjectService service, CancellationToken token) => Http(await service.RestoreAsync(id, token)));
        group.MapDelete("/{id:long}", async (long id, ProjectService service, CancellationToken token) => Http(await service.DeleteAsync(id, token)));
    }

    private static IResult Http<T>(ProjectResult<T> result) => result.Code is null ?
        Results.Json(result.Value, statusCode: result.Status) : AuthEndpoints.Problem(result.Code, result.Message!, result.Status, result.Field);
}
