namespace Web.Features.Members.Classes.CreateClass;

public class CreateClassRequest
{
    public string Name { get; set; } = null!;
    public List<StudentRequest>? Students { get; set; }
}

public class StudentRequest
{
    public string Number { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
}
