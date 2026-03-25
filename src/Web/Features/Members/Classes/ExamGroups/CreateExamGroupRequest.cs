namespace Web.Features.Members.Classes.ExamGroups;

public class CreateExamGroupRequest
{
    public string Name { get; set; } = "";
    public Guid ClassId { get; set; }
    public List<GroupStudentRequest>? Students { get; set; }
}

public class GroupStudentRequest
{
    public string Number { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}
