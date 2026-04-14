namespace Web.Features.Members.Classes.CreateClass;

public class CreateClassRequest
{
    public string Name { get; set; } = string.Empty;
    public List<Guid> SkillIds { get; set; } = new();
    public Guid? ProgramId { get; set; }
}
