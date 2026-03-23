namespace Web.Features.Grids.GetGrids;

public record GridDto(
    Guid Id,
    Guid ClassId,
    string Name,
    string CourseCode,
    string SessionName,
    bool IsPublic,
    DateTime CreatedAt
);
