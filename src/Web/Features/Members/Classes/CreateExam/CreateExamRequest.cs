namespace Web.Features.Members.Classes.CreateExam;

public class CreateExamRequest
{
    public Guid ClassId { get; set; }
    public string Name { get; set; } = null!;
}
