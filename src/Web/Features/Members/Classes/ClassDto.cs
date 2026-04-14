namespace Web.Features.Members.Classes;

public class ClassDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ProgramId { get; set; }
    public string? ProgramName { get; set; }
}
