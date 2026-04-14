namespace Web.Dtos;

public class SaveClassSkillsRequest
{
    public List<Guid> SkillIds { get; set; } = new();
}
