namespace Web.Features.Grids.UpdateVisibility;

public class UpdateVisibilityRequest
{
    public Guid ExamId { get; set; }
    public bool IsPublic { get; set; }
}
