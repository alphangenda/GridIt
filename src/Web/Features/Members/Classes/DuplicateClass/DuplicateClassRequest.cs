namespace Web.Features.Members.Classes.DuplicateClass;

public class DuplicateClassRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ProgramId { get; set; }
    public Guid SourceClassId { get; set; }
    public List<ExamRenameEntry> Exams { get; set; } = new();
}

public class ExamRenameEntry
{
    public Guid SourceExamId { get; set; }
    public string Name { get; set; } = string.Empty;
}
