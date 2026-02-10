namespace Web.Features.Members.Classes;

public class ExamDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string Name { get; set; } = null!;
}
