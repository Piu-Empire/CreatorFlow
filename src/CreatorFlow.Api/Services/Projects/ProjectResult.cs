namespace CreatorFlow.Api.Services.Projects;

public sealed record ProjectResult<T>(T? Value, string? Code = null, string? Message = null, int Status = 200, string? Field = null);
