namespace Web.Features.Members.Classes.DuplicateClass;

public class DuplicateClassRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ProgramId { get; set; }
    public Guid SourceClassId { get; set; }
    public List<string> ExamNames { get; set; } = new();
}
